using Bolt.Automation.ApiClients.AdbxApi;
using Bolt.Automation.ApiClients.CaseManagerApi;
using Bolt.Automation.ApiClients.GetQuoteApi;
using Bolt.Automation.ApiClients.Infrastructure;
using Bolt.Automation.ApiClients.IntegrationHubApi;
using Bolt.Automation.ApiClients.PartnerPortalApi;
using Bolt.Automation.ApiClients.PlatformApi;
using Bolt.Automation.ApiClients.SSO;
using Bolt.Automation.ApiClients.StsApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bolt.Automation.ApiClients
{
    public static class ApiClientServiceExtensions
    {
        public static IServiceCollection AddApiClients(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<IScopedRequestHeaderCache, ScopedRequestHeaderCache>();
            services.AddGetQuoteApi(configuration);
            services.AddPlatformApi();
            services.AddCaseManagerApi(configuration);
            services.AddPartnerPortalApi(configuration);
            services.AddSsoApi();
            services.AddAdbxApi();
            services.AddStsApi();
            services.AddSingleton<RefitApiServiceLocator>();
            services.AddIntegrationHubApi(configuration);
            return services;
        }
    }
}
