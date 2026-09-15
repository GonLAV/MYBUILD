using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.ADBX.Base;

namespace Bolt.Automation.FrontEnds.Projects.ADBX.Pages.Policies
{
    public class ADBX_PoliciesTabPage : ADBX_BasePage
    {
        protected override string PageIdentifier => "policies";
        protected override string PageName => "Policies Grid Page";

        public ADBX_PoliciesTabPage(IBrowserManager browserManager, IPageHelper pageHelper, IScopeContext scopeContext, bool validatePageReady = true, IAutomationLogger? logger = null)
        : base(browserManager, pageHelper, scopeContext, validatePageReady, logger)
        {
            WaitForLoaderToDisappear().GetAwaiter().GetResult();
        }
    }
}
