using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Exceptions;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.FormData;
using Bolt.Automation.FrontEnds.Interfaces;
using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.HQXAgent.FormData;
using Bolt.Automation.FrontEnds.Projects.HQXAgent.Popups;
using Microsoft.Playwright;

namespace Bolt.Automation.FrontEnds.Projects.HQXAgent.Base
{
    public abstract class HQXAgentBase : IInterview
    {
        protected readonly IPageHelper _pageHelper;
        public IPageHelper PageHelper => _pageHelper;
        protected readonly IBrowserManager BrowserManager;
        protected readonly IAutomationLogger? _logger;
        protected readonly IScopeContext ScopeContext;
        private IPage? _page;
        public IPage Page => _page ??= BrowserManager.GetCurrentTab()!;

        protected abstract string PageIdentifier { get; }
        protected abstract string PageName { get; }
        protected virtual int PageTimeout => 30000;
        // PGR-specific common locators
        protected static class CommonLocators
        {
            // PGR typically uses different automation elements than D2C
            public const string ContinueButton = "button[aria-label='Continue'], button[data-testid='continue-btn'], .continue-button";
            public const string NextButton = "button[aria-label='Next'], button[data-testid='next-btn'], .next-button";
            public const string Loader = ".loading, .loader, .spinner, .progress-indicator";
            public const string ErrorMessage = ".error, .error-message, .validation-error, [role='alert']";
            public const string MandatoryFields = ".required:invalid, .field-error, [aria-invalid='true']";
            public const string QuoteButton = ".quote-button, .get-quote-btn, button[data-testid='quote']";
            public const string BackButton = "button[aria-label='Back'], .back-button, .previous-button";
            public const string EditButton = "//button[contains(text(),'Edit Quote')]";
            public const string StopEditButton = "//button[contains(text(),'Stop Edit')]";
        }

        // PGR-specific selectors for continue/next buttons
        private static readonly string[] ContinueButtonSelectors = new[]
        {
            CommonLocators.ContinueButton,
            CommonLocators.NextButton,
            "//button[contains(text(), 'Continue')]",
            "//button[contains(text(), 'Next')]",
            "//button[@data-testid='continue-btn']",
            "//button[@data-testid='next-btn']",
            "//input[@type='submit' and @value='Continue']",
            "//input[@type='submit' and @value='Next']",
            ".btn-primary:has-text('Continue')",
            ".btn-primary:has-text('Next')"
        };

        // Expose the PGR field registry for all derived pages
        protected virtual Dictionary<string, UIElement> FieldRegistry => FieldRegistryHQXAgent.Fields;

        // Helper for easy field access
        protected UIElement Field(string fieldName) => FieldRegistry[fieldName];

        // Helper for field access with value - avoids Field(name)[value] syntax
        protected UIElement FieldWithValue(string fieldName, string value) => FieldRegistry[fieldName][value];

