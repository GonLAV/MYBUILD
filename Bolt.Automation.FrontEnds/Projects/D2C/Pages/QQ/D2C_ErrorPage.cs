using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Exceptions;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Microsoft.Playwright;
using D2CBase = Bolt.Automation.FrontEnds.Projects.D2C.Base.D2CBase;

namespace Bolt.Automation.FrontEnds.Projects.D2C.Pages
{
    public class D2C_ErrorPage : D2CBase
    {
        #region Locators
        private const string MessageLocator = "h1";
        private const string TitleMessageLocator = "//h1";
        private const string ResendLinkButtonLocator = "//button//*[contains(text(),'Resend link')]";
        #endregion

        #region Page Properties
        protected override string PageIdentifier => "error";
        protected override string PageName => "Error Page";
        #endregion

        private readonly string _expectedErrorUrl;

        public D2C_ErrorPage(
            IBrowserManager browserManager,
            IPageHelper pageHelper,
            IScopeContext scopeContext,
            string expectedErrorUrl = "",
            bool validatePageReady = true,
            IAutomationLogger? logger = null)
            : base(browserManager, pageHelper, scopeContext, validatePageReady, logger)
        {
            _expectedErrorUrl = expectedErrorUrl;
            if (validatePageReady)
            {
                ValidateErrorPageReady().ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }

        /// <summary>
        /// Alternative factory method
        /// </summary>
        public static D2C_ErrorPage CreateWithExpectedUrl(
            IBrowserManager browserManager,
            IPageHelper pageHelper,
            IScopeContext scopeContext,
            string expectedUrl)
        {
            return new D2C_ErrorPage(browserManager, pageHelper, scopeContext, expectedUrl);
        }

        private async Task ValidateErrorPageReady()
        {
            try
            {
                // Wait for navigation to the error page if we have an expected URL
                if (!string.IsNullOrEmpty(_expectedErrorUrl))
                {
                    var expectedPath = $"/error/{_expectedErrorUrl}";
                    _logger?.LogBusinessRule("WaitingForNavigation", true, $"Waiting for navigation to URL containing: {expectedPath}");
                    var navigationSuccess = await PageHelper.WaitForNavigationOrUrlContainsAsync(expectedPath, timeout: 30000);
                    if (!navigationSuccess)
                    {
                        var currentUrl = Page.Url;
                        throw new NavigationException($"Expected error page URL to contain '{expectedPath}', but current URL is: {currentUrl}. Navigation may not have completed.");
                    }
                    _logger?.LogBusinessRule("ExpectedErrorUrlValidation", true, $"URL validation successful. Expected: {expectedPath}, Actual: {Page.Url}");
                }
                else
                {
                    var navigationSuccess = await PageHelper.WaitForNavigationOrUrlContainsAsync("/error/", timeout: 30000);
                    if (!navigationSuccess)
                    {
                        var currentUrl = Page.Url;
                        throw new NavigationException($"Expected error page URL to contain '/error/', but current URL is: {currentUrl}. Navigation may not have completed.");
                    }
                }
                var messageElement = Page.Locator(MessageLocator);
                await messageElement.WaitForAsync(new LocatorWaitForOptions
                {
                    State = WaitForSelectorState.Visible,
                    Timeout = 5000
                });
                _logger?.LogBusinessRule("ErrorPageReady", true, $"Error page validated successfully with URL: {Page.Url}");
            }
            catch (Exception ex)
            {
                _logger?.LogException(ex, "Error page validation failed");
                _logger?.LogBusinessRule("ErrorPageReady", false, $"Error page validation failed: {ex.Message}");
                throw;
            }
        }

        public override async Task ValidatePageReady()
        {
            await ValidateErrorPageReady();
            await CaptureApplicationIdsFromSessionStorageAsync();
        }

        public async Task<string> GetMessage()
        {
            try
            {
                var messageElement = Page.Locator(MessageLocator);
                var text = await messageElement.TextContentAsync();
                return !string.IsNullOrWhiteSpace(text) ? text.Trim() : "Not found";
            }
            catch (Exception ex)
            {
                _logger?.LogException(ex, "Failed to get message from error page");
                return "Not found";
            }
        }

        public async Task<string> GetTitleMessage()
        {
            try
            {
                var titleElement = Page.Locator(TitleMessageLocator);
                var text = await titleElement.TextContentAsync();
                return !string.IsNullOrWhiteSpace(text) ? text.Trim() : "Not found";
            }
            catch (Exception ex)
            {
                _logger?.LogException(ex, "Failed to get title message from error page");
                return "Not found";
            }
        }
    }
}
