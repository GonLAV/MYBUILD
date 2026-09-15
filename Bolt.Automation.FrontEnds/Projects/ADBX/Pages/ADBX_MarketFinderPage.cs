using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.ADBX.Base;

namespace Bolt.Automation.FrontEnds.Projects.ADBX.Pages
{
    public class ADBX_MarketFinderPage : ADBX_BasePage
    {
        protected override string PageIdentifier => "market-finder";
        protected override string PageName => "Market Finder Page";
        private const string MapLocator = "div.map-wrapper";

        public ADBX_MarketFinderPage(IBrowserManager browserManager, IPageHelper pageHelper, IScopeContext scopeContext, bool validatePageReady = true, IAutomationLogger? logger = null)
            : base(browserManager, pageHelper, scopeContext, validatePageReady, logger)
        {
            PageHelper.WaitForElementAsync(Page.Locator("//h1[contains(text(),'Market Finder')]")).GetAwaiter().GetResult();
        }

        public async Task<bool> IsMarketMapDisplayed()
        {
            return await PageHelper.ElementExists(LocatorType.CSS, MapLocator);
        }

    }
}
