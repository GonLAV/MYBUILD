using System.Diagnostics;
using Bolt.Automation.ApiClients.Infrastructure;
using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;

namespace Bolt.Automation.ApiClients.CaseManagerApi.Handlers
{
    public class CaseManagerApiKeyHandler : DelegatingHandler
    {
        private readonly IScopeContext _scopeContext;
        private readonly IAutomationLogger _logger;
        private readonly IScopedRequestHeaderCache _scopedRequestHeaderCache;

        public CaseManagerApiKeyHandler(
            IScopeContext scopeContext,
            IAutomationLogger logger,
            IScopedRequestHeaderCache scopedRequestHeaderCache)
        {
            _scopeContext = scopeContext ?? throw new ArgumentNullException(nameof(scopeContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _scopedRequestHeaderCache = scopedRequestHeaderCache ?? throw new ArgumentNullException(nameof(scopedRequestHeaderCache));
            InnerHandler = new HttpClientHandler();
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var headers = await _scopedRequestHeaderCache.GetOrAddHeadersAsync(
                 ctx =>
                {
                    // Use the strongly-typed context to get the current user from the provided context
                    var user = ctx.Get(x => x.CurrentUser);

                    // DEBUG: Log what we have in context for CurrentUser
                   _logger.Debug($"[DEBUG] CurrentUser in context: {user?.Role.ToString() ?? "null"} | Type: {user?.GetType().FullName ?? "null"}");

                    // Use the username or another property from UserTestData as the user key
                    var userKey = user?.Username ?? "DefaultUser";
                    var result = new Dictionary<string, string>();

                    if (!string.IsNullOrWhiteSpace(user?.ApiKey))
                        result["X-Api-Key"] = user.ApiKey;

                    if (!string.IsNullOrWhiteSpace(user?.UserExternalId))
                        result["X-UserExternalId"] = user.UserExternalId;

                    // Log the final headers
                    _logger.Debug($"[DEBUG] Headers to be sent: {{ {string.Join(", ", result.Select(kvp => kvp.Key + ": " + kvp.Value))} }}");

                    return Task.FromResult<IDictionary<string, string>>(result);
                },
                "CaseManager",
                // Use the username from the current user for cache key
                _scopeContext.Get(x => x.CurrentUser)?.Role.ToString() ?? "DefaultUserRole"
            );

            foreach (var kvp in headers)
            {
                if (!request.Headers.Contains(kvp.Key))
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
