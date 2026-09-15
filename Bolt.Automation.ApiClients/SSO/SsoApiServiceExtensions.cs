using Bolt.Automation.ApiClients.SSO.Handlers;
using Microsoft.Extensions.DependencyInjection;

namespace Bolt.Automation.ApiClients.SSO
{
    public static class SsoApiServiceExtensions
    {
        public static IServiceCollection AddSsoApi(this IServiceCollection services)
        {
            services.AddScoped<SsoApiHandler>();

            services.AddHttpClient("SsoApiClient")
                .AddHttpMessageHandler<SsoApiHandler>();

            services.AddScoped<ISsoApiFactory, SsoApiFactory>();

            return services;
        }
    }
}
