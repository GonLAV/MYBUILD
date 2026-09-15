using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.Interfaces;
using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Microsoft.Playwright;

namespace Bolt.Automation.FrontEnds.Projects.STS
{
    public class STS_LoginPage : IBase
    {
        protected readonly IPageHelper _pageHelper;
        public IPageHelper PageHelper => _pageHelper;
        protected readonly IBrowserManager BrowserManager;
        protected readonly IAutomationLogger? _logger;
        protected readonly IScopeContext ScopeContext;
        public IPage Page => BrowserManager.GetCurrentTab()!;

        // Page identification
        private const string PageIdentifier = "login";
        private const string PageName = "STS Login Page";

        // Locators
        private const string UsernameInputLocator = "input#emailInput, input#txtUsername";
        private const string PasswordInputLocator = "input#passwordInput, input#txtPassword";
        private const string GroupExternalIdInputLocator = "input#groupExternalIdInput";
        private const string LoginButtonLocator = "button#btnLogin, button.next-button";

        public STS_LoginPage(
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

        public async Task ClickContinue()
        {
            await _pageHelper.InteractWithElement(
                LocatorType.CSS,
                LoginButtonLocator,
                ElementAction.Click);
        }

        public async Task Login()
        {
            var user = ScopeContext.Get(ctx => ctx.CurrentUser);

            await SetUserName(user!.Username);
            if (!string.IsNullOrEmpty(user.GroupExternalId))
            {
                await SetGroupExternalId(user.GroupExternalId);
            }

            // SetPassword is placed inside the action so the response listener is registered
            // before the fill runs. If the page JS auto-submits on the input event, the
            // response is captured. ClickContinue is still called to verify the button works;
            // if auto-submit already navigated away before the click, the exception is swallowed
            // because the response was already captured above.
            try
            {
                await Page.RunAndWaitForResponseAsync(
                    async () =>
                    {
                        await SetPassword(user.Password);
                        try
                        {
                            await ClickContinue();
                        }
                        catch (Exception) when (!Page.Url.Contains("login", StringComparison.OrdinalIgnoreCase))
                        {
                            // Auto-submit fired during SetPassword and navigated away before
                            // we could click. Response was already captured — this is a success.
                            _logger?.Info($"Auto-submit navigated away before button click. Current URL: '{Page.Url}'.");
                        }
                    },
                    response => response.Status == 200 &&
                                (response.Url.Contains("token=", StringComparison.OrdinalIgnoreCase) ||
                                 response.Url.Contains("/login/token", StringComparison.OrdinalIgnoreCase) ||
                                 response.Url.Contains("/login/get-permissions", StringComparison.OrdinalIgnoreCase)),
                    new PageRunAndWaitForResponseOptions { Timeout = 30000 });
            }
            catch (TimeoutException)
            {
                throw new TimeoutException(
                    $"Timed out waiting for login response. CurrentPageUrl='{Page.Url}'.");
            }
        }

        public async Task SetUserName(string username)
        {
            await _pageHelper.InteractWithElement(
                LocatorType.CSS,
                UsernameInputLocator,
                ElementAction.Fill,
                new ElementInteractionOptions { Value = username, IgnoreIfNotFound = false, Timeout = 30000 }
            );
        }

        public async Task SetPassword(string password)
        {
            await _pageHelper.InteractWithElement(
                LocatorType.CSS,
                PasswordInputLocator,
                ElementAction.Fill,
                new ElementInteractionOptions { Value = password, IgnoreIfNotFound = false, Timeout = 30000 }
            );
        }

        public async Task SetGroupExternalId(string groupExternalId)
        {
            await _pageHelper.InteractWithElement(
                LocatorType.CSS,
                GroupExternalIdInputLocator,
                ElementAction.Fill,
                new ElementInteractionOptions { Value = groupExternalId }
            );
        }
    }
}
