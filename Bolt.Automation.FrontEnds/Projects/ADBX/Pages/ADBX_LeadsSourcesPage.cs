using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.ADBX.Base;
using static Bolt.Automation.FrontEnds.Projects.ADBX.FormData.ADBX_FieldNames;

namespace Bolt.Automation.FrontEnds.Projects.ADBX.Pages
{
    /// <summary>Admin &gt; Leads Sources grid (/leads-sources).</summary>
    public class ADBX_LeadsSourcesPage(IBrowserManager browserManager, IPageHelper pageHelper, IScopeContext scopeContext, bool validatePageReady = true, IAutomationLogger? logger = null)
        : ADBX_BasePage(browserManager, pageHelper, scopeContext, validatePageReady, logger)
    {
        protected override string PageIdentifier => "leads-sources";
        protected override string PageName => "Leads Sources Grid Page";

        /// <summary>
        /// Filters the grid to <paramref name="leadSourceName"/> and opens it. Searching first is what
        /// makes the name enough: a hub can hold more sources than one page shows, and the source's id
        /// - the only other handle - is different in every environment.
        /// </summary>
        public async Task<ADBX_UpdateLeadSourcePage> OpenLeadSourceByNameAsync(string leadSourceName, IPageFactory pageFactory)
        {
            _logger?.Info($"Filtering the Leads Sources grid to '{leadSourceName}'.");
            await PageHelper.InteractWithField(LeadSourcesSearch, leadSourceName);

            _logger?.Info($"Opening lead source '{leadSourceName}'.");
            await PageHelper.InteractWithField(LeadSourcesGridEditIcon, leadSourceName);

            return pageFactory.CreatePage<ADBX_UpdateLeadSourcePage>();
        }
    }
}
