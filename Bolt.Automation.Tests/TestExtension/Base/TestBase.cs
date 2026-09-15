using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Enums;
using Bolt.Automation.Common.Exceptions;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.Common.Logging.Mongo;
using Bolt.Automation.Common.Utils;
using Bolt.Automation.Core.Infrastructure;
using Bolt.Automation.InternalServices.Database.Services.Connections;
using Bolt.Automation.TestDataProvider.Context;
using Bolt.Automation.Tests.TestExtension.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using NUnit.Framework;
using Environment = Bolt.Automation.Common.Environment;

namespace Bolt.Automation.Tests.TestExtension.Base
{
    public abstract class TestBase
    {
        // Per-test instance fields instead of static properties
        protected IConfigurationRoot Configuration { get; private set; }
        protected Environment Environment { get; private set; }
        protected IServiceProvider ServiceProvider { get; private set; }

        protected IServiceScope _testScope;
        protected IAutomationLogger _logger;
        private readonly ITestRunWriter _testRunWriter;
        private readonly TestMetadataResolver _metadata;

        protected bool HasTestFailed =>
            TestContext.CurrentContext.Result.Outcome.Status == NUnit.Framework.Interfaces.TestStatus.Failed;

        protected IScopeContext ScopeContext { get; }
        protected TestContextAccessor TestContextAccessor { get; }

        protected TestBase()
        {
            _metadata = new TestMetadataResolver(GetType());

            (Configuration, Environment) = TestInfrastructure.GetConfiguration();

            ServiceProvider = TestInfrastructure.CreateServiceProvider(Configuration, Environment, services =>
            {
                services.AddScoped<IDbConnectionService>(sp =>
                {
                    var inner = new DbConnectionService(sp.GetRequiredService<IScopeContext>());
                    return new SkipGuardDbConnectionService(inner, sp.GetRequiredService<IAutomationLogger>());
                });
            });

            _testScope = ServiceProvider.CreateScope();
            var scopedProvider = _testScope.ServiceProvider;

            var scopeContext = scopedProvider.GetRequiredService<IScopeContext>();
            scopeContext.StartAsyncChildScope();
            ScopeContext = scopeContext;

            _logger = scopedProvider.GetRequiredService<IAutomationLogger>();
            _testRunWriter = scopedProvider.GetRequiredService<ITestRunWriter>();

            if (_logger is MongoLoggingDecorator decorator)
                decorator.SetTestId(_metadata.TestId);

            scopeContext.Set(ctx => ctx.Environment, Environment);
            NameSelector.RegisterScopeContext(scopeContext);
            var tenant = _metadata.Tenant;
            if (tenant.HasValue)
            {
                scopeContext.Set(ctx => ctx.Tenant, tenant.Value);
            }
            TestContextAccessor = scopedProvider.GetRequiredService<TestContextAccessor>();
            InitializeTestDataCollections();

        }

        [SetUp]
        public async Task InitializeTestAsync()
        {
            LoggerFactory.SetTestOutput(TestContext.Out);

            // Retry tenant + data collection init — NUnit test properties
            // may not be available during the constructor for all test types.
            if (ScopeContext.Data.Tenant == null)
            {
                var tenant = _metadata.Tenant;
                if (tenant.HasValue)
                {
                    ScopeContext.Set(ctx => ctx.Tenant, tenant.Value);
                    //ScopeContext.SetTestValue("Tenant", tenant.ToString());
                    InitializeTestDataCollections();
                }
            }

            try
            {
                var metadata = _metadata.BuildMetadata(
                    Environment.ToString(),
                    ScopeContext.Data.Lob?.ToString(),
                    ScopeContext.Data.FrontEnd?.ToString());
                await _testRunWriter.StartTestAsync(_metadata.TestId, _metadata.DisplayName, metadata)
                    .WaitAsync(TimeSpan.FromSeconds(10))
                    .ConfigureAwait(false);
            }
            catch (TimeoutException)
            {
                _logger.Warning($"MongoDB StartTestAsync timed out — test will continue without reporting");
            }
            catch (Exception ex)
            {
                _logger.Warning($"MongoDB StartTestAsync failed — test will continue: {ex.Message}");
            }

            // Validate the environment gate AFTER StartTestAsync, for the same reason
            // InjectionTestBase validates its INJECTED_* vars there: this throws
            // Assert.Inconclusive, NUnit marks the test skipped, and TearDown records the
            // outcome via CompleteTestAsync. Running it first meant the run row was never
            // opened, so an environment-skipped test incremented `skipped` (and decremented
            // `inProgressTests`) without ever incrementing `totalTests` — which is why a
            // run's skipped count could exceed its total, and why `inProgressTests` could
            // go negative and mark a run Completed while tests were still running.
            EnvironmentChecker.ValidateTestExecution(GetType(), Environment);

            ResolveServices();
        }

