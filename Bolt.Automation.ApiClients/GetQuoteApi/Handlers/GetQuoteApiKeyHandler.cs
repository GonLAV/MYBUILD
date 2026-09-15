using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;
using Automation.Configuration.ApiClients;
using Bolt.Automation.ApiClients.GetQuoteApi.Models.Authorization;
using Bolt.Automation.ApiClients.Infrastructure;
using Bolt.Automation.Common;
using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Microsoft.Extensions.Options;

namespace Bolt.Automation.ApiClients.GetQuoteApi.Handlers
{
    public class GetQuoteApiKeyHandler : DelegatingHandler
    {
        private readonly IOptions<GetQuoteApiOptions> _options;
        private readonly IScopeContext _scopeContext;
        private readonly IAutomationLogger _logger;
        private readonly IScopedRequestHeaderCache _scopedRequestHeaderCache;

        public GetQuoteApiKeyHandler(
            IOptions<GetQuoteApiOptions> options,
            IScopeContext scopeContext,
            IAutomationLogger logger,
            IScopedRequestHeaderCache scopedRequestHeaderCache) 
        {
            _options = options;
            _scopeContext = scopeContext ?? throw new ArgumentNullException(nameof(scopeContext));
            _logger = logger;
            _scopedRequestHeaderCache = scopedRequestHeaderCache ?? throw new ArgumentNullException(nameof(scopedRequestHeaderCache));
            InnerHandler = new HttpClientHandler();
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var headers = await _scopedRequestHeaderCache.GetOrAddHeadersAsync(
                async ctx =>
                {
                    var result = new Dictionary<string, string>();
                    var userData = ctx.Get(x => x.CurrentUser);
                    var tenant = ctx.Data.Tenant.ToString();
                    if (!string.IsNullOrEmpty(userData.ApiKey))
                    {
                        result["X-Api-Key"]= userData.ApiKey;
                    }

                    if (!string.IsNullOrEmpty(userData.AgentIdentity) && userData.SendAgentIdentity)
                    {
                        result["X-Agent-Identity"] = userData.AgentIdentity;
                    }

                    if (!string.IsNullOrEmpty(userData.ApiSource))
                    {
                        result["X-API-Source"] = userData.ApiSource;
                    }

                    var config = _options.Value;
          
                    if (tenant == nameof(Tenant.PROGRESSIVEPL) && !string.IsNullOrEmpty(userData.OAuthToken))
                    {
                        var token = await TokenCacheHelper.GetOrFetchTokenAsync(
                            $"GQ_{userData.OAuthToken}",
                            async () =>
                            {
                                _logger.Info("Getting auth token for Progressive Get Quote Api");
                                using var authClient = new HttpClient();
                                authClient.BaseAddress = new Uri(config.AuthBaseUrl!);
                                authClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", userData.OAuthToken);
                                authClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                                authClient.DefaultRequestHeaders.TransferEncodingChunked = false;

                                var authRequest = new HttpRequestMessage(HttpMethod.Post, "auth");
                                var sw = Stopwatch.StartNew();
                                var response = await authClient.SendAsync(authRequest, cancellationToken);
                                sw.Stop();
                                await response.Content.LoadIntoBufferAsync();
                                await _logger.LogApiCallAsync(authRequest, response, sw.ElapsedMilliseconds);
                                response.EnsureSuccessStatusCode();

                                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                                var result = JsonSerializer.Deserialize<AuthorizationTokenPGRResponseModel>(json);
                                var token = result?.Token;
                                if (token == null)
                                {
                                    _logger.Info("Received null token from the authentication response.");
                                }
                                var expiration = DateTime.UtcNow.AddMinutes(50); // or use result?.ExpiresIn if available
                                return (token, expiration);
                            });
                        if (!string.IsNullOrWhiteSpace(token))
                            result["Authorization"] = $"Bearer {token}";
                    }

                    return result;
                },
                "GQ", // domain identifier
                // Use the username from the current user for cache key
                _scopeContext.Get(x => x.CurrentUser)?.Username ?? "DefaultUser"
            );

            foreach (var kvp in headers)
            {
                if (kvp.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase))
                    request.Headers.Authorization = AuthenticationHeaderValue.Parse(kvp.Value);
                else if (!request.Headers.Contains(kvp.Key))
                    request.Headers.Add(kvp.Key, kvp.Value);
            }

            var sw = Stopwatch.StartNew();
            var response = await base.SendAsync(request, cancellationToken);
            sw.Stop();
            await _logger.LogApiCallAsync(request, response, sw.ElapsedMilliseconds);
            return response;
        }
    }
}