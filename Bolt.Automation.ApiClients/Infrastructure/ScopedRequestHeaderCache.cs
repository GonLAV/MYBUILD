using Bolt.Automation.Common.Context;

namespace Bolt.Automation.ApiClients.Infrastructure
{
    public class ScopedRequestHeaderCache(IScopeContext scopeContext) : IScopedRequestHeaderCache
    {
        private const string CachePrefix = "AuthHeaderCache:";

        public async Task<IDictionary<string, string>> GetOrAddHeadersAsync(
     Func<IScopeContext, Task<IDictionary<string, string>>> builder,
     params string[] keyParts)
        {
            var cacheKey = CachePrefix + string.Join("|", keyParts);
            if (scopeContext.TryGetTestData<IDictionary<string, string>>(cacheKey, out var cached))
            {
                return cached!;
            }

            var headers = await builder(scopeContext);
            scopeContext.SetTestData(cacheKey, headers);
            return headers;
        }
    }
}
