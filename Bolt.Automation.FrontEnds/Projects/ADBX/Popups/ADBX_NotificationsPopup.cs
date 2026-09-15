using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.ADBX.Base;
using Microsoft.Playwright;

namespace Bolt.Automation.FrontEnds.Projects.ADBX.Popups
{
    public class ADBX_NotificationsPopup(IBrowserManager browserManager, IPageHelper pageHelper, IScopeContext scopeContext, bool validatePageReady = true, IAutomationLogger? logger = null)
        : ADBX_BasePopup(browserManager, pageHelper, scopeContext, validatePageReady, logger)
    {
        protected override string PopupIdentifier => "adbx";

        protected override string PopupName => "Notifications";


        public const string TitleLocator =
            "app-notifications-popup div.notification";


        public async Task DownloadExportFile(string notificationText, string timestamp)
        {
            var linkLocator = $"//div[contains(text(), '{timestamp}')]/preceding-sibling::div[contains(text(), '{notificationText}')]/a";
            await PageHelper.InteractWithElement(
                LocatorType.XPath,
                linkLocator,
                ElementAction.Click,
                new ElementInteractionOptions { Timeout = 2500 }
            );
        }

        public override async Task ValidatePageReady()
        {
            try
            {
                await Page.Locator($"css={TitleLocator}").WaitForAsync(new LocatorWaitForOptions
                {
                    State = WaitForSelectorState.Visible,
                    Timeout = 5000
                });
            }
            catch (TimeoutException)
            {
                throw;
            }
        }

    }
}
