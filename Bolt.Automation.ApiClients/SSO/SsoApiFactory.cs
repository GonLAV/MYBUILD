using Bolt.Automation.ApiClients.SSO.Handlers;
using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Microservices.Entities.Cryptography;
using Refit;

namespace Bolt.Automation.ApiClients.SSO
{
    public class SsoApiFactory : ISsoApiFactory
    {
        //private readonly IOptions<SsoApiOptions> _options;
        private readonly IAutomationLogger _logger;
        private readonly IScopeContext _scopeContext;
        private readonly ICryptographyService _cryptographyService;

        public SsoApiFactory(
            //IOptions<SsoApiOptions> options,
            IScopeContext scopeContext,
            IAutomationLogger logger,
            ICryptographyService cryptographyService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            //_options = options;
            _cryptographyService = cryptographyService ?? throw new ArgumentNullException(nameof(cryptographyService));
            _scopeContext = scopeContext;
        }

        public ISsoApi CreateClient()
        {
            var tenant = _scopeContext.Data.Tenant
                ?? throw new Exception("Tenant is not specified on test level");

            var url = _scopeContext.Data.UrlDataCollection.SsoApi;
            if (string.IsNullOrEmpty(url.BaseUrl))
            {
                throw new InvalidOperationException($"BaseUrl for tenant '{tenant}' is null or empty.");
            }

            var httpClient = new HttpClient(new SsoApiHandler(
                _scopeContext,
                _logger,
                _cryptographyService))
            {
                BaseAddress = new Uri(url.BaseUrl)
            };

            return RestService.For<ISsoApi>(httpClient);
        }
    }
}
