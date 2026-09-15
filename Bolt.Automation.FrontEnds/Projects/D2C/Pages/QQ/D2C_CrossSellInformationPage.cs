using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.FormData;
using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.D2C.Base;

namespace Bolt.Automation.FrontEnds.Projects.D2C.Pages
{
    public class D2C_CrossSellInformationPage(IBrowserManager browserManager, IPageHelper pageHelper, IScopeContext scopeContext, bool validatePageReady = true, IAutomationLogger? logger = null)
        : D2CBase(browserManager, pageHelper, scopeContext, validatePageReady, logger)
    {
        protected override string PageIdentifier => "cross-sell-information";
        protected override string PageName => "Cross Sell Information Page";
        public const string BundleBoxLocator = "//button[@ data-automation-data= 'cross-sell-information']";


        /// <summary>
        /// Default behavior: Clicks "Not Now" to skip the cross-sell offer
        /// </summary>
        public override async Task ClickContinue()
        {
            // Click "Not Now" button to skip cross-sell and continue flow
            var notNowButton = Page.Locator("button.skip-btn");

            if (await notNowButton.IsVisibleAsync())
            {
                await notNowButton.ClickAsync();
                _logger?.Debug($"Clicked 'Not Now' button on {PageName}");
            }
            else
            {
                _logger?.Warning($"'Not Now' button not found on {PageName}, attempting default continue");
                await base.ClickContinue();
            }
        }

        public virtual async Task SelectBundleBox()
        {
            try
            {
                await PageHelper.InteractWithElement(
                    LocatorType.XPath,
                    new LocatorSet { BundleBoxLocator },
                    ElementAction.Click);
                await Task.Delay(1000);
                _logger?.LogUiAction("Click", "BundleBox", "Bundle box clicked successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogException(ex, "Failed to select bundle box on {0}", PageName);
                throw;
            }
        }

    }
}