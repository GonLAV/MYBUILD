using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.HQXConsumer.Base;
using Microsoft.Playwright;

namespace Bolt.Automation.FrontEnds.Projects.HQXConsumer.Pages
{
    public class HQXConsumer_OnlineBuyKickOutPage(IBrowserManager browserManager,
        IPageHelper pageHelper,
        IScopeContext scopeContext,
        bool validatePageReady = true,
        IAutomationLogger? logger = null)
        : HQXConsumerBase(browserManager, pageHelper, scopeContext, validatePageReady, logger)
    {
        protected override string PageIdentifier => "OnlineBuyBridge";
        protected override string PageName => "Online Buy Kick-Out Page";

        public ILocator TitleHeader => Page.Locator("app-online-buy .title-container h1");
        public ILocator QuoteNumberText => Page.Locator("app-online-buy .quoteNumber");
        public ILocator RetrieveQuoteLink => Page.Locator("app-online-buy a.button");
    }
}
