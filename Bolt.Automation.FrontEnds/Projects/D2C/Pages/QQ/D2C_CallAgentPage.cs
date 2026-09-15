using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.D2C.Base;
using Bolt.Automation.FrontEnds.Projects.D2C.Popups;

namespace Bolt.Automation.FrontEnds.Projects.D2C.Pages
{
    public class D2C_CallAgentPage(IBrowserManager browserManager, IPageHelper pageHelper,IScopeContext scopeContext, bool validatePageReady = true, IAutomationLogger? logger = null) 
        : D2CBase(browserManager, pageHelper, scopeContext, validatePageReady, logger)
    {
        private const string ViewCoverageButtonLocator = "//button[@aria-label='View coverage']";

        protected override string PageIdentifier => "call-agent";
        protected override string PageName => "Call Agent Page";

        public async Task<D2C_CoverageDetailsPopUp> ClickOnViewCoverageButton()
        {
            await PageHelper.InteractWithElement(
                LocatorType.XPath,
                ViewCoverageButtonLocator,
                ElementAction.Click
            );

            return new D2C_CoverageDetailsPopUp(BrowserManager, PageHelper,scopeContext);
        }
    }
}