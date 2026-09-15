using Microsoft.Extensions.DependencyInjection;
using Refit;

namespace Bolt.Automation.ExternalServices.CasePortal.Infrastructure
{
    public static class CasePortalExtensions
    {
        public static IServiceCollection AddCasePortal(this IServiceCollection services)
        {
            services.AddRefitClient<ICasePortalApi>();
            services.AddScoped<ICasePortalApiClientFactory, CasePortalApiClientFactory>();
            services.AddScoped<CasePortalApiHandler>();
            services.AddHttpClient("CasePortalApiClient")
                .AddHttpMessageHandler<CasePortalApiHandler>();
            return services;
        }
    }
}
