using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.Interview.Base;

namespace Bolt.Automation.FrontEnds.Projects.Interview.Pages
{
    // The shared Markets page (URL /MarketResults) — KLX CL Auto, Comparion PL Auto and the
    // PL agent property flow all land here. No form fields; the user clicks Get Quotes / Next to
    // advance, and button.Get.quotes is covered by the standard ContinueButton selector list in
    // InterviewBase.
    public class Product_MarketsPage : InterviewBase
    {
        // The summary banner and the per-product carrier count are the only readable state
        // on this page, so they live here rather than in the field registry.
        private const string ResultsSummaryLocator = ".search-params";
        private const string ProductTabHeadingLocator = "h6.heading";
        private const string CarrierCountLocatorFormat = "h6.heading:has-text(\"{0}\") span";

        public Product_MarketsPage(
            IBrowserManager browserManager,
            IPageHelper pageHelper,
            IScopeContext scopeContext,
            bool validatePageReady = true,
            IAutomationLogger? logger = null)
            : base(browserManager, pageHelper, scopeContext, validatePageReady, logger) { }

        protected override string PageIdentifier => "MarketResults";
        protected override string PageName => "Markets Page";

        /// <summary>Reads the "Results for: &lt;product&gt; in &lt;state&gt;" banner.</summary>
        public async Task<string> GetResultsSummary()
        {
            var text = await Page.Locator(ResultsSummaryLocator).First.TextContentAsync();
            var summary = text?.Trim() ?? string.Empty;
            // Info, not Debug: these reads back the test's business assertions, so the
            // actual values must reach the report even on a passing run.
            _logger?.Info($"Markets page summary: '{summary}'");
            return summary;
        }

        /// <summary>
        /// Parses the "(N carriers)" suffix from the product tab heading. Returns 0 when the
        /// product tab is absent, which is itself a meaningful assertion target.
        /// </summary>
        public async Task<int> GetCarrierCount(string product)
        {
            // Auto-wait on the tab strip as a whole; the requested product's tab is then
            // checked without waiting so an absent tab yields 0 instead of a timeout.
            await Page.Locator(ProductTabHeadingLocator).First.WaitForAsync();

            var locator = Page.Locator(string.Format(CarrierCountLocatorFormat, product));
            if (await locator.CountAsync() == 0)
            {
                _logger?.Info($"Markets page has no product tab for '{product}', carrier count is 0");
                return 0;
            }

            var text = await locator.First.TextContentAsync();
            var digits = new string((text ?? string.Empty).Where(char.IsDigit).ToArray());
            var count = digits.Length > 0 ? int.Parse(digits) : 0;
            _logger?.Info($"Markets page carrier count for '{product}': {count}");
            return count;
        }
    }
}