        protected HQXAgentBase(
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

        public virtual async Task ValidatePageReady()
        {
            await PageHelper.ValidatePageReadyAsync(PageIdentifier, PageName, PageTimeout);
        }

        public virtual async Task ClickContinue()
        {
            await ClickContinueButton(true);
        }

        public async Task ClickEditQuote()
        {
            await PageHelper.InteractWithElement(
                LocatorType.XPath,
                CommonLocators.EditButton,
                ElementAction.Click
            ); 
        }

        public async Task ClickStopEdit()
        {
            await PageHelper.InteractWithElement(
                LocatorType.XPath,
                CommonLocators.StopEditButton,
                ElementAction.Click
            );
        }

        public async Task ClickContinueButton(bool throwErrorMessage = true)
        {
            if (!(await TryStandardButtonClick() || await ClickWithJavaScript()))
            {
                if (throwErrorMessage)
                {
                    throw new PageElementException("Next/Continue button", $"Could not find or click any continue button on PGR page: {Page.Url}");
                }
            }
        }

        private async Task<bool> TryStandardButtonClick()
        {
            foreach (var selector in ContinueButtonSelectors)
            {
                var locator = CreateLocator(selector);
                if (await TryClickingElements(locator))
                    return true;
            }
            return false;
        }

        private ILocator CreateLocator(string locatorValue)
        {
            return locatorValue.StartsWith("//")
                ? Page.Locator($"xpath={locatorValue}")
                : Page.Locator(locatorValue);
        }

        private async Task<bool> TryClickingElements(ILocator locator)
        {
            int count = await locator.CountAsync();
            for (int i = 0; i < count; i++)
            {
                try
                {
                    var specificButton = locator.Nth(i);
                    if (!await specificButton.IsVisibleAsync())
                        continue;
                    await specificButton.ClickAsync(new() { Timeout = 5000 });
                    return true;
                }
                catch
                {
                    // Optionally log
                }
            }
            return false;
        }

        private async Task<bool> ClickWithJavaScript(string? specificSelector = null)
        {
            try
            {
                string js = @"(selector = null) => {\n" +
                    "    try {\n" +
                    "        let elementsToTry = [];\n" +
                    "        if (selector) {\n" +
                    "            if (selector.startsWith('//')) {\n" +
                    "                const result = document.evaluate(selector, document, null, XPathResult.ORDERED_NODE_SNAPSHOT_TYPE, null);\n" +
                    "                for (let i = 0; i < result.snapshotLength; i++) {\n" +
                    "                    elementsToTry.push(result.snapshotItem(i));\n" +
                    "                }\n" +
                    "            } else {\n" +
                    "                elementsToTry = Array.from(document.querySelectorAll(selector));\n" +
                    "            }\n" +
                    "        } else {\n" +
                    "            const selectors = [\"button[aria-label='Continue']\", \"button[aria-label='Next']\", \"button[data-testid='continue-btn']\", \"button[data-testid='next-btn']\", \".continue-button\", \".next-button\", \".btn-primary\"];\n" +
                    "            for (let sel of selectors) {\n" +
                    "                const buttons = document.querySelectorAll(sel);\n" +
                    "                if (buttons.length > 0) {\n" +
                    "                    elementsToTry = elementsToTry.concat(Array.from(buttons));\n" +
                    "                }\n" +
                    "            }\n" +
                    "        }\n" +
                    "        const isClickable = (element) => {\n" +
                    "            const isVisible = element.offsetWidth > 0 && element.offsetHeight > 0 && !element.hasAttribute('hidden') && element.offsetParent !== null && window.getComputedStyle(element).display !== 'none';\n" +
                    "            if (!isVisible) { return false; }\n" +
                    "            const isEnabled = !element.disabled && !element.hasAttribute('disabled') && element.getAttribute('aria-disabled') !== 'true';\n" +
                    "            return isEnabled;\n" +
                    "        };\n" +
                    "        for (let i = 0; i < elementsToTry.length; i++) {\n" +
                    "            const element = elementsToTry[i];\n" +
                    "            if (isClickable(element)) {\n" +
                    "                element.click();\n" +
                    "                return true;\n" +
                    "            }\n" +
                    "        }\n" +
                    "        return false;\n" +
                    "    } catch (e) {\n" +
                    "        return false;\n" +
                    "    }\n" +
                    "}";
                return await Page.EvaluateAsync<bool>(js, specificSelector);
            }
            catch
            {
                return false;
            }
        }

        protected virtual async Task WaitForLoaderToDisappear()
        {
            try
            {
                var loaderLocator = Page.Locator(CommonLocators.Loader);
                if (await loaderLocator.CountAsync() > 0)
                {
                    await PageHelper.WaitForElementToDisappearAsync(loaderLocator);
                }
            }
            catch
            {
                // Optionally log
            }
        }

        protected virtual async Task<int> GetValidationErrorCount()
        {
            var errorFields = Page.Locator(CommonLocators.MandatoryFields);
            return await errorFields.CountAsync();
        }

        public abstract Task FillForm(Dictionary<string, string>? formData = null);

        protected async Task<bool> IsButtonEnabled(ILocator buttonLocator)
        {
            if (await buttonLocator.CountAsync() == 0)
                return false;
            return !await buttonLocator.EvaluateAsync<bool>("el => el.disabled || el.getAttribute('disabled') === 'true' || el.closest('button')?.getAttribute('disabled') === 'true'");
        }

        /// <summary>
        /// Returns <c>true</c> when the page-level Continue button is enabled and clickable.
        /// After a successful sold note submission the interview is locked and this returns <c>false</c>.
        /// </summary>
        public Task<bool> IsContinueButtonEnabledAsync()
            => IsButtonEnabled(Page.Locator(CommonLocators.ContinueButton));

        /// <summary>
        /// Returns the number of interactive form inputs inside <c>app-overview</c> that are
        /// NOT locked. The Angular app signals a locked/read-only field by setting
        /// <c>tabindex="-1"</c> on the input (and adding <c>edit-mode-disabled</c> to the
        /// wrapper), rather than using the HTML <c>disabled</c> attribute.
        /// After a successful sold note submission all interview inputs should be locked,
        /// so the expected value is 0.
        /// </summary>
        public async Task<int> GetEnabledFormInputCountAsync()
            => await Page.Locator("app-overview input:not([type='hidden']):not([tabindex='-1'])").CountAsync();

        public async Task ClickBackButton()
        {
            await PageHelper.ClickBrowserBackButton();
        }

        /// <summary>
        /// On the Agent (PAA) Interview, each prefilled/assumed answer carries an "Assumed / Verified"
        /// switch next to its question, rendered as <c>&lt;… class="switch" name="{field}-verified-switch"&gt;</c>.
        /// Returns <c>true</c> when that switch is displayed for the given <paramref name="fieldName"/>
        /// (e.g. UtilitiesUpdated → name="UtilitiesUpdated-verified-switch"), so it is scoped to that
        /// question and not to any other verified switch on the page.
        /// </summary>
        public async Task<bool> IsAssumedVerifiedComponentDisplayedAsync(string fieldName)
        {
            var component = Page.Locator($"[name='{fieldName}-verified-switch']");
            return await component.CountAsync() > 0 && await component.First.IsVisibleAsync();
        }

        // Builds a Playwright locator for a registered field, formatting the {0} placeholder to empty
        // so it matches the field regardless of the selected value (e.g. all X_CompleteUpdate/X_NotUpdated
        // radios share the 'X_' fragment). Used by the conditional-reveal wait helpers below.
        private ILocator FieldLocator(string fieldName)
        {
            var field = Field(fieldName);
            return CreateLocator(field.GetLocators().First());
        }

        /// <summary>
        /// Waits for a conditionally-revealed question to appear and returns <c>true</c> once it is
        /// attached. Agent interview children are added/removed from the DOM (Angular *ngIf) after the
        /// parent answer changes, so this polls rather than checking presence immediately (which races
        /// the async reveal).
        /// </summary>
        public async Task<bool> WaitForQuestionVisibleAsync(string fieldName, int timeout = 10000)
        {
            try
            {
                await FieldLocator(fieldName).First.WaitForAsync(
                    new LocatorWaitForOptions { State = WaitForSelectorState.Attached, Timeout = timeout });
                return true;
            }
            catch (Exception ex) when (ex is PlaywrightException or TimeoutException)
            {
                return false;
            }
        }

        /// <summary>
        /// Waits for a conditionally-revealed question to be removed from the DOM and returns
        /// <c>true</c> once it is gone. The child *ngIf teardown after the parent flips to No is
        /// asynchronous, so an immediate presence check races it — this polls until detached.
        /// </summary>
        public Task<bool> WaitForQuestionHiddenAsync(string fieldName, int timeout = 10000)
            => PageHelper.WaitForElementToDisappearAsync(FieldLocator(fieldName), timeout);

        /// <summary>
        /// Clicks the "Create a Note" button in the PAA header menu, then waits for the
        /// Create Note popup to become visible and returns it ready for interaction.
        /// </summary>
        public async Task<HQXAgent_CreateNotePopup> ClickCreateNoteAsync()
        {
            _logger?.Info("Clicking 'Create a Note' button in PAA header.");
            await Page.Locator("menu ul li button", new PageLocatorOptions { HasText = "Create a Note" })
                      .ClickAsync();
            return new HQXAgent_CreateNotePopup(BrowserManager, _pageHelper, ScopeContext, logger: _logger);
        }

        protected virtual async Task<List<string>> GetFieldErrorMessages()
        {
            var errorMessages = new List<string>();
            var errorElements = await Page.Locator(CommonLocators.ErrorMessage).AllAsync();
            
            foreach (var element in errorElements)
            {
                var text = await element.TextContentAsync();
                if (!string.IsNullOrEmpty(text))
                    errorMessages.Add(text.Trim());
            }
            
            return errorMessages;
        }
    }
}