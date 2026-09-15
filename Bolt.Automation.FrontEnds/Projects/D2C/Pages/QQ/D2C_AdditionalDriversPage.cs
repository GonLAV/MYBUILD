using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.D2C.Popups;
using Microsoft.Playwright;
using D2CBase = Bolt.Automation.FrontEnds.Projects.D2C.Base.D2CBase;

namespace Bolt.Automation.FrontEnds.Projects.D2C.Pages
{
    public class D2C_AdditionalDriversPage(
        IBrowserManager browserManager,
        IPageHelper pageHelper,
        IScopeContext scopeContext,
        bool validatePageReady = true,
        IAutomationLogger? logger = null
    ) : D2CBase(browserManager, pageHelper, scopeContext, validatePageReady, logger)
    {
        #region Locators
        private const string AddAnotherDriverBtnLocator = "//button//span[contains(text(),'Add another driver')]";
        private const string DriverCardLocator = "app-driver";
        private const string DriverErrorIconLocator = "img.error-driver";
        #endregion

        #region Toggle State Constants
        private static readonly string[] ActiveToggleStates = { "active", "on", "checked" };
        private static readonly string[] ActiveToggleColors = { "rgb(0, 191, 204)", "rgba(0, 191, 204, 1)" };
        #endregion

        protected override string PageIdentifier => "drivers";
        protected override string PageName => "Additional Drivers Page";

        public async Task<D2C_AddEditAnotherDriverPopUp> ClickOnSpecificDriverEditDetails(string driverName, int index = 0)
        {
            try
            {
                string editButtonSelector = $"(//app-driver//*[contains(text(),'{driverName}')]/following::button//*[text()='Edit details'])[1]";
                await PageHelper.InteractWithElement(
                    LocatorType.XPath,
                    editButtonSelector,
                    ElementAction.Click
                );

                return new D2C_AddEditAnotherDriverPopUp(browserManager, pageHelper, scopeContext, logger: logger);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to find edit button for driver {driverName}: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> IsSpecificDriverToggleSliderOn(string driverName, int? index = null)
        {
            var toggleSelector = BuildDriverToggleXPath(driverName, index);

            try
            {
                var element = Page.Locator(toggleSelector);
                if (await element.CountAsync() == 0)
                    return false;

                var classAttribute = await element.GetAttributeAsync("class") ?? "";
                var backgroundColor = await element.EvaluateAsync<string>("el => window.getComputedStyle(el).backgroundColor");

                return ActiveToggleStates.Any(state => classAttribute.Contains(state)) ||
                       ActiveToggleColors.Contains(backgroundColor);
            }
            catch
            {
                return false;
            }
        }

        private string BuildDriverToggleXPath(string driverName, int? index = null)
        {
            //string uppercaseDriverName = driverName.ToUpper();
            return index.HasValue
                ? $"(//div[contains(text(),'{driverName}')]//following::toggle-slider//span)[{index.Value}]"
                : $"//app-driver[.//span[contains(text(),'{driverName}')]]//toggle-slider//span[contains(@class,'slider')]";
        }

        public async Task<D2C_AddEditAnotherDriverPopUp> ClickOnAddAnotherDriver()
        {
            await PageHelper.InteractWithElement(
                LocatorType.XPath,
                AddAnotherDriverBtnLocator,
                ElementAction.Click
            );

            return new D2C_AddEditAnotherDriverPopUp(BrowserManager, PageHelper, ScopeContext, logger: _logger);
        }

        public async Task<bool> IsAddAnotherDriverEnabled()
        {
            var disabledButton = Page.Locator("//button[.//span[normalize-space()='Add another driver']]");
            return await IsButtonEnabled(disabledButton);
        }

        public async Task<List<string>> GetAllDrivers()
        {
            List<string> driversList = [];

            var driverNames = Page.Locator("div.accordion-block.driver-accordion");

            var count = await driverNames.CountAsync();
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    var text = await driverNames.Nth(i).TextContentAsync();
                    driversList.Add(text.Trim());
                }

                return driversList;
            }

            return null;
        }

        /// <summary>
        /// 1-based positions of the drivers showing a validation error. Empty means none.
        /// </summary>
        public async Task<List<int>> CheckDriversValidationError()
        {
            List<int> driversList = [];
            var drivers = Page.Locator(DriverCardLocator);

            var count = await drivers.CountAsync();
            for (int i = 0; i < count; i++)
            {
                // Scoped to each driver's own card. The old locator indexed error icons across the
                // whole page, so it reported which *icon* it found, not which *driver* was invalid -
                // an error on driver 2 alone came back as position 1.
                var errorElement = drivers.Nth(i).Locator(DriverErrorIconLocator);
                if (await errorElement.CountAsync() > 0)
                {
                    driversList.Add(i + 1);
                }
            }

            _logger?.Debug("Drivers showing a validation error: [{0}] of {1}", string.Join(", ", driversList), count);
            return driversList;
        }

        /// <summary>
        /// Waits for every driver validation error to clear, so a caller asserting on
        /// <see cref="CheckDriversValidationError"/> is not racing the re-render after a save.
        /// Returns quietly on timeout and leaves the assertion to the caller.
        /// </summary>
        public async Task WaitForNoDriverValidationErrors(int timeoutMs = 10000)
        {
            try
            {
                // Scoped to the driver cards for the same reason CheckDriversValidationError is:
                // an unscoped 'img.error-driver' would wait on icons this page never reports on,
                // burning the whole timeout while the check itself already returns empty.
                await Page.Locator(DriverCardLocator)
                    .Locator(DriverErrorIconLocator)
                    .First
                    .WaitForAsync(new LocatorWaitForOptions
                    {
                        State = WaitForSelectorState.Detached,
                        Timeout = timeoutMs
                    });
            }
            catch (PlaywrightException)
            {
                // Microsoft.Playwright.TimeoutException derives from PlaywrightException.
                _logger?.Warning("Driver validation errors were still present after {0}ms", timeoutMs);
            }
        }
    }
}
