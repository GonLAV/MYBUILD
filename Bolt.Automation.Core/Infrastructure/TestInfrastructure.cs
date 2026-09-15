using Bolt.Automation.Common.Logging.Extentions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Environment = Bolt.Automation.Common.Environment;
using Host = Microsoft.Extensions.Hosting.Host;

namespace Bolt.Automation.Core.Infrastructure
{
    public static class TestInfrastructure
    {
        /// <summary>
        /// Gets configuration and environment for a specific test context.
        /// This method is called per-test to ensure proper isolation.
        /// </summary>
        /// <param name="testParameters">Optional test-specific parameters</param>
        /// <returns>Configuration and resolved environment</returns>
        public static (IConfigurationRoot Configuration, Environment Environment) GetConfiguration(
            IDictionary<string, string> testParameters = null)
        {
            return ConfigurationLoader.LoadConfigurations(testParameters);
        }

        /// <summary>
        /// Creates a service provider for a specific test context.
        /// Each test gets its own service provider for proper isolation.
        /// </summary>
        /// <param name="configuration">Test-specific configuration</param>
        /// <param name="environment">Resolved environment enum</param>
        /// <param name="configureServices">Optional callback to add or decorate services after infrastructure registration.</param>
        /// <returns>Service provider instance</returns>
        public static IServiceProvider CreateServiceProvider(
            IConfigurationRoot configuration,
            Environment environment,
            Action<IServiceCollection>? configureServices = null)
        {
            var builder = Host.CreateApplicationBuilder();

            // Register the environment enum so services can access it
            builder.Services.AddSingleton<Func<Environment>>(() => environment);

            builder.Services.AddInfrastructureServices(
                configuration,
                configuration.GetSection("Logging"));
            builder.Services.AddNLogWithTestOutputTarget();

            configureServices?.Invoke(builder.Services);

            var host = builder.Build();
            return host.Services;
        }

    }
}
