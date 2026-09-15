using Bolt.Automation.FrontEnds.Executor.Helpers;
using Bolt.Automation.FrontEnds.Executor.Services;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Microsoft.Extensions.DependencyInjection;

namespace Bolt.Automation.FrontEnds.Executor.ExecutorInfra
{
    public static class PlaywrightExecutorExtensions
    {
        public static IServiceCollection AddPlaywrightInterviewExecutor(this IServiceCollection services)
        {
            if (!services.IsServiceRegistered<PlaywrightExecutor>())
            {
                services.AddPlaywrightPageFactory();
                
                // Register helper services with constructor injection
                services.AddScoped<PageFlowHelper>();
                services.AddScoped<PageCallbackManager>();
                
                // Register flow services with automatic constructor injection
                services.AddScoped<IFlowNavigationService, FlowNavigationService>();
                services.AddScoped<IFlowPreparationService, FlowPreparationService>();
                services.AddScoped<IFlowExecutionService, FlowExecutionService>();
                
                services.AddScoped<PlaywrightExecutor>();
            }

            return services;
        }

        public static IServiceCollection AddPlaywrightPageFactory(this IServiceCollection services)
        {
            if (!services.IsServiceRegistered<IPageFactory>())
            {
                services.AddSingleton<IPageFactory, PageFactory>();
            }

            return services;
        }
        
        private static bool IsServiceRegistered<T>(this IServiceCollection services)
        {
            return services.Any(d => d.ServiceType == typeof(T));
        }
    }
}
