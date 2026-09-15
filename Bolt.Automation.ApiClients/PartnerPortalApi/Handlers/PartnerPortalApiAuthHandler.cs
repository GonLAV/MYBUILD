using Bolt.Automation.Common.Logging.Core;

namespace Bolt.Automation.ApiClients.PartnerPortalApi.Handlers
{
    public class PartnerPortalApiAuthHandler: DelegatingHandler
    {
        private readonly IAutomationLogger _logger;
        public PartnerPortalApiAuthHandler(IAutomationLogger logger)
        {
            _logger = logger;
            InnerHandler = new HttpClientHandler();
        }


        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = await base.SendAsync(request, cancellationToken);
            await _logger.LogApiCallAsync(request, response, 320);
            return response;
        }

    }
}
