using Bolt.Automation.ApiClients.PlatformApi.Handlers;
using Microsoft.Extensions.DependencyInjection;

namespace Bolt.Automation.ApiClients.PlatformApi
{
    public static class PlatformApiServiceExtensions
    {
        public static IServiceCollection AddPlatformApi(this IServiceCollection services)
        {
            services.AddScoped<IPlatformApiClientFactory, PlatformApiClientFactory>();
            services.AddScoped<PlatformApiHandler>();
            services.AddHttpClient("PlatformApiClient")
                .AddHttpMessageHandler<PlatformApiHandler>();

            return services;


        }
    }
}

