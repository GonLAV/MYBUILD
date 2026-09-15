using Bolt.Automation.ApiClients.AdbxApi.Interfaces;

namespace Bolt.Automation.ApiClients.AdbxApi
{
    public interface IAdbxApiClientFactory
    {
        Task<IAdbxApi> CreateApiClientAsync();
    }
}
