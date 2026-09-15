using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Exceptions;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.D2C.Base;
using Microsoft.Playwright;

namespace Bolt.Automation.FrontEnds.Projects.D2C.Popups
{
    public class D2C_AlternativeCarriersPopup(
        IBrowserManager browserManager,
        IPageHelper pageHelper,
        IScopeContext scopeContext,
        bool validatePageReady = true,
        IAutomationLogger? logger = null
    ) : D2CPopupBase(browserManager, pageHelper, scopeContext, validatePageReady, logger)
    {
        #region Locators
        private const string TitlePageLocator = "h1:has-text('Alternative carriers')";
        private const string SubTitleLocator = "h4:text('Non-OEM Parts Acknowledgment')";
        private const string ConfirmButtonLocator = "button[aria-label='Confirm']";
        #endregion

        protected override string PopupIdentifier => "carriers";
        protected override string PopupName => "Alternative Carriers Popup";

        public override async Task ClickContinue()
        {
            await ClickPopupContinue();
        }

        // Carriers-specific methods
        public async Task<string> GetPopupSubtitle()
        {
            var element = Page.Locator(SubTitleLocator);
            await element.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible
            });

            return await element.TextContentAsync() ?? string.Empty;
        }

        public async Task ClickConfirmButton()
        {
            await PageHelper.InteractWithElement(
                LocatorType.CSS,
                ConfirmButtonLocator,
                ElementAction.Click
            );
        }

        public override async Task ValidatePageReady()
        {
            // Wait for popup to appear (base implementation)
            await base.ValidatePageReady();
            
            // Additional validation: wait for title to be visible
            try
            {
                var titleLocator = Page.Locator(TitlePageLocator);
                await titleLocator.WaitForAsync(new LocatorWaitForOptions
                {
                    State = WaitForSelectorState.Visible,
                    Timeout = 5000
                });
            }
            catch (TimeoutException)
            {
                throw new PopupTimeoutException("Alternative Carriers", "title visible", 5000);
            }
        }
    }
}