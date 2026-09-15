using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.Interfaces;
using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Microsoft.Playwright;

namespace Bolt.Automation.FrontEnds.Projects.STS
{
    public class STS_MfaPage : IBase
    {
        protected readonly IPageHelper _pageHelper;
        public IPageHelper PageHelper => _pageHelper;
        protected readonly IBrowserManager BrowserManager;
        protected readonly IAutomationLogger? _logger;
        protected readonly IScopeContext ScopeContext;
        public IPage Page => BrowserManager.GetCurrentTab()!;

        // Page identification
        private const string PageIdentifier = "2fa/entrance";
        private const string PageName = "STS MFA Verification Page";

        // Locators
        private const string CodeInputLocator = "input#otpInput";

        public STS_MfaPage(
          IBrowserManager browserManager,
          IPageHelper pageHelper,
          IScopeContext scopeContext,
          bool validatePageReady = true,
          IAutomationLogger? logger = null)
        {
            BrowserManager = browserManager ?? throw new ArgumentNullException(nameof(browserManager));
            _pageHelper = pageHelper ?? throw new ArgumentNullException(nameof(pageHelper));
            ScopeContext = scopeContext ?? throw new ArgumentNullException(nameof(scopeContext));
            _logger = logger;

            if (validatePageReady)
            {
                try
                {
                    ValidatePageReady().ConfigureAwait(false).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    _logger?.Error($"Page validation failed during {PageName} initialization: {ex.Message}");
                    throw;
                }
            }
        }

        public async Task ValidatePageReady()
        {
            await _pageHelper.ValidatePageReadyAsync(PageIdentifier, PageName);
        }

        // No continue button on this page — it auto-submits once the code reaches 6 digits (see EnterVerificationCode).
        public Task ClickContinue() => Task.CompletedTask;

        /// <summary>
        /// Fills the 6-digit verification code. The page auto-submits via a client-side
        /// 'input' event listener once the field reaches 6 digits — no separate submit button.
        /// </summary>
        public async Task EnterVerificationCode(string code)
        {
            try
            {
                await Page.RunAndWaitForResponseAsync(
                    async () =>
                    {
                        await _pageHelper.InteractWithElement(
                            LocatorType.CSS,
                            CodeInputLocator,
                            ElementAction.Fill,
                            new ElementInteractionOptions { Value = code, IgnoreIfNotFound = false, Timeout = 30000 });
                    },
                    response => response.Status == 200 &&
                                response.Url.Contains("/2fa/verify", StringComparison.OrdinalIgnoreCase),
                    new PageRunAndWaitForResponseOptions { Timeout = 30000 });
            }
            catch (TimeoutException)
            {
                throw new TimeoutException(
                    $"Timed out waiting for MFA verification response. CurrentPageUrl='{Page.Url}'.");
            }
        }
    }
}
