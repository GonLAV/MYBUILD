using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Exceptions;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.FormData.Helpers;
using Bolt.Automation.FrontEnds.Interfaces;
using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Microsoft.Playwright;

namespace Bolt.Automation.FrontEnds.Projects.Interview.Base
{
    /// <summary>
    /// Base class for Interview popup/modal dialogs.
    /// Provides common functionality for popup interactions.
    /// </summary>
    public abstract class InterviewPopupBase : IPopup
    {
        protected readonly IPageHelper PageHelper;
        protected readonly IBrowserManager BrowserManager;
        protected readonly IAutomationLogger? _logger;
        protected readonly IScopeContext ScopeContext;
        private IPage? _page;
        public IPage Page => _page ??= BrowserManager.GetCurrentTab()!;

        /// <summary>
        /// CSS/XPath selector that identifies this popup uniquely.
        /// </summary>
        protected abstract string PopupIdentifier { get; }

        /// <summary>
        /// Human-readable name for logging purposes.
        /// </summary>
        protected abstract string PopupName { get; }

        protected static class PopupLocators
        {
            public const string CloseButton = "button[aria-label='Close'], .close-btn, .modal-close, button:has-text('×'), .close-button, .btn-close";
            public const string ModalContainer = "div.modal-dialog, div.modal-content";
            public const string ContinueButton = "button:has-text('Continue'), button:has-text('Save'), button:has-text('OK'), button:has-text('Add')";
            public const string ConfirmButton = "button:has-text('CONFIRM')";
            public const string OkButton = "//button[contains(@class,'OK')]";
            public const string SaveButton = "button:has-text('Save'), button[aria-label='Save'], .save-btn";
            public const string SendButton = "//span[contains(text(),'Send')]";
            public const string SubmitButton = "button.Submit";
            public const string CancelButton = "button:has-text('Cancel'), button[aria-label='Cancel'], .cancel-btn";
            public const string PopupTitle = ".modal-title, .popup-title, h1, h2, h3, .title";
            public const string OverlayBackdrop = "//app-loader/div[@class='loader-overlay show']";
            public const string RequestSubmittedlocator = "//button[contains(@class,'Request Submitted')][@disabled='true']";
        }

        private static readonly string[] PopupContinueSelectors = new[]
        {
            PopupLocators.ContinueButton,
            PopupLocators.SaveButton,
            PopupLocators.ConfirmButton,
            PopupLocators.SubmitButton,
            PopupLocators.OkButton,
            PopupLocators.SendButton,
            "button span:has-text('Save')",
            "button span:has-text('Continue')",
            "button span:has-text('Add')",
            "//button//span[text()='Save']",
            "//button//span[contains(text(),'Add')]",
            "button.app-button.primary",
            "button.continue-btn",
            "button.save-btn"
        };
        private static readonly string[] PopupConfirmSelectors = new[]
        {
            PopupLocators.ConfirmButton,
            PopupLocators.OkButton,
        };
        protected virtual async Task WaitForLoaderToDisappear()
        {
            try
            {
                var loaderLocator = Page.Locator(PopupLocators.OverlayBackdrop);
                await PageHelper.WaitForElementAsync(loaderLocator);
                if (await loaderLocator.CountAsync() > 0)
                {
                    _logger?.Debug("Loader found, waiting for it to disappear");
                    await PageHelper.WaitForElementToDisappearAsync(loaderLocator);
                    _logger?.Debug("Loader disappeared successfully");
                }
                else
                {
                    _logger?.Debug("No loader found on page");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogException(ex, "Failed to wait for loader to disappear on {0}", PopupName);
                throw;
            }
        }
        protected InterviewPopupBase(
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
                    _logger?.LogException(ex, "Popup validation failed during {0} initialization", PopupName);
                    throw;
                }
            }
        }

        public virtual async Task ClickContinue() => await ClickPopupContinue();

        public virtual async Task ValidatePageReady() => await WaitForPopupToAppear();

        public virtual async Task FillForm(Dictionary<string, string>? formData = null)
        {
            var (pageSpecificData, fields) = FormDataHelper.GetFieldsForPage(this, ScopeContext, formData);
            await Page.FillRelevantFields(pageSpecificData, fields, ScopeContext, PageHelper, _logger);
        }

