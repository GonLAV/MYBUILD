using System.Diagnostics;
using Bolt.Automation.Common.Logging.Core;

namespace Bolt.Automation.ApiClients.StsApi.Handlers
{
    internal class StsApiHandler(IAutomationLogger logger) : DelegatingHandler
    {
        private readonly IAutomationLogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var sw = Stopwatch.StartNew();
            var response = await base.SendAsync(request, cancellationToken);
            sw.Stop();
            await _logger.LogApiCallAsync(request, response, sw.ElapsedMilliseconds);
            return response;
        }
    }

}
