using System.Text.RegularExpressions;
using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Exceptions;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.D2C.Popups;
using D2CBase = Bolt.Automation.FrontEnds.Projects.D2C.Base.D2CBase;

namespace Bolt.Automation.FrontEnds.Projects.D2C.Pages
{
    public class D2C_VehiclesPage(
        IBrowserManager browserManager,
        IPageHelper pageHelper,
        IScopeContext scopeContext,
        bool validatePageReady = true,
        IAutomationLogger? logger = null)
        : D2CBase(browserManager, pageHelper, scopeContext, validatePageReady, logger)
    {
        #region Locators
        private const string VehicleListLocator = "div app-vehicle";
        private const string DiscoveredVehicleNameLocator = "app-vehicle[source='discoverVehicles'] div.car-name";
        private const string AddAnotherCarBtnLocator = "//button//span[text()='Add another car']/ancestor::button";
        private const string FirstCarToggleLocator = "(//mat-slide-toggle)[1]";
        private const string SaveButtonLocator = "//button//span[contains(text(),'Save')]";
        private const string AddCarButtonLocator = "//button[@aria-label = 'Add car']";
        private const string VehicleCardByNameLocator = "app-vehicle:has(div.car-name:text-is('{0}'))";
        private const string VehicleErrorMarkLocator = "img.error-vehicle-mark";
        #endregion

        protected override string PageIdentifier => "vehicles";
        protected override string PageName => "Vehicles Page";

        public override async Task FillForm(Dictionary<string, string>? formData = null)
        {
            if (await IsContinueButtonDisabled())
            {
                await ClickOnFirstCarToggle();
                await base.FillForm(formData);
                await ClickOnSave();
            }
            else
            {
                await base.FillForm(formData);
            }
        }

        public async Task SwitchSpecificCarToggleSlider(string carName, bool isSwitchOn = true, int index = 0)
        {
            var onOrOff = isSwitchOn ? "unselected" : "checked";
            string toggleXPath = $"//div[contains(@class,'car-name') and contains(text(),'{carName}')]//following::button[contains(@class,'{onOrOff}')]/ancestor::mat-slide-toggle";

            var toggleElements = Page.Locator(toggleXPath);
            var count = await toggleElements.CountAsync();

            if (count > 0 && index < count)
            {
                await toggleElements.Nth(index).ClickAsync();
                return;
            }

            // Silence here used to surface much later as a confusing assertion on the car's state,
            // so say plainly that the toggle was never clicked.
            _logger?.Warning("No '{0}' toggle found for car [{1}] at index {2} ({3} match(es)); nothing was clicked",
                onOrOff, carName, index, count);
        }

        public async Task<D2C_AddEditAnotherCarPopUp> ClickOnSpecificCarEditBtn(string carFullname, int index = 0)
        {
            string editButtonXPath = $"//div[contains(@class,'car-name') and contains(text(),'{carFullname}')]//ancestor::div[@class='accordion-top']//button[contains(@class,'edit')]";

            await PageHelper.InteractWithElement(
                LocatorType.XPath,
                editButtonXPath,
                ElementAction.Click
            );

            return new D2C_AddEditAnotherCarPopUp(BrowserManager, PageHelper, ScopeContext, logger: _logger);
        }

        /// <summary>
        /// True when the app is flagging this car as incomplete. A found (discovered) vehicle whose
        /// details were never filled in gets an 'error-vehicle' class on its accordion plus an
        /// 'error-vehicle-mark' icon; the red "Edit details" text is only a CSS consequence of that
        /// class. Asserting on the marker keeps this tenant-agnostic - the error red is theme-driven,
        /// so matching a colour meant guessing at "red enough" - and unlike a colour, the marker is
        /// something a caller can wait on detaching once the car is completed.
        /// </summary>
        public async Task<bool> IsSpecificCarInErrorState(string carFullname)
        {
            // ':text-is' not ':has-text': the names come from GetDiscoveredVehicles already
            // whitespace-normalised, and a substring match would let "2007 LEXUS IS 250" also
            // answer for a trim level like "2007 LEXUS IS 250 AWD".
            var vehicleCard = Page.Locator(string.Format(VehicleCardByNameLocator, carFullname)).First;
            if (await vehicleCard.CountAsync() == 0)
            {
                _logger?.Warning("No vehicle card found for car [{0}]", carFullname);
                return false;
            }

            var isInError = await vehicleCard.Locator(VehicleErrorMarkLocator).CountAsync() > 0;
            _logger?.Debug("Car [{0}] validation error state: {1}", carFullname, isInError);

            return isInError;
        }

        /// <summary>
        /// Names of the vehicles the quote discovered for this driver, as opposed to the ones a
        /// test added by hand. Names come back whitespace-normalised because callers feed them
        /// straight back into a <c>contains(text(),'...')</c> locator, and the Angular template
        /// indents the name across lines - a raw Trim() leaves the inner newline in place and
        /// every later lookup for that car silently matches nothing.
        /// </summary>
        public async Task<List<string>> GetDiscoveredVehicles()
        {
            var discoveredVehicles = Page.Locator(DiscoveredVehicleNameLocator);
            var count = await discoveredVehicles.CountAsync();

            var result = new List<string>();
            for (int i = 0; i < count; i++)
            {
                var vehicleName = await discoveredVehicles.Nth(i).TextContentAsync();
                if (!string.IsNullOrWhiteSpace(vehicleName))
                    result.Add(Regex.Replace(vehicleName, @"\s+", " ").Trim());
            }

            _logger?.Debug("Discovered (found) vehicles on the page: [{0}]", string.Join(" | ", result));
            return result;
        }

        public async Task<D2C_AddEditAnotherCarPopUp> ClickOnAddAnotherCar()
        {
            await PageHelper.InteractWithElement(
                LocatorType.XPath,
                AddAnotherCarBtnLocator,
                ElementAction.Click
            );

            return new D2C_AddEditAnotherCarPopUp(BrowserManager, PageHelper, ScopeContext, logger: _logger);
        }

        public async Task<string> GetVehicleMessage()
        {
            var messageElement = Page.Locator("//div[contains(@class , 'error')]");

            if (await messageElement.CountAsync() > 0)
                return await messageElement.TextContentAsync() ?? "Not Found";

            return "Not Found";
        }

        public async Task<bool> IsAdditionalCarBtnEnabled()
        {
            var disabledButton = Page.Locator("div.add-car button[disabled]");
            return await disabledButton.CountAsync() == 0;
        }

        public async Task<List<string>> GetAllVehicles()
        {
            var vehicles = Page.Locator(VehicleListLocator);
            var count = await vehicles.CountAsync();

            if (count == 0)
                return new List<string>();

            var result = new List<string>();
            for (int i = 0; i < count; i++)
            {
                var vehicleElement = vehicles.Nth(i);
                var nameElement = vehicleElement.Locator("div.car-name");

                if (await nameElement.CountAsync() > 0)
                {
                    var vehicleName = await nameElement.TextContentAsync();
                    result.Add(vehicleName?.Trim() ?? string.Empty);
                }
            }

            return result;
        }

        public async Task<D2C_AddEditAnotherCarPopUp?> ClickOnFirstCarToggle()
        {
            try
            {
                var elementExists = await PageHelper.ElementExists(
                    LocatorType.XPath,
                    FirstCarToggleLocator
                );

                if (!elementExists)
                {
                    return null;
                }

                _logger?.LogUiAction("Click", "FirstCarToggle", "Clicking first vehicle toggle");

                await PageHelper.InteractWithElement(
                    LocatorType.XPath,
                    FirstCarToggleLocator,
                    ElementAction.Click
                );

                _logger?.LogUiAction("Click", "FirstCarToggle", "First vehicle toggle clicked successfully");
                return new D2C_AddEditAnotherCarPopUp(BrowserManager, PageHelper, ScopeContext, logger: _logger);
            }
            catch (Exception ex)
            {
                _logger?.LogException(ex, "Failed to click first car toggle");
                throw;
            }
        }

        public async Task ClickOnSave()
        {
            try
            {
                // Try the Save button first
                if (await PageHelper.ElementExists(LocatorType.XPath, SaveButtonLocator))
                {
                    await PageHelper.InteractWithElement(
                        LocatorType.XPath,
                        SaveButtonLocator,
                        ElementAction.Click
                    );
                    _logger?.LogUiAction("Click", "SaveButton", "Save button clicked successfully");
                }
                // Fallback to Add car button
                else if (await PageHelper.ElementExists(LocatorType.XPath, AddCarButtonLocator))
                {
                    await PageHelper.InteractWithElement(
                        LocatorType.XPath,
                        AddCarButtonLocator,
                        ElementAction.Click
                    );
                    _logger?.LogUiAction("Click", "AddCarButton", "Add car button clicked successfully");
                }
                // Fallback to Continue button
                else if (await PageHelper.ElementExists(LocatorType.XPath, "(//button//span[contains(text(),'Continue')])[1]"))
                {
                    await PageHelper.InteractWithElement(
                        LocatorType.XPath,
                        "(//button//span[contains(text(),'Continue')])[1]",
                        ElementAction.Click
                    );
                    _logger?.LogUiAction("Click", "ContinueButton", "Continue button clicked successfully");
                }
                else
                {
                    throw new PageElementException("Save/Add car/Continue button", "not found on the page");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogException(ex, "Failed to click save/add car/continue button");
                throw;
            }
        }
    }
}