        public virtual async Task ClosePopup()
        {
            if (await TryClickCloseButton()) return;
            if (await TryClickCancelButton()) return;
            if (await TryClickOutsidePopup()) return;

            await Page.Keyboard.PressAsync("Escape");
        }

        public virtual async Task WaitForPopupToAppear(int timeoutMs = 10000)
        {
            try
            {
                var popupLocator = Page.Locator(PopupLocators.ModalContainer);
                await popupLocator.WaitForAsync(new LocatorWaitForOptions
                {
                    State = WaitForSelectorState.Visible,
                    Timeout = timeoutMs
                });

                await Task.Delay(300);
            }
            catch (TimeoutException ex)
            {
            
            }
        }

        public async Task ClickPopupConfirm()
        {
            foreach (var selector in PopupConfirmSelectors)
            {
                var locator = CreateLocator(selector);
                if (await TryClickElement(locator))
                {
                    await WaitForPopupToClose();
                    return;
                }
            }

            var selectorDetails = string.Join(", ", PopupConfirmSelectors.Select((sel, idx) => $"#{idx + 1}: '{sel}'"));
            throw new PopupTimeoutException(PopupName,
                $"Confirm button not clicked after trying {PopupConfirmSelectors.Length} selector(s): [{selectorDetails}]. Page: {Page.Url}",
                timeoutMs: 0);
        }

        protected virtual async Task ClickPopupContinue()
        {
            foreach (var selector in PopupContinueSelectors)
            {
                var locator = CreateLocator(selector);
                if (await TryClickElement(locator))
                {
                    await WaitForLoaderToDisappear();
                    await WaitForPopupToClose();
                    return;
                }
            }

            var selectorDetails = string.Join(", ", PopupContinueSelectors.Select((sel, idx) => $"#{idx + 1}: '{sel}'"));
            throw new PopupTimeoutException(PopupName,
                $"Continue/Save button not clicked after trying {PopupContinueSelectors.Length} selector(s): [{selectorDetails}]. Page: {Page.Url}",
                timeoutMs: 0);
        }

        protected virtual async Task WaitForPopupToClose(int timeoutMs = 5000)
        {
            try
            {
                var popupLocator = Page.Locator(PopupLocators.ModalContainer);
                await popupLocator.WaitForAsync(new LocatorWaitForOptions
                {
                    State = WaitForSelectorState.Hidden,
                    Timeout = timeoutMs
                });
            }
            catch (TimeoutException)
            {
                // Popup may have already closed
            }
        }

        protected virtual async Task<bool> TryClickCloseButton()
        {
            var closeButtonLocator = Page.Locator(PopupLocators.CloseButton);
            return await TryClickElement(closeButtonLocator);
        }

        protected virtual async Task<bool> TryClickCancelButton()
        {
            var cancelButtonLocator = Page.Locator(PopupLocators.CancelButton);
            return await TryClickElement(cancelButtonLocator);
        }

        protected virtual async Task<bool> TryClickOutsidePopup()
        {
            try
            {
                var backdropLocator = Page.Locator(PopupLocators.OverlayBackdrop);
                if (await backdropLocator.CountAsync() > 0)
                {
                    await backdropLocator.ClickAsync();
                    return true;
                }

                await Page.ClickAsync("body", new PageClickOptions
                {
                    Position = new Position { X = 10, Y = 10 }
                });
                return true;
            }
            catch
            {
                return false;
            }
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

        protected virtual ILocator CreateLocator(string locatorValue)
        {
            return locatorValue.StartsWith("//")
                ? Page.Locator($"xpath={locatorValue}")
                : Page.Locator(locatorValue);
        }

        protected virtual async Task<string> GetPopupTitle()
        {
            try
            {
                var titleLocator = Page.Locator(PopupLocators.PopupTitle);
                if (await titleLocator.CountAsync() > 0)
                {
                    return await titleLocator.First.TextContentAsync() ?? string.Empty;
                }
            }
            catch
            {
                // Ignore title retrieval errors
            }
            return string.Empty;
        }

        protected virtual async Task<bool> IsPopupVisible()
        {
            try
            {
                var popupLocator = Page.Locator(PopupLocators.ModalContainer);
                return await popupLocator.CountAsync() > 0 && await popupLocator.IsVisibleAsync();
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> IsRequestSubmittedButtonExists()
        {
            return await PageHelper.ElementExists(LocatorType.XPath, PopupLocators.RequestSubmittedlocator);
        }
    }
}
