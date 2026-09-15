using Bolt.Automation.InternalServices.Common.Interfaces;

namespace Bolt.Automation.InternalServices.Common
{
    internal class StaticAddressResolver(string address) : IAddressResolver
    {
        public Task<string> ResolveAddressAsync()
        {
            return Task.FromResult(address);
        }
    }
}
