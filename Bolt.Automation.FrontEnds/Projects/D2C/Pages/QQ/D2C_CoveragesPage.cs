using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using D2CBase = Bolt.Automation.FrontEnds.Projects.D2C.Base.D2CBase;

namespace Bolt.Automation.FrontEnds.Projects.D2C.Pages
{
    public class D2C_CoveragesPage(
        IBrowserManager browserManager,
        IPageHelper pageHelper,
        IScopeContext scopeContext,
        bool validatePageReady = true,
        IAutomationLogger? logger = null
    ) : D2CBase(browserManager, pageHelper, scopeContext, validatePageReady, logger)
    {
        #region Locators
        private const string BodilyInjuryLiabilityLocator = "ng-select[name='BI']";
        private const string CompDeductibleLocator = "ng-select[name='CompDeductible']";
        #endregion

        protected override string PageIdentifier => "coverages";
        protected override string PageName => "Coverages Page";

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

        public async Task<string> GetBodilyInjuryLiabilityValue()
        {
            var element = Page.Locator(BodilyInjuryLiabilityLocator + " .ng-value-label");
            return await element.TextContentAsync() ?? string.Empty;
        }

        public async Task<string> GetSpecificCompDeductible(string carName)
        {
            var editCarName = carName.Trim();
            string selector = $"//p[contains(@class,'name') and contains(text(),'{editCarName}')]/ancestor::app-vehicle-cover//ng-select[@name='CompDeductible']//span[contains(@class,'ng-value-label')]";

            var element = Page.Locator(selector);
            return await element.TextContentAsync() ?? string.Empty;
        }
    }
}