using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using D2CBase = Bolt.Automation.FrontEnds.Projects.D2C.Base.D2CBase;

namespace Bolt.Automation.FrontEnds.Projects.D2C.Pages
{
    public class D2C_PetsPage(
        IBrowserManager browserManager,
        IPageHelper pageHelper,
        IScopeContext scopeContext,
        bool validatePageReady = true,
        IAutomationLogger? logger = null
    ) : D2CBase(browserManager, pageHelper, scopeContext, validatePageReady, logger)
    {
        #region Locators
        private const string AddAnotherPetLocator = "//button[@aria-label='Add another pet']";
        private const string EditDetailsLocator = "//span[text()='{0}']/ancestor::div[@class='accordion-top']//span[text()='Edit details']/..";
        private const string DisablePetToggleLocator = "//span[text()='{0}']/ancestor::div[@class='accordion-top']//mat-slide-toggle";
        #endregion

        #region Page Properties
        protected override string PageIdentifier => "pet-details";
        protected override string PageName => "Pet Details Page";
        #endregion

        public override async Task FillForm(Dictionary<string, string>? formData = null)
        {
            for (int step = 0; step < 3; step++)
            {
                await base.FillForm(formData);

                if (step != 3)
                {
                    await ClickContinue();
                }
            }
        }

        public async Task ClickAddAnotherPet()
        {
            await PageHelper.InteractWithElement(
                LocatorType.XPath,
                AddAnotherPetLocator,
                ElementAction.Click
            );
        }

        public async Task ClickEditDetailsForPet(string petName)
        {
            string locator = string.Format(EditDetailsLocator, petName);
            await PageHelper.InteractWithElement(
                LocatorType.XPath,
                locator,
                ElementAction.Click
            );
        }

        public async Task DisablePet(string petName)
        {
            string locator = string.Format(DisablePetToggleLocator, petName);
            await PageHelper.InteractWithElement(
                LocatorType.XPath,
                locator,
                ElementAction.Click
            );
        }
    }
}
