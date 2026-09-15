using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Exceptions;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.FormData.Helpers;
using Bolt.Automation.FrontEnds.Interfaces;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Microsoft.Playwright;

namespace Bolt.Automation.FrontEnds.Projects.HQXAgent.Base
{
    public abstract class HQXAgentPopupBase : IPopup
    {
        protected readonly IPageHelper PageHelper;
        protected readonly IBrowserManager BrowserManager;
        protected readonly IAutomationLogger? _logger;
        protected readonly IScopeContext ScopeContext;
        private IPage? _page;
        public IPage Page => _page ??= BrowserManager.GetCurrentTab()!;
        protected abstract string PopupIdentifier { get; }
        protected abstract string PopupName { get; }

        protected static class PopupLocators
        {
            public const string CloseButton = "button[aria-label='Close'], .close-btn, .modal-close, button:has-text('×'), .close-button, button.cancel";
            public const string ModalContainer = ".modal, .popup, .dialog, [role='dialog'], [role='alertdialog']";
            public const string ContinueButton = "button.next-button, button:has-text('Continue'), .continue-btn, button[aria-label='Continue']";
            public const string ConfirmButton = "button:has-text('Confirm'), button[aria-label='Confirm'], .confirm-btn";
            public const string SaveButton = "button:has-text('Save'), button[aria-label='Save'], .save-btn";
            public const string CancelButton = "button.cancel, button:has-text('Cancel'), button[aria-label='Cancel'], .cancel-btn";
            public const string PopupTitle = ".modal-title, .popup-title, h1, h2, h3, .title";
        }

        protected HQXAgentPopupBase(
            IBrowserManager browserManager,
            IPageHelper pageHelper,
            IScopeContext scopeContext,
            bool validatePageReady = true,
            IAutomationLogger? logger = null)
        {
            BrowserManager = browserManager ?? throw new ArgumentNullException(nameof(browserManager));
            PageHelper = pageHelper ?? throw new ArgumentNullException(nameof(pageHelper));
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
                    _logger?.LogException(ex, "Page validation failed during {0} initialization", PopupName);
                    throw;
                }
            }
        }


        public virtual async Task ClickContinue()
        {
            await TryClickElement(Page.Locator(PopupLocators.ContinueButton));
        }

        public virtual async Task ValidatePageReady() => await WaitForPopupToAppear();

        public virtual async Task FillForm(Dictionary<string, string>? formData = null)
        {
            var (pageSpecificData, fields) = FormDataHelper.GetFieldsForPage(this, ScopeContext, formData);
            await Page.FillRelevantFields(pageSpecificData, fields, ScopeContext, PageHelper, _logger);
        }
        public virtual async Task WaitForPopupToAppear(int timeoutMs = 10000)
        {
            try
            {
                // Wait for popup to be visible
                var popupLocator = Page.Locator(PopupLocators.ModalContainer);
                await popupLocator.WaitForAsync(new LocatorWaitForOptions
                {
                    State = WaitForSelectorState.Visible,
                    Timeout = timeoutMs
                });

                // Additional wait for popup content to load
                await Task.Delay(500);
            }
            catch (TimeoutException ex)
            {
                throw new PopupTimeoutException(PopupName, "did not appear", timeoutMs, ex);
            }
        }
        public virtual async Task ClosePopup()
        {
            if (await TryClickCloseButton()) return;
        }

        protected virtual async Task<bool> TryClickCloseButton()
        {
            var closeButtonLocator = Page.Locator(PopupLocators.CloseButton);
            return await TryClickElement(closeButtonLocator);
        }


        protected virtual async Task<bool> TryClickElement(ILocator locator)
        {
            try
            {
                if (await locator.CountAsync() > 0 && await locator.IsVisibleAsync())
                {
                    await locator.ClickAsync(new LocatorClickOptions { Timeout = 3000 });
                    return true;
                }
            }
            catch
            {
                // Element not clickable or not found
            }
            return false;
        }

    }
}
