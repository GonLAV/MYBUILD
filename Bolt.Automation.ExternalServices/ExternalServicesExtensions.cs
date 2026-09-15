using Bolt.Automation.ExternalServices.CasePortal.Infrastructure;
using Bolt.Automation.ExternalServices.LaunchDarkly;
using Bolt.Automation.ExternalServices.Outlook;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bolt.Automation.ExternalServices
{
    public static class ExternalServicesExtensions
    {
        public static IServiceCollection AddExternalServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOutlookClient(configuration);
            services.AddLaunchDarklyServices(configuration);
            services.AddCasePortal();

            return services;
        }


    }
}
