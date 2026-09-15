using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.ADBX.Base;
using Bolt.Automation.FrontEnds.Projects.ADBX.FormData;
using Bolt.Automation.FrontEnds.Projects.ADBX.Popups;
using Microsoft.Playwright;

namespace Bolt.Automation.FrontEnds.Projects.ADBX.Pages.Leads
{
    public record PolicyInfo(string PolicyNumber, string Product, string Carrier, string Premium);

    public class ADBX_LeadSummaryPage(IBrowserManager browserManager, IPageHelper pageHelper, IScopeContext scopeContext, bool validatePageReady = true, IAutomationLogger? logger = null)
        : ADBX_BasePage(browserManager, pageHelper, scopeContext, validatePageReady, logger)
    {
        protected override string PageIdentifier => "lead";

        protected override string PageName => "ADBX Lead Summary Page";

        private const string LatestPoliciesSection =
            "app-latest-list:has(h2)";
        private const string PolicyItemWrapper =
            "app-latest-item div.item-wrapper:not(.empty)";
        private const string PolicyNameSpan =
            "div.title span";
        private const string PolicyDetailRow =
            "div.details div.row";
        private const string PolicyDetailTitle =
            "span.detail-title";
        private const string PolicyDetailValue =
            "span.detail-value";

        public async Task NavigateCommnicationTab(string type)
        {
            await pageHelper.InteractWithField(ADBX_FieldNames.LeadCommunicationsTabs, type);
        }

        public async Task<ADBX_AddPolicyInformationPopUp> ClickOnAddPolicy()
        {
            await PageHelper.InteractWithField(ADBX_FieldNames.AddPolicyButton);
            await WaitForLoaderToDisappear();
            return new ADBX_AddPolicyInformationPopUp(BrowserManager, PageHelper, ScopeContext);
        }

        public async Task<List<PolicyInfo>> GetLatestPolicies()
        {
            // Scope to the policies app-latest-list to avoid hitting quotes/cases lists
            var section = Page.Locator(LatestPoliciesSection)
                .Filter(new LocatorFilterOptions { HasText = "Latest Policies" });

            var items = section.Locator(PolicyItemWrapper);
            await PageHelper.WaitForElementAsync(items.First, DefaultTimeout, waitForVisibility: true);

            var count = await items.CountAsync();
            var result = new List<PolicyInfo>();

            for (int i = 0; i < count; i++)
            {
                var item = items.Nth(i);
                var policyName = (await item.Locator(PolicyNameSpan).InnerTextAsync()).Trim();

                var rows = await item.Locator(PolicyDetailRow).AllAsync();
                var details = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                foreach (var row in rows)
                {
                    var key = (await row.Locator(PolicyDetailTitle).InnerTextAsync()).Trim().TrimEnd(':');
                    var val = (await row.Locator(PolicyDetailValue).InnerTextAsync()).Trim();
                    details[key] = val;
                }

                result.Add(new PolicyInfo(
                    PolicyNumber: policyName,
                    Product: details.GetValueOrDefault("Product", string.Empty),
                    Carrier: details.GetValueOrDefault("Carrier", string.Empty),
                    Premium: details.GetValueOrDefault("Premium", string.Empty)
                ));
            }

            _logger?.Info($"Found {result.Count} latest policies on lead summary page.");
            return result;
        }

        public async Task ClickSeeAllLatest(string latestEntititesName)
        {
            var locator = $"//h2[contains(text(), 'Latest {latestEntititesName}')]/following::a[contains(@class, 'see-all')]";
            await PageHelper.InteractWithElement(
                LocatorType.XPath,
                locator,
                ElementAction.Click);


            await WaitForLoaderToDisappear();
        }
    }
}
