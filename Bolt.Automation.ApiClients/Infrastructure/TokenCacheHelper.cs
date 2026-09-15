using System.Collections.Concurrent;

namespace Bolt.Automation.ApiClients.Infrastructure
{
    public static class TokenCacheHelper
    {
        private static readonly ConcurrentDictionary<string, TokenCacheEntry> _tokenCache = new();
        private static readonly SemaphoreSlim _lock = new(1, 1);

        public static async Task<string?> GetOrFetchTokenAsync(string key, Func<Task<(string? token, DateTime expiration)>> fetchToken)
        {
            if (_tokenCache.TryGetValue(key, out var cacheEntry) && cacheEntry.IsValid)
            {
                return cacheEntry.Token;
            }

            await _lock.WaitAsync();
            try
            {
                if (_tokenCache.TryGetValue(key, out cacheEntry) && cacheEntry.IsValid)
                {
                    return cacheEntry.Token;
                }

                var (token, expiration) = await fetchToken();
                if (!string.IsNullOrWhiteSpace(token))
                {
                    _tokenCache[key] = new TokenCacheEntry(token, expiration);
                }
                return token;
            }
            finally
            {
                _lock.Release();
            }
        }

        private record TokenCacheEntry(string? Token, DateTime Expiration)
        {
            public bool IsValid => !string.IsNullOrWhiteSpace(Token) && DateTime.UtcNow < Expiration;
        }
    }
}

