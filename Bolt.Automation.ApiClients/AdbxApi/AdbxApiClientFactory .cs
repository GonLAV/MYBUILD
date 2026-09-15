using System.Collections.Concurrent;
using System.Net.Http.Headers;
using Bolt.Automation.ApiClients.AdbxApi.Handlers;
using Bolt.Automation.ApiClients.AdbxApi.Interfaces;
using Bolt.Automation.Common.Context;
using Refit;

namespace Bolt.Automation.ApiClients.AdbxApi
{
    public class AdbxApiClientFactory(
        IHttpClientFactory httpClientFactory,
        IScopeContext scopeContext,
        AdbxApiHandler handler) : IAdbxApiClientFactory
    {
        private readonly IScopeContext _scopeContext = scopeContext ?? throw new ArgumentNullException(nameof(scopeContext));
        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        private readonly ConcurrentDictionary<string, IAdbxApi> _clients = new();
        private readonly AdbxApiHandler _handler = handler ?? throw new ArgumentNullException(nameof(handler));

        public async Task<IAdbxApi> CreateApiClientAsync()
        {
            // Get current user context for caching key
            var currentUser = _scopeContext.Get(x => x.CurrentUser)
                ?? throw new Exception("User is not specified on test level");

            var tenant = _scopeContext.Data.Tenant.ToString()
                ?? throw new Exception("Tenant is not specified on test level");

            var cacheKey = $"{tenant}_{currentUser.Username}";

            if (_clients.TryGetValue(cacheKey, out var existingClient))
            {
                return existingClient;
            }

            var token = await _handler.GetTokenFromStsAsync(currentUser);
            var currentUrlType = _scopeContext.Data.UrlDataCollection.AdbxApi;

            var httpClient = _httpClientFactory.CreateClient("AdbxApiClient");
            httpClient.BaseAddress = new Uri(currentUrlType.BaseUrl);

            var sessionToken = await AdbxApiHandler.GetCurrentSessionToken(httpClient, tenant, token);
            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", sessionToken);

            var apiClient = RestService.For<IAdbxApi>(httpClient);
            _clients.TryAdd(cacheKey, apiClient);

            return apiClient;
        }
    }
}
