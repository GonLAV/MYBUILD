using System.Collections.Concurrent;
using Bolt.Automation.ApiClients.PlatformApi.Interfaces;
using Bolt.Automation.Common.Context;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Refit;

namespace Bolt.Automation.ApiClients.PlatformApi
{ 
    public class PlatformApiClientFactory(
        IHttpClientFactory httpClientFactory,
        IScopeContext scopeContext)
        : IPlatformApiClientFactory
    {
        private readonly ConcurrentDictionary<string, IPlatformApi> _clients = new();

        public Task<IPlatformApi> CreateApiClientAsync()
        {
            var tenant = scopeContext.Data.Tenant;
            var user = scopeContext.Get(x => x.CurrentUser);
            var cacheKey = $"{tenant}_{user?.Username ?? "default"}";

            if (_clients.TryGetValue(cacheKey, out var existingClient))
                return Task.FromResult(existingClient);

            var currentUrlType = scopeContext.Data.UrlDataCollection.PlatformApi;
            if (currentUrlType == null || string.IsNullOrWhiteSpace(currentUrlType.BaseUrl))
                throw new InvalidOperationException("CurrentUrlType with a valid BaseUrl must be set in the context before creating the PlatformApi client.");

            var httpClient = httpClientFactory.CreateClient("PlatformApiClient");
            httpClient.BaseAddress = new Uri(currentUrlType.BaseUrl);

            // Use the same Refit settings as configured in service extensions
            var refitSettings = new RefitSettings
            {
                ContentSerializer = new NewtonsoftJsonContentSerializer(new JsonSerializerSettings
                {
                    ContractResolver = new DefaultContractResolver(),
                    NullValueHandling = NullValueHandling.Ignore
                })
            };

            var apiClient = RestService.For<IPlatformApi>(httpClient, refitSettings);
            _clients.TryAdd(cacheKey, apiClient);
            return Task.FromResult(apiClient);
        }
    }
}
