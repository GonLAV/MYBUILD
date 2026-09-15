using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.D2C.Base;
using Bolt.Automation.FrontEnds.Projects.D2C.Pages;

namespace Bolt.Automation.FrontEnds.Projects.D2C.Popups
{
    public class D2C_AddEditAnotherCarPopUp(IBrowserManager browserManager, IPageHelper pageHelper, IScopeContext scopeContext, bool validatePageReady = true, IAutomationLogger? logger = null)
        : D2CPopupBase(browserManager, pageHelper, scopeContext, validatePageReady, logger)
    {
        private const string VINLocator = "(//input[@name='VIN'])[1]";
        private const string AnnualMileageLocator = "input#NumberOfMiles";
        private const string OwnershipTypeLocator = "ng-select[name='OwnershipType']";

        protected override string PopupIdentifier => "vehicles";
        protected override string PopupName => "Add/Edit Car Popup";

        public async Task<bool> IsSpecificFieldEnabled(string elName)
        {
            var element = Page.Locator($"input#{elName}");
            if (await element.CountAsync() == 0)
                return false;

            return await element.IsEnabledAsync();
        }

        public async Task<D2C_VehiclesPage> ClickOnAddCar()
        {
            await ClickPopupContinue();
            return new D2C_VehiclesPage(BrowserManager, PageHelper, ScopeContext);
        }

        public async Task SetVIN(string vin)
        {
            var vinInput = Page.Locator(VINLocator);
            await PageHelper.InteractWithElement(
                LocatorType.XPath,
                VINLocator,
                ElementAction.Fill,
                new ElementInteractionOptions { Value = vin }
            );
            await vinInput.PressAsync("Enter");
            await Task.Delay(1000);
        }

        public async Task SetAnnualMileage(string annualMileage)
        {
            await PageHelper.InteractWithElement(
                LocatorType.CSS,
                AnnualMileageLocator,
                ElementAction.Fill,
                new ElementInteractionOptions { Value = annualMileage }
            );
        }

        public async Task SelectOwnershipType(string value)
        {
            await PageHelper.InteractWithElement(
                LocatorType.CSS,
                OwnershipTypeLocator,
                ElementAction.Select,
                new ElementInteractionOptions { Value = value }
            );
        }
    }
}
