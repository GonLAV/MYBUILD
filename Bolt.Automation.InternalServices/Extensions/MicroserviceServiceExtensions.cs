using Automation.Configuration.InternalServices.BoltServices;
using Bolt.Automation.InternalServices.Common;
using Bolt.Automation.InternalServices.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bolt.Automation.InternalServices.Extensions
{
    public static class MicroserviceServiceExtensions
    {
        public static IServiceCollection AddMicroserviceInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<MicroserviceOptions>(configuration.GetSection(MicroserviceOptions.SectionKey));
            services.Configure<ServiceRegistrationOptions>(configuration.GetSection(ServiceRegistrationOptions.SectionKey));
            services.AddSingleton<ITokenManager, JwtTokenManager>();
            services.AddSingleton<IGenericMicroserviceClientFactory, GenericMicroserviceClientFactory>();
            services.AddSingleton<IServiceRegistrationService, ServiceRegistrationService>();
            services.AddSingleton(typeof(IMicroservice<>), typeof(MicroserviceHolder<>));
            return services;
        }

        public static IServiceCollection AddBoltMicroservice<T>(this IServiceCollection services)
            where T : class
        {
            services.AddScoped(serviceProvider =>
            {
                var microservice = serviceProvider.GetRequiredService<IMicroservice<T>>();
                return microservice.GetClient();
            });
            return services;
        }
    }
}
