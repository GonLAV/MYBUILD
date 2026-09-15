using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Bolt.Automation.Common.Logging.Extentions
{
    public static class StandardLoggingExtensions
    {
        public static IServiceCollection AddStandardLogging(this IServiceCollection services, IConfiguration loggingConfiguration)
        {
            services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.ClearProviders();
                loggingBuilder.AddConfiguration(loggingConfiguration);
                loggingBuilder.AddConsole();
            });
            return services;
        }
    }
}

