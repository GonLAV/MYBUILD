using System.Text.Json;
using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.FormData;
using Bolt.Automation.FrontEnds.FormData.Helpers;
using Bolt.Automation.FrontEnds.Interfaces;
using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.D2C.FormData;
using Microsoft.Playwright;

namespace Bolt.Automation.FrontEnds.Projects.D2C.Base
{
    public abstract class D2CBase : IInterview
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

        protected static class CommonLocators
        {
            public const string ContinueButton = "button[type=\"button\"][data-automation-element=\"next-button-element\"]";
            public const string Loader = ".loader, .loading-overlay, .spinner";
            public const string ErrorMessage = ".error-message, .field-error, .form-error, .validation-error";
            public const string MandatoryFields = ".ng-invalid:visible, .error-message:visible, [aria-invalid='true'], .has-error";
        }

        private static readonly string[] ContinueButtonSelectors = new[]
        {
            CommonLocators.ContinueButton,
            "//next-button//button",
            "//button[@aria-label='Continue']",
            "//button[@aria-label='Next step']",
            "//div[@class='next-button-wrapper']//button",
            "//button[@data-automation-element='next-button-element']",
            "button.app-button"
        };

        // Expose the D2C field registry for all derived pages
        protected virtual Dictionary<string, UIElement> FieldRegistry => FieldRegistryD2C.Fields;

        // Helper for field access with value - avoids Field(name)[value] syntax
        protected UIElement FieldWithValue(string fieldName, string value) => FieldRegistry[fieldName][value];

