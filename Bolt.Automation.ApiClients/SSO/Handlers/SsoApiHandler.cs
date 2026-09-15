using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Bolt.Automation.ApiClients.SSO.Saml;
using Bolt.Automation.Common;
using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Enums;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.Common.Models.RelayStates;
using Bolt.Automation.Common.Models.Urls;
using Bolt.Automation.Common.Models.Users;
using Bolt.Microservices.Entities.Cryptography;

namespace Bolt.Automation.ApiClients.SSO.Handlers
{
    internal class SsoApiHandler : DelegatingHandler
    {
        private readonly IAutomationLogger _logger;
        private readonly string _tenant;
        private readonly IScopeContext _scopeContext;
        private readonly SsoHelper _ssoHelper;
        private readonly ICryptographyService _cryptographyService;

        public SsoApiHandler(
            IScopeContext scopeContext,
            IAutomationLogger logger,
            ICryptographyService cryptographyService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _scopeContext = scopeContext ?? throw new ArgumentNullException(nameof(scopeContext));
            InnerHandler = new HttpClientHandler();
            _ssoHelper = new SsoHelper();
            _cryptographyService = cryptographyService ?? throw new ArgumentNullException(nameof(cryptographyService));
            _tenant = _scopeContext.Data.Tenant.ToString() ?? throw new ArgumentNullException("Tenant is null");
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var user = _scopeContext.Get(x => x.CurrentUser);
            var url = _scopeContext.Data.UrlDataCollection.SsoApi;
            var relayState = _scopeContext.Get(x => x.CurrentRelayStateType);

            var originalUri = request.RequestUri;
            var tenantBaseUri = new Uri(url.BaseUrl);
            request.RequestUri = new Uri(tenantBaseUri, originalUri.PathAndQuery);

            var signedSaml = PrepareSsoSaml(user, url, relayState);
            request.Content = new StringContent(signedSaml, null, "application/x-www-form-urlencoded");
            var response = await base.SendAsync(request, cancellationToken);
            await _logger.LogApiCallAsync(request, response, 320);
            var responseContent = response.Content.ReadAsStringAsync(cancellationToken).Result;

            if (responseContent.Contains("Error"))
            {
                throw new Exception($"Error: {responseContent}");
            }

            if (responseContent.Contains("script type='text/javascript'"))
            {
                Match match = Regex
                    .Match(responseContent, @"window\.parent\.location\s*=\s*['""](https?://[^'""]+)['""];", RegexOptions.IgnoreCase);
                var redirectUrl = match.Groups[1].Value;
                var jsonResponse = new { RedirectUrl = redirectUrl };
                var json = JsonSerializer.Serialize(jsonResponse);
                response.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }

            return response;
        }

        private string PrepareSsoSaml(UserTestData user, UrlTestData url, RelayStateTestData relayState)
        {
            var samlTemplateType = _scopeContext.Get(ctx => ctx.SamlTemplate) ?? SamlTemplateType.Saml2ResponseTemplate;
            var samlTemplate = samlTemplateType.ToString();

            var saml = _tenant == nameof(Tenant.BOLTAG)
                ? _ssoHelper.GetSsoSignedSaml(user, url, relayState, samlTemplate, _cryptographyService)
                : _ssoHelper.GetSsoSignedSaml(user, url, relayState, samlTemplate);

            return saml;
        }
    }
}
