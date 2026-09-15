using Bolt.Automation.ApiClients.StsApi.Handlers;
using Microsoft.Extensions.DependencyInjection;

namespace Bolt.Automation.ApiClients.StsApi
{
    public static class StsApiServiceExtensions
    {
        public static IServiceCollection AddStsApi(this IServiceCollection services)
        {
            services.AddScoped<StsApiHandler>();
            services.AddHttpClient("StsApiClient")
                .AddHttpMessageHandler<StsApiHandler>();

            return services;
        }
    }
}

