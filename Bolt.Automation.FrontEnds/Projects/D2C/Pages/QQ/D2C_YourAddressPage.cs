using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.FormData.Common;
using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.D2C.Base;

namespace Bolt.Automation.FrontEnds.Projects.D2C.Pages
{
    public class D2C_YourAddressPage(
    IBrowserManager browserManager,
    IPageHelper pageHelper,
    IScopeContext scopeContext,
    bool validatePageReady = true,
    IAutomationLogger? logger = null) : D2CBase(browserManager, pageHelper, scopeContext, validatePageReady, logger)
    {
        #region Page Properties
        protected override string PageIdentifier => "your-address";
        protected override string PageName => "Your Address Page";
        #endregion

        #region Locators
        private const string FindMyQuoteLinkLocator = "//a[@class = 'find-quote-button ng-star-inserted']";
        #endregion

        public override async Task FillForm(Dictionary<string, string> formData)
        {
            if (formData != null && formData.ContainsKey(FieldNames.OnlineAddress))
            {
                await Task.Delay(2000);
                await base.FillForm(formData);
            }
        }

        public async Task<D2C_FindMyQuotePage> ClickOnFindMyQuote()
        {
            try
            {
                await PageHelper.InteractWithElement(
                    LocatorType.XPath,
                    FindMyQuoteLinkLocator,
                    ElementAction.Click);

                _logger?.LogUiAction("Click", "FindMyQuoteLink", "Clicked 'Find my quote' link");

                return new D2C_FindMyQuotePage(BrowserManager, PageHelper, ScopeContext, true, _logger);
            }
            catch (Exception ex)
            {
                _logger?.LogException(ex, "Failed to click 'Find my quote' link");
                throw;
            }
        }
    }

}