        protected D2CBase(
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
            try
            {
                await PageHelper.ValidatePageReadyAsync(PageIdentifier, PageName,30000);
                await CaptureApplicationIdsFromSessionStorageAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Reads friendlyId, applicationId (→ExternalId), and applicantId from browser sessionStorage.
        /// Called automatically after each page validates as ready. Safe when IDs are not yet present.
        /// </summary>
        protected async Task CaptureApplicationIdsFromSessionStorageAsync()
        {
            try
            {
                var metadata = await BrowserStorageHelper.GetSessionStorageJsonElementAsync(Page, "state", "interviewMetadata", _logger);
                if (metadata == null)
                    return;

                var friendlyId = metadata.Value.TryGetProperty("friendlyId", out var fid) ? fid.GetString() : null;
                var applicationId = metadata.Value.TryGetProperty("applicationId", out var aid) ? aid.GetString() : null;
                var applicantId = metadata.Value.TryGetProperty("applicantId", out var apid) ? apid.GetString() : null;

                var hasNewData = (!string.IsNullOrEmpty(friendlyId) && friendlyId != ScopeContext.Data.FriendlyId)
                              || (!string.IsNullOrEmpty(applicationId) && applicationId != ScopeContext.Data.ExternalId)
                              || (!string.IsNullOrEmpty(applicantId) && applicantId != ScopeContext.Data.ApplicantId);

                if (!hasNewData)
                    return;

                if (!string.IsNullOrEmpty(friendlyId)) ScopeContext.Set(ctx => ctx.FriendlyId, friendlyId);
                if (!string.IsNullOrEmpty(applicationId)) ScopeContext.Set(ctx => ctx.ExternalId, applicationId);
                if (!string.IsNullOrEmpty(applicantId)) ScopeContext.Set(ctx => ctx.ApplicantId, applicantId);

                _logger?.Info($"D2C Session IDs — FriendlyId: {friendlyId} | ApplicationId: {applicationId} | ApplicantId: {applicantId}");

                ScopeContext.Data.IdentifierHistory.Add(new IdentifierSnapshot
                {
                    Source = "SessionStorage",
                    FriendlyId = friendlyId,
                    ExternalId = applicationId,
                    ApplicantId = applicantId
                });
            }
            catch (Exception ex)
            {
                _logger?.Debug($"Could not capture D2C session IDs: {ex.Message}");
            }
        }

        public virtual async Task ClickContinue()
        {
            await ClickContinueButton(true);
        }

        public async Task ClickContinueButton(bool throwErrorMessage = true)
        {
            try
            {
                if (await TryStandardButtonClick() || await ClickWithJavaScript())
                {
                    return;
                }

                if (throwErrorMessage)
                {
                    var exception = new Exception($"Could not find or click a Next/Continue button on the {PageName} page");
                    _logger?.LogException(exception, "Continue button interaction failed");
                    _logger?.LogBusinessRule("ContinueButtonAvailable", false, "No clickable continue button found on page");
                    throw exception;
                }

                _logger?.Warning($"Continue button not found on {PageName} but not throwing error due to throwErrorMessage=false");
            }
            catch (Exception ex)
            {
                _logger?.LogException(ex, "Failed to click continue button on {0}", PageName);
                throw;
            }
        }

        private async Task<bool> TryStandardButtonClick()
        {
            foreach (var selector in ContinueButtonSelectors)
            {
                try
                {
                    var locator = CreateLocator(selector);
                    if (await TryClickingElements(locator))
                    {
                        _logger?.Debug($"Continue button clicked using selector: {selector}");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    _logger?.Debug($"Failed to click continue button with selector '{selector}': {ex.Message}");
                }
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
                catch (Exception ex)
                {
                    _logger?.Debug($"Failed to click button element {i}: {ex.Message}");
                }
            }
            return false;
        }

        // Click a specific button using JavaScript, handling multiple matches
        private async Task<bool> ClickWithJavaScript(string specificSelector = null)
        {
            try
            {
                _logger?.Debug("Attempting JavaScript-based continue button click");

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
                    "            const selectors = [\"button[aria-label=\\\"Next step\\\"]\", \"button[aria-label=\\\"Continue\\\"]\", \"button[data-automation-element=\\\"next-button-element\\\"]\", \"button.app-button\", \"next-button button\", \".next-button-wrapper button\"];\n" +
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

                bool result = await Page.EvaluateAsync<bool>(js, specificSelector);

                if (result)
                {
                    _logger?.LogUiAction("JavaScriptClick", "ContinueButton", "JavaScript continue button click succeeded");
                }
                else
                {
                    _logger?.Debug("JavaScript continue button click failed - no clickable elements found");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger?.LogException(ex, "JavaScript continue button click failed");
                return false;
            }
        }

        protected virtual async Task WaitForLoaderToDisappear()
        {
            try
            {
                const int maxAttempts = 50;
                const int delayMs = 200;

                for (int attempt = 0; attempt < maxAttempts; attempt++)
                {
                    bool loaderExists = await PageHelper.ElementExists(LocatorType.CSS, CommonLocators.Loader, timeout: 100);

                    if (!loaderExists)
                    {
                        _logger?.Debug("Loader disappeared successfully");
                        return;
                    }

                    if (attempt == 0)
                    {
                        _logger?.Debug("Loader found, waiting for it to disappear");
                    }

                    await Task.Delay(delayMs);
                }

                _logger?.Warning("Loader did not disappear within timeout");
            }
            catch (Exception ex)
            {
                _logger?.LogException(ex, "Failed to wait for loader to disappear on {0}", PageName);
            }
        }

        public virtual async Task FillForm(Dictionary<string, string>? formData = null)
        {
            var (pageSpecificData, fields) = FormDataHelper.GetFieldsForPage(this, ScopeContext, formData);
            await Page.FillRelevantFields(pageSpecificData, fields, ScopeContext, PageHelper, _logger);
        }
        public async Task<bool> IsContinueButtonDisabled()
        {
            try
            {
                foreach (var selector in ContinueButtonSelectors)
                {
                    var locator = CreateLocator(selector);
                    if (await locator.CountAsync() > 0)
                    {
                        return !await IsButtonEnabled(locator);
                    }
                }
                return true;
            }
            catch
            {
                return true;
            }
        }

        protected async Task<bool> IsButtonEnabled(ILocator buttonLocator)
        {
            try
            {
                if (await buttonLocator.CountAsync() == 0)
                {
                    _logger?.Debug("Button not found when checking if enabled");
                    return false;
                }

                bool isEnabled = !await buttonLocator.EvaluateAsync<bool>("el => el.disabled || el.getAttribute('disabled') === 'true' || el.closest('button')?.getAttribute('disabled') === 'true'");
                _logger?.Debug($"Button enabled state: {isEnabled}");
                return isEnabled;
            }
            catch (Exception ex)
            {
                _logger?.LogException(ex, "Failed to check if button is enabled");
                return false;
            }
        }

        public async Task<string> GetFriendlyId()
        {
            try
            {
                var friendlyId = await BrowserStorageHelper.GetSessionStorageJsonValueAsync(Page, "state", "interviewMetadata.friendlyId", _logger);
                return friendlyId ?? "NOT Found";
            }
            catch (Exception ex)
            {
                _logger?.LogException(ex, "Failed to get friendly ID");
                return "NOT Found";
            }
        }

        public async Task ClickBackButton()
        {
            try
            {
                _logger?.LogUiAction("Click", "BackButton", $"Clicking browser back button on {PageName}");
                await PageHelper.ClickBrowserBackButton();
            }
            catch (Exception ex)
            {
                _logger?.LogException(ex, "Failed to click back button on {0}", PageName);
                throw;
            }
        }

        public async Task GoBackButton()
        {
            try
            {
                var backBtnLocator = "//section[not(@hidden)][@class='ng-star-inserted']//button[text() = 'Go back']";
                _logger?.LogUiAction("Click", "GoBackButton", $"Clicking 'Go back' button on {PageName}");

                await PageHelper.InteractWithElement(
                    LocatorType.XPath,
                    backBtnLocator,
                    ElementAction.Click);

                _logger?.LogUiAction("Click", "GoBackButton", "'Go back' button clicked successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogException(ex, "Failed to click 'Go back' button on {0}", PageName);
                throw;
            }
        }

    }
}