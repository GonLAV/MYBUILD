using Automation.Configuration.ApiClients;
using Bolt.Automation.ApiClients.GetQuoteApi.Handlers;
using Bolt.Automation.ApiClients.GetQuoteApi.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Refit;

namespace Bolt.Automation.ApiClients.GetQuoteApi
{
    public static class GetQuoteApiServiceExtensions
    {
        public static IServiceCollection AddGetQuoteApi(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<GetQuoteApiOptions>(configuration.GetSection(GetQuoteApiOptions.ConfigSection));
            services.AddScoped<GetQuoteApiKeyHandler>();
            services.AddHttpClient("GetQuoteClient")
                .AddHttpMessageHandler<GetQuoteApiKeyHandler>();

            var refitSettings = new RefitSettings
            {
                ContentSerializer = new NewtonsoftJsonContentSerializer(new JsonSerializerSettings
                {
                    ContractResolver = new DefaultContractResolver(),
                    NullValueHandling = NullValueHandling.Ignore
                })
            };

            services.AddRefitClient<IGetQuoteApi>(refitSettings)
                .ConfigureHttpClient((sp, client) =>
                {
                    var options = sp.GetRequiredService<IOptions<GetQuoteApiOptions>>();
                    client.BaseAddress = new Uri(options.Value.BaseUrl);
                })
                .ConfigurePrimaryHttpMessageHandler(sp => sp.GetRequiredService<GetQuoteApiKeyHandler>());

            return services;
        }
    }
}

