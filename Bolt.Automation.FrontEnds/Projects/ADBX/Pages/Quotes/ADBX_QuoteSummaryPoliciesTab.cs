using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.ADBX.Base;
using Bolt.Automation.FrontEnds.Projects.ADBX.FormData;
using Bolt.Automation.FrontEnds.Projects.ADBX.Popups;

namespace Bolt.Automation.FrontEnds.Projects.ADBX.Pages.Quotes
{
    public class ADBX_QuoteSummaryPoliciesTab(IBrowserManager browserManager, IPageHelper pageHelper, IScopeContext scopeContext, bool validatePageReady = true, IAutomationLogger? logger = null)
        : ADBX_BasePage(browserManager, pageHelper, scopeContext, validatePageReady, logger)
    {
        protected override string PageIdentifier => "quotes policies";
        protected override string PageName => "Quote Summary Policies Tab";

        public async Task<ADBX_AddPolicyInformationPopUp> ClickOnAddPolicy()
        {
            await PageHelper.InteractWithField(ADBX_FieldNames.AddPolicyButton);
            await WaitForLoaderToDisappear();
            return new ADBX_AddPolicyInformationPopUp(BrowserManager, PageHelper, scopeContext);
        }

    }
}
