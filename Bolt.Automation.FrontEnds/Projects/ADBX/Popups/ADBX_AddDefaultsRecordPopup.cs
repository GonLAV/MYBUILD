using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.ADBX.Base;
using Microsoft.Playwright;

namespace Bolt.Automation.FrontEnds.Projects.ADBX.Popups
{
    public class ADBX_AddDefaultsRecordPopup(IBrowserManager browserManager, IPageHelper pageHelper, IScopeContext scopeContext, bool validatePageReady = true, IAutomationLogger? logger = null)
        : ADBX_BasePopup(browserManager, pageHelper, scopeContext, validatePageReady, logger)
    {
        protected override string PopupIdentifier => "defaults-management";
        protected override string PopupName => "Add Defaults Record";
        private const string ClearButtonLocator = "app-add-edit-defaults-record button.reset-input-btn";
        public async Task ClearEditableFileds()
        {
            var locator = CreateLocator(ClearButtonLocator);
           
            await locator.First.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = 2000
            });

            var elements = await locator.AllAsync();

            foreach (var element in elements)
            {
                try
                {
                    await pageHelper.WaitForElementAsync(element, timeout: 5000, waitForVisibility: true);
                    await element.ClickAsync();
                }
                catch (Exception ex)
                {
                    _logger?.Warning($"Failed to click reset button: {ex.Message}");
                }
            }
        }
    }
}
