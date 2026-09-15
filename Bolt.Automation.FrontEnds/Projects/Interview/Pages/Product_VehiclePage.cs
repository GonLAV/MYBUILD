using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.Interview.Base;
using Microsoft.Playwright;
using static Bolt.Automation.FrontEnds.FormData.Common.FieldNames;

namespace Bolt.Automation.FrontEnds.Projects.Interview.Pages
{
    public class Product_VehiclePage : InterviewBase
    {
        // KLX old-interview Vehicle page is gated by a VIN-decode step: typing a VIN
        // and clicking Submit populates and locks Year/Make/Model/BodyStyle. This is a
        // multi-step interaction not expressible as a single registry field, so it's
        // driven from FillForm before delegating to base for the remaining fields.
        private const string VinInputLocator =
            "//*[contains(@class,'vehicle-search-block')]//input[@placeholder='Type VIN Number']";
        private const string VinSubmitButtonLocator =
            "//*[contains(@class,'vehicle-search-block')]//app-button[@text='Submit']//button";
        private const string YearDecodedLocator =
            "//*[contains(@class,'PolicyDataVehicles') and contains(@class,'YearVehicleDropdownWithSearch')]//ng-select[contains(@class,'ng-select-disabled')]";

        // KLX-only: several Vehicle-page ng-selects have a floated `<app-form-label>` that
        // overlays the input area and intercepts Playwright's standard click. The
        // `.ng-arrow-wrapper` sits on the far right, outside the label overlay — clicking
        // it opens the panel reliably (same fix as the CL Policy page).
        // Fields handled here: AnnualMileage (Radius), VehicleOwnerShip, PrimaryUseOfVehicle.
        private static readonly (string FieldName, string ClassFragment)[] LabelInterceptedFields =
        {
            (AnnualMileage,            "RadiusCustomDropdown"),
            (VehicleOwnerShip,         "CLLengthVehicleOwnership"),
            (PrimaryUseOfVehicle,      "PrimaryUseOfVehicleCustomDropdown"),
        };

        public Product_VehiclePage(
            IBrowserManager browserManager,
            IPageHelper pageHelper,
            IScopeContext scopeContext,
            bool validatePageReady = true,
            IAutomationLogger? logger = null)
            : base(browserManager, pageHelper, scopeContext, validatePageReady, logger) { }

        protected override string PageIdentifier => "_Vehicle";
        protected override string PageName => "Vehicle Page";

        public override async Task FillForm(Dictionary<string, string>? formData = null)
        {
            var labelInterceptValues = new Dictionary<string, string>();

            if (formData != null && formData.TryGetValue(VIN, out var vinValue) && !string.IsNullOrEmpty(vinValue))
            {
                await FillVinAndDecodeAsync(vinValue);

                // Decode populated and locked Year/Make/Model/BodyStyle. Suppress them so
                // base.FillForm doesn't try to re-select on disabled ng-selects.
                var filtered = new Dictionary<string, string>(formData)
                {
                    [VIN] = "",
                    [PLYear] = "",
                    [PLMake] = "",
                    [PLModel] = "",
                    [BodyStyle] = ""
                };

                // Pull label-intercepted fields out of the normal pass — handled below.
                foreach (var (fieldName, _) in LabelInterceptedFields)
                {
                    if (filtered.TryGetValue(fieldName, out var value) && !string.IsNullOrEmpty(value))
                    {
                        labelInterceptValues[fieldName] = value;
                        filtered[fieldName] = "";
                    }
                }

                formData = filtered;
            }

            await base.FillForm(formData);

            foreach (var (fieldName, classFragment) in LabelInterceptedFields)
            {
                if (labelInterceptValues.TryGetValue(fieldName, out var value))
                    await SelectViaArrowAsync(classFragment, value);
            }
        }

        // Opens the dropdown via .ng-arrow-wrapper (bypassing the floated-label overlay
        // that intercepts standard ng-select clicks) and picks an option by text.
        private async Task SelectViaArrowAsync(string classFragment, string optionText)
        {
            _logger?.Info($"Vehicle: selecting '{optionText}' on '{classFragment}' via ng-arrow-wrapper");

            var container = Page.Locator(
                $"xpath=//*[contains(@class,'PolicyDataVehicles') and contains(@class,'{classFragment}')]").First;
            if (await container.CountAsync() == 0)
            {
                _logger?.Warning($"Vehicle: container '{classFragment}' not found, skipping");
                return;
            }

            var arrow = container.Locator(".ng-arrow-wrapper").First;
            await arrow.ClickAsync(new LocatorClickOptions { Timeout = 5000 });

            var panel = Page.Locator(".ng-dropdown-panel");
            await panel.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = 5000
            });

            var option = panel.Locator(".ng-option", new() { HasText = optionText }).First;
            await option.ClickAsync();

            await panel.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Hidden,
                Timeout = 5000
            });
        }

        private async Task FillVinAndDecodeAsync(string vinValue)
        {
            _logger?.Info($"Typing VIN '{vinValue}' and clicking Submit to decode");

            var input = Page.Locator($"xpath={VinInputLocator}");
            await input.FillAsync(vinValue);
            await input.PressAsync("Tab");

            var submit = Page.Locator($"xpath={VinSubmitButtonLocator}");
            await submit.ClickAsync();

            try
            {
                await Page.Locator($"xpath={YearDecodedLocator}").First.WaitForAsync(new LocatorWaitForOptions
                {
                    State = WaitForSelectorState.Attached
                });
            }
            catch (TimeoutException)
            {
                _logger?.Warning("VIN decode signal not detected; continuing.");
            }

            _logger?.Info("VIN decode complete");
        }
    }
}
