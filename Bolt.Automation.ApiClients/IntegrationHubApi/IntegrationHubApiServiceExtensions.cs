using Automation.Configuration.ApiClients;
using Bolt.Automation.ApiClients.IntegrationHubApi.Handlers;
using Bolt.Automation.ApiClients.IntegrationHubApi.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Refit;

namespace Bolt.Automation.ApiClients.IntegrationHubApi
{
    public static class IntegrationHubApiServiceExtensions
    {
        public static IServiceCollection AddIntegrationHubApi(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<IntegrationHubApiOptions>(configuration.GetSection(IntegrationHubApiOptions.ConfigSection));
            services.AddScoped<TwilioSignatureHandler>();

            services.AddRefitClient<ITwilioWebhookApi>()
                .ConfigureHttpClient((sp, client) =>
                {
                    var options = sp.GetRequiredService<IOptions<IntegrationHubApiOptions>>();
                    client.BaseAddress = new Uri(options.Value.BaseUrl);
                })
                .AddHttpMessageHandler<TwilioSignatureHandler>();

            return services;
        }
    }
}
