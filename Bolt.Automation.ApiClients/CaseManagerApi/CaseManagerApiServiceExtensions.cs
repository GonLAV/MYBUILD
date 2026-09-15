using Automation.Configuration.ApiClients;
using Bolt.Automation.ApiClients.CaseManagerApi.Handlers;
using Bolt.Automation.ApiClients.CaseManagerApi.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Refit;

namespace Bolt.Automation.ApiClients.CaseManagerApi
{
    public static class CaseManagerApiServiceExtensions
    {
        public static IServiceCollection AddCaseManagerApi(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<CaseManagerApiOptions>(configuration.GetSection(CaseManagerApiOptions.ConfigSection));
            services.AddScoped<CaseManagerApiKeyHandler>();
            services.AddHttpClient("CaseManagerApiClient")
                .AddHttpMessageHandler<CaseManagerApiKeyHandler>();

            services.AddRefitClient<ICaseManagerApi>()
                .ConfigureHttpClient((sp, client) =>
                {
                    var options = sp.GetRequiredService<IOptions<CaseManagerApiOptions>>();
                    client.BaseAddress = new Uri(options.Value.BaseUrl);
                })
                .ConfigurePrimaryHttpMessageHandler(sp => sp.GetRequiredService<CaseManagerApiKeyHandler>());

            return services;
        }
    }
}

