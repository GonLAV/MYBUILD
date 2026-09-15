using Bolt.Automation.Common.Context;

namespace Bolt.Automation.ApiClients.Infrastructure
{
    public interface IScopedRequestHeaderCache
    {
        Task<IDictionary<string, string>> GetOrAddHeadersAsync(
             Func<IScopeContext, Task<IDictionary<string, string>>> builder,
             params string[] keyParts);
    }
}
