using Bolt.Automation.Common.Configuration.InjectedConfig;
using Bolt.Automation.Common.Logging.Extentions;
using Bolt.Automation.Common.Services.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Host = Microsoft.Extensions.Hosting.Host;

namespace Bolt.Automation.Core.Infrastructure
{
    /// <summary>
    /// Test infrastructure for runtime-injected tests. Mirrors <see cref="TestInfrastructure"/>
    /// but loads configuration from process environment variables (prefix <c>INJECTED_</c>) instead
    /// of <c>appsettings.json</c>, and registers an injection-only DI graph that excludes
    /// tenant-bound services. Calls <see cref="InjectedConfigValidator.Validate"/> before any
    /// service registration so missing env vars surface as a single aggregated error.
    /// </summary>
    public static class InjectedTestInfrastructure
    {
        /// <summary>
        /// Loads the injected test config from the process environment without validation.
        /// The test base class is responsible for calling
        /// <see cref="InjectedConfigValidator.Validate(InjectedTestConfig, IReadOnlySet{string})"/>
        /// with its required-sections set so each test only demands the env vars it actually uses.
        /// </summary>
        public static InjectedTestConfig GetConfig() => InjectedConfigLoader.Load();

        /// <summary>
        /// Builds a service provider for one runtime-injected test. Uses an empty <see cref="IConfiguration"/>
        /// for the standard infrastructure registration calls (logging, Mongo reporting, Playwright)
        /// so they can read whatever appsettings layers are still on disk via the host builder.
        /// </summary>
        public static IServiceProvider CreateServiceProvider(InjectedTestConfig injectedConfig,
     Action<IServiceCollection>? configureServices = null)
        {
            ArgumentNullException.ThrowIfNull(injectedConfig);

            var builder = Host.CreateApplicationBuilder();

            var environmentName = injectedConfig.Environment?.Trim();
            builder.Configuration
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile($"appsettings.{environmentName}.json", optional: true)
                // Load the Bolt secrets bundle so secret-only config (e.g. MongoReporting:ConnectionString,
                // Aws:AccessKey) resolves for injected tests too — mirrors ConfigurationLoader's standard path.
                // Without this the Mongo reporting writer sees an empty connection string, IsEnabled is false,
                // and every StartTest/CompleteTest/FlushLogs call silently no-ops (no run rows, no logs).
                .AddBoltSecrets(System.Environment.GetEnvironmentVariable("BOLT_SECRETS_PATH"), environmentName ?? string.Empty)
                .AddEnvironmentVariables();

            builder.Services.AddInjectedInfrastructureServices(
                builder.Configuration,
                builder.Configuration.GetSection("Logging"),
                injectedConfig);
            builder.Services.AddNLogWithTestOutputTarget();

            configureServices?.Invoke(builder.Services);

            var host = builder.Build();
            return host.Services;
        }

    }
}
