using Automation.Configuration.ExternalServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Bolt.Automation.ExternalServices.Outlook
{
    public static class OutlookServiceExtensions
    {
        public static IServiceCollection AddOutlookClient(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<OutlookClientOptions>(configuration.GetSection(OutlookClientOptions.ConfigSection));
            services.AddSingleton(sp =>
            {
                var settings = sp.GetRequiredService<IOptions<OutlookClientOptions>>();
                return new TokenProvider(settings);
            });
            services.AddHttpClient("OutlookClient")
                    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler());
            services.AddTransient<IOutlookClient, OutlookClient>();
            return services;
        }
    }
}

