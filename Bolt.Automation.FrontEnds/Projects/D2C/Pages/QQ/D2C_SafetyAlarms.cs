using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using D2CBase = Bolt.Automation.FrontEnds.Projects.D2C.Base.D2CBase;

namespace Bolt.Automation.FrontEnds.Projects.D2C.Pages
{
    public class D2C_SafetyAlarms(
        IBrowserManager browserManager,
        IPageHelper pageHelper,
        IScopeContext scopeContext,
        bool validatePageReady = true,
        IAutomationLogger? logger = null)
        : D2CBase(browserManager, pageHelper, scopeContext, validatePageReady, logger)
    {
        #region Locators
        private const string ProductTypeToggleLocator = "//mat-button-toggle-group[@id='{0}']//span[contains(text(),'{1}')]";
        #endregion

        #region Page Properties
        /// <summary>
        /// URL fragment that identifies this page. Public for the same reason as
        /// <see cref="D2C_RoofReplacementPage.UrlPart"/>.
        /// </summary>
        public const string UrlPart = "-alarms";

        protected override string PageIdentifier => UrlPart;
        protected override string PageName => "Safety Alarms Page";
        #endregion

        public async Task SelectProductChildQuestion(string productType, string value)
        {
            string locator = string.Format(ProductTypeToggleLocator, productType, value);
            
            await PageHelper.InteractWithElement(
                LocatorType.XPath,
                locator,
                ElementAction.Click,
                new ElementInteractionOptions { Value = value }
            );           
        }
    }
}
