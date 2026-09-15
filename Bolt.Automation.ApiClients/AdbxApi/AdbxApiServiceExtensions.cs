using Bolt.Automation.ApiClients.AdbxApi.Handlers;
using Microsoft.Extensions.DependencyInjection;

namespace Bolt.Automation.ApiClients.AdbxApi
{
    public static class AdbxApiServiceExtensions
    {
        public static IServiceCollection AddAdbxApi(this IServiceCollection services)
        {
            services.AddScoped<IAdbxApiClientFactory, AdbxApiClientFactory>();
            services.AddScoped<AdbxApiHandler>();
            services.AddHttpClient("AdbxApiClient")
            .AddHttpMessageHandler<AdbxApiHandler>();
            return services;
        }
    }
}