        /// <summary>
        /// Override to resolve DI services needed by the test class.
        /// Called during [SetUp] AFTER the logger record has been created,
        /// so DI failures will be reported as test failures, not silently lost.
        /// Do NOT resolve services in the constructor — use this hook instead.
        /// </summary>
        protected virtual void ResolveServices() { }

        private void InitializeTestDataCollections()
        {
            try
            {
                ScopeContext.Set(ctx => ctx.UrlDataCollection, TestContextAccessor.CurrentUrlCollection);
                ScopeContext.Set(ctx => ctx.DatabaseCollection, TestContextAccessor.CurrentDatabaseConnectionCollection);
            }
            catch (TestSetupException)
            {
                // Tenant/Environment not yet available — will retry in [SetUp]
            }
        }

        [TearDown]
        public virtual async Task TearDownAsync()
        {
            var (outcome, failureData) = _metadata.DetectOutcome();

            if (outcome == "Failed")
                _logger.Error($"Test FAILED: {failureData?.CombinedMessage}");
            else if (outcome == "Skipped")
                _logger.Info($"Test SKIPPED: {failureData?.Messages.FirstOrDefault()}");

            try
            {
                await _testRunWriter.SetIdentifiersAsync(_metadata.TestId, ScopeContext.Data).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.Warning($"SetIdentifiersAsync failed: {ex.Message}");
            }

            try
            {
                // Same 10s guard as StartTestAsync: a hung Mongo write here would burn
                // the test's remaining worker-timeout budget and get the process killed
                // — losing this very outcome.
                await _testRunWriter.CompleteTestAsync(_metadata.TestId, outcome, failureData)
                    .WaitAsync(TimeSpan.FromSeconds(10))
                    .ConfigureAwait(false);
            }
            catch (TimeoutException)
            {
                _logger.Warning("MongoDB CompleteTestAsync timed out — outcome not recorded");
            }
            catch (Exception ex)
            {
                _logger.Warning($"CompleteTestAsync failed: {ex.Message}");
            }

            if (_logger is MongoLoggingDecorator dec)
                dec.ClearTestId();

            LoggerFactory.ClearTestOutput();
            LogManager.Flush(TimeSpan.FromSeconds(5));
            _testScope?.Dispose();

            if (ServiceProvider is IDisposable disposableServiceProvider)
                disposableServiceProvider.Dispose();
        }

        /// <summary>
        /// Uploads a local artifact file to S3 and records it in MongoDB.
        /// Returns the S3 URL if upload succeeded, null otherwise.
        /// </summary>
        protected async Task<string?> UploadArtifactAsync(string localPath, string name, ArtifactType type)
        {
            if (string.IsNullOrEmpty(localPath)) return null;
            return await _testRunWriter.UploadAndAddArtifactAsync(_metadata.TestId, localPath, name, type)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Uploads artifact bytes directly to S3 (no disk IO) and records in MongoDB.
        /// Returns the S3 URL if upload succeeded, null otherwise.
        /// </summary>
        protected async Task<string?> UploadArtifactBytesAsync(byte[] data, string fileName, string contentType, ArtifactType type)
        {
            if (data.Length == 0) return null;
            return await _testRunWriter.UploadAndAddArtifactAsync(_metadata.TestId, data, fileName, contentType, type)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Skips the current test via <see cref="Assert.Inconclusive(string)"/> if the SQL Server
        /// for the specified <paramref name="dbType"/> is not reachable.
        /// Checks connectivity once per connection string and caches the result.
        /// </summary>
        protected void SkipIfDatabaseUnavailable(DatabaseType dbType = DatabaseType.MainDB)
        {
            var dbCollection = ScopeContext.Data.DatabaseCollection;
            if (dbCollection == null || !dbCollection.HasConnection(dbType))
            {
                Assert.Inconclusive($"Database collection not configured for {dbType} — skipping test.");
                return;
            }

            var connectionString = dbCollection[dbType];
            SqlConnectionGuard.SkipIfUnavailable(connectionString, dbType, _logger);
        }

        protected string GetTestMethodName() => _metadata.MethodName;

        protected virtual string GetTestArtifactsPath()
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestResults");
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            return path;
        }
    }
}