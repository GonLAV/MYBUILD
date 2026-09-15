using Bolt.Automation.Common.Context;
using Bolt.Automation.InternalServices.Common.Interfaces;
using Bolt.Automation.InternalServices.Context;
using Bolt.Automation.InternalServices.Extensions;
using Bolt.Automation.InternalServices.MultiConfiguration;
using Bolt.Microservices.Entities.Case;
using Bolt.Microservices.Entities.Consumer;
using Bolt.Microservices.Entities.Cryptography;
using Bolt.Microservices.Entities.Policy;
using Bolt.Microservices.Entities.Quote;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bolt.Automation.InternalServices
{
    public static class InternalServicesExtension
    {
        private static readonly IBoltConfigurationRegistry _boltConfigurationRegistry = new BoltConfigurationRegistry();

        public static IServiceCollection AddInternalServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<IScopeContext, AsyncScopeContext>();
            services.AddBoltHttpClients();
            services.AddMicroserviceInfrastructure(configuration);
            services.AddMultiConfigurationServices(_boltConfigurationRegistry, configuration);
            services.AddAuthenticationOptions(configuration);
            
            services.AddBoltMicroservice<ICryptographyService>();
            services.AddBoltMicroservice<IQuoteService>();
            services.AddBoltMicroservice<IPolicyService>();
            services.AddBoltMicroservice<IConsumerService>();
            services.AddBoltMicroservice<ICaseService>(); 
            services.AddDatabaseService();

            return services;
        }
    }
}
