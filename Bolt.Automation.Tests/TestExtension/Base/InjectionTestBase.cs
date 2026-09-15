using Bolt.Automation.Common;
using Bolt.Automation.Common.Configuration.InjectedConfig;
using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.Common.Logging.Mongo;
using Bolt.Automation.Core.Infrastructure;
using Bolt.Automation.Tests.TestExtension.Helpers;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using NUnit.Framework;
using Environment = Bolt.Automation.Common.Environment;

namespace Bolt.Automation.Tests.TestExtension.Base
{
    /// <summary>
    /// Test base for runtime-injected tests that read all configuration from
    /// <c>INJECTED_*</c> environment variables instead of the static
    /// (Tenant, Environment)-keyed data stores. Use this for onboarding-style
    /// validation of newly added tenants (e.g. Professional Services suites) —
    /// see <see cref="InjectedTestConfig"/> for the supported keys.
    /// </summary>
    /// <remarks>
    /// Behavioural differences vs <see cref="TestBase"/>:
    ///   * Does NOT read the <c>[Tenant]</c> attribute. <see cref="InjectedTestConfig.Tenant"/> is a string.
    ///   * Does NOT load <c>appsettings.{Environment}.json</c> for tenant-keyed data —
    ///     <see cref="InjectedTestConfig.Environment"/> is a free-form string (no enum).
    ///   * Does NOT register <c>TestContextAccessor</c>, Refit API clients, or external services.
    ///   * Validates the full env-var set up-front and throws a single
    ///     <see cref="Common.Exceptions.InjectedConfigValidationException"/> listing every missing key.
    /// </remarks>
    public abstract class InjectionTestBase
    {
        protected InjectedTestConfig _injectedConfig;
        protected IServiceProvider ServiceProvider { get; private set; }
        protected IServiceScope _testScope;
        protected IAutomationLogger _logger;
        private readonly ITestRunWriter _testRunWriter;
        private readonly TestMetadataResolver _metadata;

        protected bool HasTestFailed =>
            TestContext.CurrentContext.Result.Outcome.Status == NUnit.Framework.Interfaces.TestStatus.Failed;

        protected IScopeContext ScopeContext { get; }

        /// <summary>
        /// Sections of <see cref="InjectedTestConfig"/> the test class consumes. Override with the
        /// property names of the sections you actually read (use <c>nameof</c>, not string literals,
        /// so refactors stay safe). Sections not in the set are not validated — a test that only
        /// touches Adbx does not need <c>INJECTED_PARTNER_PORTAL_*</c> or <c>INJECTED_D2C_*</c> set.
        /// Top-level <c>INJECTED_TENANT</c> and <c>INJECTED_ENVIRONMENT</c> are always required.
        /// </summary>
        protected virtual IReadOnlySet<string> RequiredSections => EmptyRequiredSections;

        private static readonly IReadOnlySet<string> EmptyRequiredSections = new HashSet<string>();

        protected InjectionTestBase()
        {
            _metadata = new TestMetadataResolver(GetType());

            // Load env vars WITHOUT validation. Validation runs in [SetUp] after the
            // MongoDB run record is opened (StartTestAsync), so a missing INJECTED_*
            // var produces a visible Failed row on the orchestrator runs page instead
            // of a silent ctor throw that NUnit can never wire to a run record.
            _injectedConfig = InjectedTestInfrastructure.GetConfig();

            ServiceProvider = InjectedTestInfrastructure.CreateServiceProvider(_injectedConfig);

            _testScope = ServiceProvider.CreateScope();
            var scopedProvider = _testScope.ServiceProvider;

            // Resolve logger + run writer FIRST so anything downstream (including
            // [SetUp] warnings on Enum.TryParse fall-throughs) can rely on a
            // non-null _logger.
            _logger = scopedProvider.GetRequiredService<IAutomationLogger>();
            _testRunWriter = scopedProvider.GetRequiredService<ITestRunWriter>();

            var scopeContext = scopedProvider.GetRequiredService<IScopeContext>();
            scopeContext.StartAsyncChildScope();
            ScopeContext = scopeContext;

            if (_logger is MongoLoggingDecorator decorator)
                decorator.SetTestId(_metadata.TestId);
        }

        [SetUp]
        public async Task InitializeInjectionTestAsync()
        {
            LoggerFactory.SetTestOutput(TestContext.Out);

            try
            {
                var metadata = _metadata.BuildMetadata(
                    // Prevents the orchestrator-forced ASPNETCORE_ENVIRONMENT from being SetOnInsert'd as the run's environment label before InjectedConfigValidator throws.
                    environment: string.IsNullOrWhiteSpace(_injectedConfig.Environment) ? "Unvalidated" : _injectedConfig.Environment,
                    lob: ScopeContext.Data.Lob?.ToString(),
                    frontEnd: ScopeContext.Data.FrontEnd?.ToString());
                // InjectionTestBase tests don't carry [Tenant] — overlay the INJECTED_TENANT string
                // so the run record is still tagged.
                metadata.Tenant = _injectedConfig.Tenant;

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

            // Validate INJECTED_* env vars AFTER StartTestAsync so a missing-var failure
            // lands in the MongoDB run row (and therefore on the orchestrator runs page)
            // with the full aggregated "INJECTED_X must be set" message. Letting this
            // throw is intentional — NUnit catches it, marks the test Failed, and the
            // TearDown handler below records the outcome via CompleteTestAsync.
            InjectedConfigValidator.Validate(_injectedConfig, RequiredSections);

            // Best-effort enum mapping. Tenants/environments without a C# enum entry
            // are intentionally left null on ScopeContext — runtime-injected tests
            // must not depend on those for their actual behaviour.
            if (Enum.TryParse<Environment>(_injectedConfig.Environment, true, out var environment))
                ScopeContext.Set(ctx => ctx.Environment, environment);
            else
                _logger.Warning($"INJECTED_ENVIRONMENT='{_injectedConfig.Environment}' is not a known Environment enum value. ScopeContext.Environment left null.");

            if (Enum.TryParse<Tenant>(_injectedConfig.Tenant, true, out var tenant))
                ScopeContext.Set(ctx => ctx.Tenant, tenant);
            else
                _logger.Warning($"INJECTED_TENANT='{_injectedConfig.Tenant}' is not a known Tenant enum value. ScopeContext.Tenant left null.");

            ResolveServices();
        }

        /// <summary>
        /// Override to resolve DI services needed by the test class.
        /// Called during [SetUp] AFTER the logger record has been created,
        /// so DI failures will be reported as test failures, not silently lost.
        /// Do NOT resolve services in the constructor — use this hook instead.
        /// </summary>
        protected virtual void ResolveServices() { }

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
