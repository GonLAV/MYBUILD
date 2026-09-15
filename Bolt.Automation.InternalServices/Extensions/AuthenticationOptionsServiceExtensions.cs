using Automation.Configuration.InternalServices.BoltServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bolt.Automation.InternalServices.Extensions
{
    public static class AuthenticationOptionsServiceExtensions
    {
        public static IServiceCollection AddAuthenticationOptions(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<AuthenticationOptions>(configuration.GetSection(AuthenticationOptions.SectionKey));
            return services;
        }
    }
}

