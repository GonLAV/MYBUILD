using Automation.Configuration.InternalServices.MultiConfiguration;
using Bolt.Automation.InternalServices.Common.Interfaces;
using Bolt.Automation.InternalServices.MultiConfiguration;
using Bolt.Automation.InternalServices.MultiConfiguration.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bolt.Automation.InternalServices.Extensions
{
    public static class MultiConfigurationServiceExtensions
    {
        public static IServiceCollection AddMultiConfigurationServices(this IServiceCollection services, IBoltConfigurationRegistry registry, IConfiguration configuration)
        {
            services.Configure<MultiConfigurationOptions>(configuration.GetSection(MultiConfigurationOptions.SectionKey));
            services.AddSingleton(registry);
            services.AddSingleton<IMultiConfigurationService, MultiConfigurationService>();
            services.AddSingleton(typeof(IConfigSection<>), typeof(ConfigSection<>));
            return services;
        }
    }
}
