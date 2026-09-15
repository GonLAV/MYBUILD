using System.Collections.Concurrent;
using Bolt.Automation.Common.Context;
using Refit;
namespace Bolt.Automation.ExternalServices.CasePortal.Infrastructure
{
    public class CasePortalApiClientFactory(
        IHttpClientFactory httpClientFactory, 
        IScopeContext scopeContext) : ICasePortalApiClientFactory
    {
        private readonly IScopeContext _scopeContext = scopeContext ?? throw new ArgumentNullException(nameof(scopeContext));
        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        private readonly ConcurrentDictionary<string, ICasePortalApi> _clients = new();

        public Task<ICasePortalApi> CreateApiClient()
        {
            var baseUrl = _scopeContext.Data.UrlDataCollection?.CasePortalApi?.BaseUrl;
            var apiKey = _scopeContext.Data.CurrentUser?.ApiKey;
            var tenant = _scopeContext.Data.Tenant.ToString();
            var cacheKey = $"{tenant}_{apiKey}";

            if (_clients.TryGetValue(cacheKey, out var existingClient))
            {
                return Task.FromResult(existingClient);
            }

            var httpClient = _httpClientFactory.CreateClient("CasePortalApiClient");
            httpClient.BaseAddress = new Uri(baseUrl);

            httpClient.DefaultRequestHeaders.Add("X-API-Key", apiKey);
            var api = RestService.For<ICasePortalApi>(httpClient);

            _clients.TryAdd(cacheKey, api);

            return Task.FromResult(api);
        }
    }
}
