using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.ADBX.Base;

namespace Bolt.Automation.FrontEnds.Projects.ADBX.Pages.Quotes
{
    public class ADBX_QuotesTabPage : ADBX_BasePage
    {
        protected override string PageIdentifier => "quotes";

        protected override string PageName => "Quotes Grid Page";

        public ADBX_QuotesTabPage(IBrowserManager browserManager, IPageHelper pageHelper, IScopeContext scopeContext, bool validatePageReady = true, IAutomationLogger? logger = null)
        : base(browserManager, pageHelper, scopeContext, validatePageReady, logger)
        {
            WaitForLoaderToDisappear().GetAwaiter().GetResult();
        }
    }
}
