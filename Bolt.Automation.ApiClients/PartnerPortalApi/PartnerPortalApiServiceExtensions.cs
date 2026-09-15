using Automation.Configuration.ApiClients;
using Bolt.Automation.ApiClients.PartnerPortalApi.Handlers;
using Bolt.Automation.ApiClients.PartnerPortalApi.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Refit;

namespace Bolt.Automation.ApiClients.PartnerPortalApi
{
    public static class PartnerPortalApiServiceExtensions
    {
        public static IServiceCollection AddPartnerPortalApi(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<PartnerPortalApiOptions>(configuration.GetSection(PartnerPortalApiOptions.ConfigSection));
            services.AddScoped<PartnerPortalApiHandler>();
            services.AddScoped<PartnerPortalApiAuthHandler>();

            services.AddHttpClient("PartnerPortalApiClient")
                .AddHttpMessageHandler<PartnerPortalApiHandler>();

            services.AddHttpClient("PartnerPortalApiAuthClient")
                .AddHttpMessageHandler<PartnerPortalApiAuthHandler>();

            services.AddRefitClient<IPartnerPortalAuthApi>()
                .ConfigureHttpClient((sp, client) =>
                {
                    var options = sp.GetRequiredService<IOptions<PartnerPortalApiOptions>>();
                    client.BaseAddress = new Uri(options.Value.BaseUrl);
                })
                .ConfigurePrimaryHttpMessageHandler(sp => sp.GetRequiredService<PartnerPortalApiAuthHandler>());

            services.AddRefitClient<IPartnerPortalApi>()
                .ConfigureHttpClient((sp, client) =>
                {
                    var options = sp.GetRequiredService<IOptions<PartnerPortalApiOptions>>();
                    client.BaseAddress = new Uri(options.Value.BaseUrl);
                })
                .ConfigurePrimaryHttpMessageHandler(sp => sp.GetRequiredService<PartnerPortalApiHandler>());

            return services;
        }
    }
}

