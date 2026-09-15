using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using D2CBase = Bolt.Automation.FrontEnds.Projects.D2C.Base.D2CBase;

namespace Bolt.Automation.FrontEnds.Projects.D2C.Pages
{
    public class D2C_QuoteConfirmationPage: D2CBase
    {
        protected override string PageIdentifier => "quote-confirmation";
        protected override string PageName => "quote confirmation Full Quote Page";
        private const string GettingTheBestQuoteLoaderLocator = "//app-loader[@class = 'ng-star-inserted']";


        public D2C_QuoteConfirmationPage(IBrowserManager browserManager, IPageHelper pageHelper, IScopeContext scopeContext, bool validatePageReady = true, IAutomationLogger? logger = null)
    : base(browserManager, pageHelper, scopeContext, validatePageReady, logger)
        {
            var loaderLocator = Page.Locator(GettingTheBestQuoteLoaderLocator);
            PageHelper.WaitForElementToDisappearAsync(loaderLocator, 60000, initialRetries: 5, retryDelay: 1000).GetAwaiter().GetResult();
            PageHelper.ValidatePageReadyAsync(PageIdentifier, PageName).GetAwaiter().GetResult();
        }

        public override async Task ValidatePageReady()
        {
            await PageHelper.ValidatePageReadyAsync(PageIdentifier, PageName, timeout: 40000);
            await CaptureApplicationIdsFromSessionStorageAsync();
        }
    }
}
