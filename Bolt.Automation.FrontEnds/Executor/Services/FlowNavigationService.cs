using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Exceptions;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;

namespace Bolt.Automation.FrontEnds.Executor.Services
{
    /// <summary>
    /// Service responsible for flow navigation and environment management with lazy flow loading
    /// </summary>
    public class FlowNavigationService : IFlowNavigationService
    {
        private readonly IBrowserManager _browserManager;
        private readonly IScopeContext _scopeContext;
        private readonly IAutomationLogger? _logger;

        public FlowNavigationService(
            IBrowserManager browserManager,
            IScopeContext scopeContext,
            IAutomationLogger? logger = null)
        {
            _browserManager = browserManager ?? throw new ArgumentNullException(nameof(browserManager));
            _scopeContext = scopeContext ?? throw new ArgumentNullException(nameof(scopeContext));
            _logger = logger;
        }

        /// <summary>
        /// Ensures the browser is on the start page for the flow
        /// </summary>
        public async Task EnsureOnStartPage(Enum flowType)
        {
            var startUrl = _scopeContext.Get(ctx => ctx.CurrentUrl);
            if (string.IsNullOrEmpty(startUrl))
                throw new TestSetupException($"No start URL configured for flow '{flowType}'.");

            _logger?.Info($"Navigating to flow start URL: {startUrl}");
            await _browserManager.NavigateAsync(startUrl);
        }

        public async Task EnsureOnStartPage(Enum flowType, string startUrl)
        {
            if (string.IsNullOrEmpty(startUrl))
                throw new ArgumentException("Start URL cannot be null or empty.", nameof(startUrl));

            _logger?.Info($"Navigating to custom start URL for flow '{flowType}': {startUrl}");
            await _browserManager.NavigateAsync(startUrl);
        }
    }
}