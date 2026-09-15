using Bolt.Automation.TestDataProvider.Context;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bolt.Automation.TestDataProvider
{
    public static class TestDataProviderServiceExtensions
    {
        public static IServiceCollection AddTestDataProvider(this IServiceCollection services, IConfiguration configuration)
        {      
            services.AddScoped<TestContextAccessor>();

            return services;
        }
    }
}