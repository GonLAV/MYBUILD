using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Exceptions;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.FormData.Helpers;
using Bolt.Automation.FrontEnds.Interfaces;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Microsoft.Playwright;

namespace Bolt.Automation.FrontEnds.Projects.D2C.Base
{
    public abstract class D2CPopupBase : IPopup
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
            public const string CloseButton = "button[aria-label='Close'], .close-btn, .modal-close, button:has-text('×'), .close-button";
            public const string ModalContainer = ".modal, .popup, .dialog, [role='dialog'], [role='alertdialog']";
            public const string ContinueButton = "button:has-text('Continue'), .continue-btn, button[aria-label='Continue']";
            public const string ConfirmButton = "button:has-text('Confirm'), button[aria-label='Confirm'], .confirm-btn";
            public const string SaveButton = "button:has-text('Save'), button[aria-label='Save'], .save-btn";
            public const string SaveButtonSpanText = "button span:has-text('Save')";
            public const string SaveButtonSpanExact = "//button//span[text()='Save']";
            public const string SaveButtonClass = "button.save-btn";
            public const string CancelButton = "button:has-text('Cancel'), button[aria-label='Cancel'], .cancel-btn";
            public const string PopupTitle = ".modal-title, .popup-title, h1, h2, h3, .title";
            public const string OverlayBackdrop = ".modal-backdrop, .overlay, .backdrop";
        }

        private static readonly string[] PopupContinueSelectors = new[]
        {
            // Standard button selectors (most commonly used)
            PopupLocators.ContinueButton,
            PopupLocators.SaveButton,
            PopupLocators.ConfirmButton,
            
            // Specific text variations for D2C popups
            PopupLocators.SaveButtonSpanText,
            "button span:has-text('Continue')",
            PopupLocators.SaveButtonSpanExact,
            "//button//span[contains(text(),'Next step')]",

            // Common D2C button classes
            "button.app-button.primary",
            "button.continue-btn",
            PopupLocators.SaveButtonClass
        };

        // Every entry also appears in PopupContinueSelectors above; both lists reference the same
        // PopupLocators constants so the two cannot drift apart.
        private static readonly string[] PopupSaveSelectors = new[]
        {
            PopupLocators.SaveButton,
            PopupLocators.SaveButtonSpanText,
            PopupLocators.SaveButtonSpanExact,
            PopupLocators.SaveButtonClass
        };

        protected D2CPopupBase(
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

        public virtual async Task ClickContinue() => await ClickPopupContinue();

        /// <summary>
        /// Clicks the popup's own Save button, never falling back to a generic 'Continue'
        /// selector. Use this to commit an EDIT popup: the page behind the modal already has
        /// its Continue button enabled (the reactive form is valid while the popup is open),
        /// and <see cref="ClickPopupContinue"/> tries <c>button[aria-label='Continue']</c>
        /// first - which resolves to that page button, closing the popup and losing the edit.
        /// Scoped to the modal, so a page that has its own Save button (Vehicles does) cannot
        /// be hit either.
        /// </summary>
        public virtual async Task ClickSave()
        {
            foreach (var selector in PopupSaveSelectors)
            {
                if (await TryClickElement(CreatePopupScopedLocator(selector)))
                {
                    _logger?.Debug("{0} saved using selector: '{1}'", PopupName, selector);
                    return;
                }
            }

            var selectorDetails = string.Join(", ", PopupSaveSelectors.Select((sel, idx) => $"#{idx + 1}: '{sel}'"));
            throw new PopupTimeoutException(PopupName, $"click Save button. Tried {PopupSaveSelectors.Length} selector(s): [{selectorDetails}]. Page URL: {Page.Url}", 0);
        }
        
        public virtual async Task ValidatePageReady() => await WaitForPopupToAppear();

        public virtual async Task FillForm(Dictionary<string, string>? formData = null)
        {
            var (pageSpecificData, fields) = FormDataHelper.GetFieldsForPage(this, ScopeContext, formData);
            await Page.FillRelevantFields(pageSpecificData, fields, ScopeContext, PageHelper, _logger);
        }

        /// <summary>
        /// Clicks the popup's Cancel button, which is what a test case means by "click Cancel" -
        /// discard the edit rather than just dismiss the popup. Throws when the popup has no
        /// Cancel button: an X / backdrop / Escape dismiss is a different gesture and the app may
        /// keep the edit that Cancel would have discarded, so falling back to it silently turns a
        /// missing button into a wrong assertion. Pass <paramref name="allowDismiss"/> for popups
        /// that genuinely only offer a dismiss, so the weaker gesture stays a deliberate choice.
        /// </summary>
        public virtual async Task ClickCancel(bool allowDismiss = false)
        {
            if (await TryClickElement(CreatePopupScopedLocator(PopupLocators.CancelButton)))
            {
                _logger?.Debug("{0} cancelled using selector: '{1}'", PopupName, PopupLocators.CancelButton);
                return;
            }

            if (!allowDismiss)
            {
                throw new PopupTimeoutException(
                    PopupName,
                    $"click Cancel button. Tried '{PopupLocators.CancelButton}' inside the popup. " +
                    $"Pass allowDismiss: true to accept an X / backdrop dismiss instead. Page URL: {Page.Url}",
                    0);
            }

            _logger?.Warning("{0} has no Cancel button; dismissing it instead - the app may keep the edit that Cancel would discard", PopupName);
            await ClosePopup();
        }

        public virtual async Task ClosePopup()
        {
            // Try different close methods in order of preference
            if (await TryClickCloseButton()) return;
            if (await TryClickOutsidePopup()) return;
            
            // Fallback to escape key
            await Page.Keyboard.PressAsync("Escape");
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
            catch (TimeoutException)
            {
                throw new PopupTimeoutException(PopupName, "appear", timeoutMs);
            }
        }
      
        protected virtual async Task ClickPopupContinue()
        {
            foreach (var selector in PopupContinueSelectors)
            {
                var locator = CreateLocator(selector);
                if (await TryClickElement(locator))
                    return;
            }
            
            var selectorDetails = string.Join(", ", PopupContinueSelectors.Select((sel, idx) => $"#{idx + 1}: '{sel}'"));
            throw new PopupTimeoutException(PopupName, $"click Continue/Save/Next button. Tried {PopupContinueSelectors.Length} selector(s): [{selectorDetails}]. Page URL: {Page.Url}", 0);
        }

        protected virtual async Task<bool> TryClickCloseButton()
        {
            var closeButtonLocator = Page.Locator(PopupLocators.CloseButton);
            return await TryClickElement(closeButtonLocator);
        }

        protected virtual async Task<bool> TryClickOutsidePopup()
        {
            try
            {
                // Try clicking on backdrop/overlay
                var backdropLocator = Page.Locator(PopupLocators.OverlayBackdrop);
                if (await backdropLocator.CountAsync() > 0)
                {
                    await backdropLocator.ClickAsync();
                    return true;
                }

                // Fallback: click outside modal area
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

        /// <summary>
        /// Same as <see cref="CreateLocator"/> but constrained to the popup's own modal
        /// container, so a generic selector cannot resolve to a control on the page behind the
        /// modal - which is what made a bare 'Save'/'Continue' selector click the wrong button.
        /// </summary>
        protected virtual ILocator CreatePopupScopedLocator(string locatorValue)
        {
            // .First: ModalContainer is an OR over several class/role conventions, so a nested
            // modal can match more than once. The outermost match still holds every popup control.
            var container = Page.Locator(PopupLocators.ModalContainer).First;

            // './/' rather than '//': an absolute XPath would escape the container and re-match
            // the whole document, quietly undoing the scoping. Spelling the '.' out keeps this
            // independent of how Playwright rewrites chained XPath.
            return locatorValue.StartsWith("//")
                ? container.Locator($"xpath=.{locatorValue}")
                : container.Locator(locatorValue);
        }
    }
}
