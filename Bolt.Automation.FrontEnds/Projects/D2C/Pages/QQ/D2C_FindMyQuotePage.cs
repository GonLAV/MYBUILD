using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using D2CBase = Bolt.Automation.FrontEnds.Projects.D2C.Base.D2CBase;

namespace Bolt.Automation.FrontEnds.Projects.D2C.Pages
{
    public class D2C_FindMyQuotePage(
        IBrowserManager browserManager,     
        IPageHelper pageHelper,
        IScopeContext scopeContext,
        bool validatePageReady = true,
        IAutomationLogger? logger = null)
        : D2CBase(browserManager, pageHelper, scopeContext, validatePageReady, logger)
    {
        #region Page Properties
        protected override string PageIdentifier => "retrieve-quote";
        protected override string PageName => "Find My Quote Page";
        #endregion

        #region Locators
        private const string RetrieveButtonLocator = "//button[@data-automation-data = 'retrieve-quote']";
        private const string StartNewQuoteButtonLocator = "//button[text()='Start a new quote']";
        private const string ErrorMessageLocator = "//div[contains(@class, 'not-found')] | //div[contains(@class, 'in-use')]";
        #endregion

        #region Page Actions
        public async Task<bool> IsRetrieveButtonDisabled()
        {
            try
            {
                var buttonLocator = Page.Locator(RetrieveButtonLocator);
                
                if (await buttonLocator.CountAsync() == 0)
                {
                    _logger?.Debug("Retrieve button not found");
                    return true;
                }

                bool isDisabled = await buttonLocator.IsDisabledAsync();
                _logger?.Debug($"Retrieve button disabled state: {isDisabled}");
                return isDisabled;
            }
            catch (Exception ex)
            {
                _logger?.LogException(ex, "Failed to check if retrieve button is disabled");
                return true;
            }
        }

        public async Task<bool> IsStartNewQuoteButtonExist()
        {
            try
            {
                var buttonLocator = Page.Locator(StartNewQuoteButtonLocator);
                var count = await buttonLocator.CountAsync();
                var exists = count > 0;
                
                _logger?.Debug($"Start new quote button exists: {exists}");
                return exists;
            }
            catch (Exception ex)
            {
                _logger?.LogException(ex, "Failed to check if start new quote button exists");
                return false;
            }
        }

        public async Task ClickOnRetrieveButton()
        {
            try
            {
                await PageHelper.InteractWithElement(
                    LocatorType.XPath,
                    RetrieveButtonLocator,
                    ElementAction.Click);

                _logger?.LogUiAction("Click", "RetrieveButton", "Clicked retrieve button");
            }
            catch (Exception ex)
            {
                _logger?.LogException(ex, "Failed to click retrieve button");
                throw;
            }
        }

        public async Task<string> GetErrorMessage()
        {
            try
            {
                var errorLocator = Page.Locator(ErrorMessageLocator);
                
                await PageHelper.WaitForElementAsync(errorLocator, timeout: 5000, waitForVisibility: true);
                
                if (await errorLocator.CountAsync() == 0)
                {
                    _logger?.Debug("No error message found");
                    return string.Empty;
                }

                var errorMessage = await errorLocator.First.TextContentAsync() ?? string.Empty;
                _logger?.Debug($"Retrieved error message: {errorMessage}");
                return errorMessage.Trim();
            }
            catch (Exception ex)
            {
                _logger?.LogException(ex, "Failed to get error message");
                return string.Empty;
            }
        }
        #endregion
    }
}
