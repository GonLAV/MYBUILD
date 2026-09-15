using Bolt.Automation.InternalServices.Common.Interfaces;

namespace Bolt.Automation.InternalServices.Common
{
    internal class MicroserviceAddressResolver(
        IServiceRegistrationService serviceRegistrationService,
        string? microserviceType)
        : IAddressResolver
    {
        public async Task<string> ResolveAddressAsync()
        {
            return await serviceRegistrationService.GetMicroserviceAddressAsync(microserviceType);
        }
    }
}
