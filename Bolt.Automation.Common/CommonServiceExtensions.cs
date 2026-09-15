using Bolt.Automation.Common.Logging.Extentions;
using Bolt.Automation.Common.PollyRetry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bolt.Automation.Common
{
    public static class CommonServiceExtensions
    {
        public static IServiceCollection AddCommonServices(this IServiceCollection services, IConfiguration configuration, IConfiguration loggingConfiguration)
        {
            // Centralized registration of common services, including logging
            services.AddStandardLogging(loggingConfiguration);
            services.AddAutomationLogger();
            services.AddSingleton<PolicyProvider>();
            services.AddSingleton<IPollyRetryService, PollyRetryService>();
            // Add other common (non-logging) service registrations here if needed
            return services;
        }
    }
}
