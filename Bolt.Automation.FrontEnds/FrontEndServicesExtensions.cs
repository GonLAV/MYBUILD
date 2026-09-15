using Bolt.Automation.FrontEnds.Executor.ExecutorInfra;
using Bolt.Automation.FrontEnds.PlaywrightBase.Accessibility;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bolt.Automation.FrontEnds
{
    public static class FrontEndServicesExtensions
    {
        public static IServiceCollection AddFrontEndServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Register browser-related services
            services.AddBrowserServices(configuration);
            
            // Register PlaywrightExecutor and related services
            services.AddPlaywrightInterviewExecutor();

            // Register axe-based accessibility scanning
            services.AddAccessibilityServices();

            // Add other frontend-related service registrations here as needed
            return services;
        }
    }
}

