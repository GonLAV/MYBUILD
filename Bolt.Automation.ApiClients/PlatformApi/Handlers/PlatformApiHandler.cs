using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Bolt.Automation.ApiClients.Infrastructure;
using Bolt.Automation.ApiClients.PlatformApi.Entities.Common;
using Bolt.Automation.Common;
using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;

namespace Bolt.Automation.ApiClients.PlatformApi.Handlers
{
    public class PlatformApiHandler : DelegatingHandler
    {
        public const string AuthRequestBodyDefault = "grant_type=client_credentials&scope=QuoteRetrieval QuoteStart QuoteStatus DeepLink CrmNote Ping GroupUserBatch Provision+++++ ";

        private readonly IScopeContext _scopeContext;
        private readonly IAutomationLogger _logger;

        public PlatformApiHandler(
            IScopeContext scopeContext,
            IAutomationLogger logger)
        {
            _scopeContext = scopeContext ?? throw new ArgumentNullException(nameof(scopeContext));
            _logger = logger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var tenant = _scopeContext.Data.Tenant.ToString();
            var user = _scopeContext.Get(x => x.CurrentUser);

            var currentUrlType = _scopeContext.Data.UrlDataCollection.PlatformApi;

            if (tenant == nameof(Tenant.PROGRESSIVEPL))
            {
                //_logger.Info("Auth needed for platform api");
                var cacheKey = $"{tenant}_{user.OAuthToken}";
                var headerValue = await TokenCacheHelper.GetOrFetchTokenAsync(user.Username, async () =>
                {
                    if (string.IsNullOrWhiteSpace(currentUrlType.BaseUrl))
                        throw new InvalidOperationException($"No BaseUrl configured for tenant: {tenant}");
                    using var client = new HttpClient { BaseAddress = new Uri(currentUrlType.BaseUrl) };
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", user.OAuthToken);
                    var content = new StringContent(AuthRequestBodyDefault, Encoding.UTF8, "application/x-www-form-urlencoded");
                    var response = await client.PostAsync("/auth", content, cancellationToken);
                    response.EnsureSuccessStatusCode();
                    var json = await response.Content.ReadAsStringAsync(cancellationToken);
                    var auth = JsonSerializer.Deserialize<AuthResponse>(json);
                    var token = auth?.access_token;
                    if (token == null)
                    {
                        _logger.Info("Received null token from the authentication response.");
                    }
                    var expiration = DateTime.UtcNow.AddSeconds(auth?.expires_in ?? 3600);
                    return ($"Bearer {token}", expiration);
                });

                request.Headers.TryAddWithoutValidation("Authorization", headerValue);
              
            }
            else
            {
                request.Headers.TryAddWithoutValidation("X-Bolt-ApiKey", user.ApiKey);
                request.Headers.TryAddWithoutValidation("X-Bolt-Tenant", tenant);
            }

            var sw = Stopwatch.StartNew();
            var response = await base.SendAsync(request, cancellationToken);
            sw.Stop();
            await _logger.LogApiCallAsync(request, response, sw.ElapsedMilliseconds);
            return response;
        }
    }
}
