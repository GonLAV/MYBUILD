using Bolt.Automation.ApiClients;
using Bolt.Automation.Common;
using Bolt.Automation.Common.Logging.Extentions;
using Bolt.Automation.Common.Logging.Mongo;
using Bolt.Automation.ExternalServices;
using Bolt.Automation.FrontEnds;
using Bolt.Automation.InternalServices;
using Bolt.Automation.TestDataProvider;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bolt.Automation.Core.Infrastructure
{
    public static class InfrastructureServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services,
                    IConfiguration configuration, IConfiguration loggingConfiguration)
        {
            // Validate configuration
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration), @"Configuration cannot be null");

            if (loggingConfiguration == null)
                throw new ArgumentNullException(nameof(loggingConfiguration), @"Logging configuration cannot be null");

            // Register logging and common services
            services.AddCommonServices(configuration, loggingConfiguration);
            services.AddStandardLogging(loggingConfiguration);
            services.AddAutomationLogger();
            services.AddMongoReporting(configuration);
            services.AddTestDataProvider(configuration);

            services.AddApiClients(configuration);
            services.AddExternalServices(configuration);
            services.AddFrontEndServices(configuration);
            services.AddMemoryCache();
            services.AddInternalServices(configuration);

            // Register the configuration itself for future use
            services.AddSingleton(configuration);

            return services;
        }
    }
}
