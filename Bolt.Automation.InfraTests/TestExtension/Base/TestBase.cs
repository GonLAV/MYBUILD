using System.Reflection;
using Bolt.Automation.Common;
using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.Core.Infrastructure;
using Bolt.Automation.TestDataProvider.Context;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using NUnit.Framework;
using Environment = Bolt.Automation.Common.Environment;
using TenantAttribute = Bolt.Automation.InfraTests.TestExtension.Attributes.TenantAttribute;

namespace Bolt.Automation.InfraTests.TestExtension.Base
{
    public abstract class TestBase : IDisposable
    {
        // Per-test instance fields instead of static properties
        protected IConfigurationRoot Configuration { get; private set; }
        protected Environment Environment { get; private set; }
        protected IServiceProvider ServiceProvider { get; private set; }

        protected IServiceScope _testScope;
        protected IAutomationLogger _logger;

        protected IScopeContext ScopeContext { get; }
        protected TestContextAccessor TestContextAccessor { get; }
        protected TestBase()
        {
            // Get per-test configuration and environment
            (Configuration, Environment) = TestInfrastructure.GetConfiguration();

            // Create per-test service provider
            ServiceProvider = TestInfrastructure.CreateServiceProvider(Configuration, Environment);

            _testScope = ServiceProvider.CreateScope();
            var scopedProvider = _testScope.ServiceProvider;

            var scopeContext = scopedProvider.GetRequiredService<IScopeContext>();
            scopeContext.StartAsyncChildScope();
            ScopeContext = scopeContext;

            _logger = scopedProvider.GetRequiredService<IAutomationLogger>();

            // Store environment and tenant in strongly-typed context
            scopeContext.Set(ctx => ctx.Environment, Environment);        
            var tenant = GetTestTenant();
            if (tenant.HasValue)
            {
                scopeContext.Set(ctx => ctx.Tenant, tenant.Value);
            }

            TestContextAccessor = scopedProvider.GetRequiredService<TestContextAccessor>();
            InitializeTestDataCollections();

            var testMethodName = GetTestMethodName();
        }

        [SetUp]
        public void InitializeTestOutput()
        {
            LoggerFactory.SetTestOutput(TestContext.Out);

            // Retry tenant + data collection init — NUnit test properties
            // may not be available during the constructor for all test types.
            if (ScopeContext.Data.Tenant == null)
            {
                var tenant = GetTestTenant();
                if (tenant.HasValue)
                {
                    ScopeContext.Set(ctx => ctx.Tenant, tenant.Value);
                    InitializeTestDataCollections();
                }
            }
        }

        [TearDown]
        public virtual void TearDown()
        {
            LoggerFactory.ClearTestOutput();
            LogManager.Flush(TimeSpan.FromSeconds(5));
        }

        private void InitializeTestDataCollections()
        {
            try
            {
                ScopeContext.Set(ctx => ctx.UrlDataCollection, TestContextAccessor.CurrentUrlCollection);
                ScopeContext.Set(ctx => ctx.DatabaseCollection, TestContextAccessor.CurrentDatabaseConnectionCollection);
            }
            catch (InvalidOperationException)
            {
                // Tenant/Environment not yet available — will retry in [SetUp]
            }
        }

        public virtual void Dispose()
        {
            _testScope?.Dispose();

            if (ServiceProvider is IDisposable disposableServiceProvider)
            {
                disposableServiceProvider.Dispose();
            }
        }

        protected string GetTestMethodName()
        {
            return TestContext.CurrentContext?.Test?.MethodName ?? "Unknown";
        }

        private Tenant? GetTestTenant()
        {
            // Try to get tenant from NUnit test properties
            var props = TestContext.CurrentContext?.Test?.Properties;
            if (props != null && props.ContainsKey("Tenant"))
            {
                var tenantStr = props["Tenant"].FirstOrDefault()?.ToString();
                if (tenantStr != null && Enum.TryParse<Tenant>(tenantStr, out var tenantEnum))
                    return tenantEnum;
            }

            // Try to get tenant from custom [Tenant] attribute
            var testMethod = GetTestMethod();
            var tenantAttr = testMethod?.GetCustomAttribute<TenantAttribute>()
                             ?? testMethod?.DeclaringType?.GetCustomAttribute<TenantAttribute>();
            if (tenantAttr != null)
            {
                return tenantAttr.TenantValue;
            }

            return null;
        }

        private MethodInfo? GetTestMethod()
        {
            // NUnit: TestContext provides test method name
            var methodName = TestContext.CurrentContext?.Test?.MethodName;
            if (methodName != null)
            {
                var type = GetType();
                var method = type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (method != null) return method;
            }

            return null;
        }

        // Virtual method for test artifact path, can be overridden in derived classes
        protected virtual string GetTestArtifactsPath()
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestResults");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            return path;
        }
    }
}