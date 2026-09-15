using Automation.Configuration.FrontEnds;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure
{
    public static class BrowserServiceExtensions
    {
        public static IServiceCollection AddBrowserServices(this IServiceCollection services, IConfiguration configuration)
        {
            var browserOptions = new BrowserOptions();
            configuration.GetSection(BrowserOptions.ConfigSection).Bind(browserOptions);
            services.AddSingleton(browserOptions);

            services
                .AddScoped<IBrowserManager, BrowserManager>()
                .AddScoped<IPlaywrightDriverInitializer, PlaywrightDriverInitializer>();
            services.AddScoped<IPageHelperFactory, PageHelperFactory>();
            services.AddScoped<IPageFactory, PageFactory>();

            return services;
        }
    }
}

