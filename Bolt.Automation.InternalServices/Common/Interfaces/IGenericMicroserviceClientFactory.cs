using Bolt.Automation.Common.Context;

namespace Bolt.Automation.InternalServices.Common.Interfaces
{
    internal interface IGenericMicroserviceClientFactory
    {
        IGenericMicroserviceClient CreateMicroserviceClient(IAddressResolver addressResolver, IScopeContext scopeContext);
    }
}
