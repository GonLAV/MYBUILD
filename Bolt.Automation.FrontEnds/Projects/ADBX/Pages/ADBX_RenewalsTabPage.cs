using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.ADBX.Base;

namespace Bolt.Automation.FrontEnds.Projects.ADBX.Pages
{
    public class ADBX_RenewalsTabPage : ADBX_BasePage
    {
        protected override string PageIdentifier => "renewals-policies";
        protected override string PageName => "Reneweals Page";

        public ADBX_RenewalsTabPage(IBrowserManager browserManager, IPageHelper pageHelper, IScopeContext scopeContext, bool validatePageReady = true, IAutomationLogger? logger = null)
            : base(browserManager, pageHelper, scopeContext, validatePageReady, logger)
        {
            WaitForLoaderToDisappear().GetAwaiter().GetResult();
        }

    }
}
