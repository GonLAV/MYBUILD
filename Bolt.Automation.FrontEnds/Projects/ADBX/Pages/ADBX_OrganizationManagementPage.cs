using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.ADBX.Base;
using Bolt.Automation.FrontEnds.Projects.ADBX.Popups;

namespace Bolt.Automation.FrontEnds.Projects.ADBX.Pages
{
    public class ADBX_OrganizationManagementPage(IBrowserManager browserManager, IPageHelper pageHelper, IScopeContext scopeContext, bool validatePageReady = true, IAutomationLogger? logger = null)
        : ADBX_BasePage(browserManager, pageHelper, scopeContext, validatePageReady, logger)
    {
        protected override string PageIdentifier => "organization-management";
        protected override string PageName => "ADBX Organization Management Page";

        private const string AgencyAppointmentsModal = "//app-agency-direct-appointments-modal";
        private const string EmailMfaToggleSwitch = "//app-slide-toggle[@formcontrolname='email']//button[@role='switch']";

        public async Task ClickOnManagementItem(string itemName)
        {
            var locator = $"//app-management-page-item//h3[contains(text(),'{itemName}')]";
            await PageHelper.InteractWithElement(
                LocatorType.XPath,
                locator,
                ElementAction.Click,
                new ElementInteractionOptions { Timeout = 4000 }
            );
        }

        public async Task DismissAppointmentsPopupIfPresent()
        {
            if (!await PageHelper.ElementExists(LocatorType.XPath, AgencyAppointmentsModal, 2000))
            {
                _logger?.Debug("No appointments popup found on Organization Management page.");
                return;
            }

            _logger?.Debug("Appointments popup found on Organization Management page, dismissing it.");
            await PageHelper.InteractWithElement(
                LocatorType.CSS,
                "button.close",
                ElementAction.Click
            );
        }

        public async Task ExpandSection(string sectionName)
        {
            var locator = $"//p[contains(text(), '{sectionName}')]/preceding-sibling::button[@class='toggle-section-button']";

            await PageHelper.InteractWithElement(
                LocatorType.XPath,
                locator,
                ElementAction.Click,
                new ElementInteractionOptions { Timeout = 2000 }
            );
        }

        public async Task<bool> IsEmailMfaEnabled()
        {
            var ariaChecked = await Page.Locator(EmailMfaToggleSwitch).GetAttributeAsync("aria-checked");
            return ariaChecked == "true";
        }

        public async Task SetEmailMfaToggle(bool enable)
        {
            if (await IsEmailMfaEnabled() == enable)
            {
                _logger?.Debug($"Email MFA toggle is already {(enable ? "enabled" : "disabled")}.");
                return;
            }

            _logger?.Debug($"Setting Email MFA toggle to {(enable ? "enabled" : "disabled")}.");
            await PageHelper.InteractWithElement(
                LocatorType.XPath,
                EmailMfaToggleSwitch,
                ElementAction.Click
            );
        }

        public async Task ClickConfirmForSection(string section)
        {
            var locator = $"//div[@data-automation='{section}']//button[contains(text(),'Confirm')]";
            await PageHelper.InteractWithElement(
                LocatorType.XPath,
                locator,
                ElementAction.Click
            );
        }
    }
}
