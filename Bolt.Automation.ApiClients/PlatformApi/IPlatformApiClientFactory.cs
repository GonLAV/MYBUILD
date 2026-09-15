using Bolt.Automation.ApiClients.PlatformApi.Interfaces;

namespace Bolt.Automation.ApiClients.PlatformApi
{
    public interface IPlatformApiClientFactory
    {
        Task<IPlatformApi> CreateApiClientAsync();
    }
}
