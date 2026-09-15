using Automation.Configuration.InternalServices.BoltServices;
using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.InternalServices.Common.Interfaces;
using Microsoft.Extensions.Options;

namespace Bolt.Automation.InternalServices.Common
{
    internal class GenericMicroserviceClientFactory(
        IOptions<MicroserviceOptions> microserviceOptions,
        IHttpClientFactory clientFactory,
        ITokenManager tokenManager,
        IAutomationLogger logger)
        : IGenericMicroserviceClientFactory
    {
        private readonly MicroserviceOptions _microserviceOptions = microserviceOptions.Value;

        public IGenericMicroserviceClient CreateMicroserviceClient(IAddressResolver addressResolver, IScopeContext scopeContext)
        {
            ArgumentNullException.ThrowIfNull(scopeContext);

            var selfAddress = _microserviceOptions.Address;
            return new GenericMicroserviceClient(addressResolver, selfAddress, clientFactory, scopeContext, tokenManager, logger);
        }
    }
}
