using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using D2CBase = Bolt.Automation.FrontEnds.Projects.D2C.Base.D2CBase;

namespace Bolt.Automation.FrontEnds.Projects.D2C.Pages
{
    public class D2C_PersonalDetailBundlePage(
        IBrowserManager browserManager,
        IPageHelper pageHelper,
        IScopeContext scopeContext,
        bool validatePageReady = true,
        IAutomationLogger? logger = null)
        : D2CBase(browserManager, pageHelper, scopeContext, validatePageReady, logger)
    {
        #region Locators

        private const string DateOfBirthLocator = "#DateOfBirth";
        private const string AgreeToTermsLocator = "//label[@for = 'D2CAgreeToTerms']";
        #endregion

        protected override string PageIdentifier => "personal-details-bundle";
        protected override string PageName => "personal details bundle";

        public override async Task FillForm(Dictionary<string, string>? formData = null)
        {
            for (int step = 0; step < 2; step++)
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
