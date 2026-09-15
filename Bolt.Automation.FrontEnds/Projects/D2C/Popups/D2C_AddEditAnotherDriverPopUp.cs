using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.D2C.Base;

namespace Bolt.Automation.FrontEnds.Projects.D2C.Popups
{
    public class D2C_AddEditAnotherDriverPopUp(IBrowserManager browserManager, IPageHelper pageHelper, IScopeContext scopeContext, bool validatePageReady = true, IAutomationLogger? logger = null)
        : D2CPopupBase(browserManager, pageHelper, scopeContext, validatePageReady, logger)
    {
        #region Locators
        private const string TitlePageLocator = "(//span[contains(text(),'Add another driver')])[1]";
        private const string FirstNameLocator = "#FirstName";
        private const string LastNameLocator = "#LastName";
        private const string DateOfBirthLocator = "#DOB";
        #endregion

        protected override string PopupIdentifier => "drivers";
        protected override string PopupName => "Add/Edit Driver Popup";
        public int StartStep { get; set; } = 0;


        public override async Task FillForm(Dictionary<string, string> formData)
        {
            for (int step = StartStep; step < 2; step++)
            {
                await base.FillForm(formData);

                if (step == 0)
                {
                    await ClickContinue();
                }
            }
        }
    }
}