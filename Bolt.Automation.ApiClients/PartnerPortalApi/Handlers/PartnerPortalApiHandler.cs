using System.Diagnostics;
using Bolt.Automation.ApiClients.Infrastructure;
using Bolt.Automation.ApiClients.PartnerPortalApi.Entities.Login;
using Bolt.Automation.ApiClients.PartnerPortalApi.Interfaces;
using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;

namespace Bolt.Automation.ApiClients.PartnerPortalApi.Handlers
{
    public class PartnerPortalApiHandler : DelegatingHandler
    {
        private readonly IScopedRequestHeaderCache _scopedRequestHeaderCache;
        private readonly IScopeContext _scopeContext;
        private readonly IAutomationLogger _logger;
        private readonly IPartnerPortalAuthApi _authApi;

        public PartnerPortalApiHandler(
            IScopedRequestHeaderCache scopedRequestHeaderCache,
            IAutomationLogger logger,
            IScopeContext scopeContext,
            IPartnerPortalAuthApi authApi)
        {
            _scopeContext = scopeContext ?? throw new ArgumentNullException(nameof(scopeContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _scopedRequestHeaderCache = scopedRequestHeaderCache ?? throw new ArgumentNullException(nameof(scopedRequestHeaderCache));
            _authApi = authApi;
            InnerHandler = new HttpClientHandler();
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var tenant = _scopeContext.Get(ctx => ctx.Tenant).ToString();
            var user = _scopeContext.Get(ctx => ctx.CurrentUser);
            var headers = await _scopedRequestHeaderCache.GetOrAddHeadersAsync(
                async _ =>
                {
                    //var config = _options.Value;
                    var loginReq = new LoginRequest
                    {
                        Username = user.Username,
                        Password = user.Password,
                        Source = user.Source
                    };

                    var response = await _authApi.GetTokenAsync(tenant, user.Source, loginReq);

                    var token = response.EnsureSuccessContent().access_token;
                    if (token == null)
                    {
                        _logger.Info("Received null token from the authentication response.");
                    }
                    var result = new Dictionary<string, string>
                    {
                        ["Authorization"] = $"Bearer {token}",
                        ["Source"] = user.Source,
                        ["Tenant"] = tenant
                    };

                    return result;
                },
                "PartnerPortal",
                "Auth",
                tenant,
                user.Source,
                user.Username
            );

            foreach (var header in headers)
            {
                if (!request.Headers.Contains(header.Key))
                    request.Headers.Add(header.Key, header.Value);
            }

            var sw = Stopwatch.StartNew();
            var response = await base.SendAsync(request, cancellationToken);
            sw.Stop();
            await _logger.LogApiCallAsync(request, response, sw.ElapsedMilliseconds);
            return response;
        }

    }
}
