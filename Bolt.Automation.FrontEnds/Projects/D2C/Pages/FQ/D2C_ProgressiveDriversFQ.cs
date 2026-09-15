using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.D2C.Base;

namespace Bolt.Automation.FrontEnds.Projects.D2C.Pages
{
    public class D2C_ProgressiveDriversFQ : D2CBase
    {
        #region Page Properties
        protected override string PageIdentifier => "progressive-drivers";
        protected override string PageName => "progressive drivers Full Quote Page";
        private const string GettingTheBestQuoteLoaderLocator = "//app-loader[@class = 'ng-star-inserted']";

        #endregion
        public D2C_ProgressiveDriversFQ(IBrowserManager browserManager, IPageHelper pageHelper, IScopeContext scopeContext, bool validatePageReady = true, IAutomationLogger? logger = null)
            : base(browserManager, pageHelper, scopeContext, validatePageReady, logger)
        {
            var loaderLocator = Page.Locator(GettingTheBestQuoteLoaderLocator);
            PageHelper.WaitForElementToDisappearAsync(loaderLocator, 60000, initialRetries: 5, retryDelay: 1000).GetAwaiter().GetResult();
            PageHelper.ValidatePageReadyAsync(PageIdentifier, PageName).GetAwaiter().GetResult();
        }
    }
}
