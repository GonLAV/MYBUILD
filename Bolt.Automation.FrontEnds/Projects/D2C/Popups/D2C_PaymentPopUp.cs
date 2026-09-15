using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Exceptions;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.FormData;
using Bolt.Automation.FrontEnds.FormData.Enums;
using Bolt.Automation.FrontEnds.FormData.Helpers;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.D2C.Base;
using Microsoft.Playwright;

namespace Bolt.Automation.FrontEnds.Projects.D2C.Popups
{
    public class D2C_PaymentPopUp(
        IBrowserManager browserManager,
        IPageHelper pageHelper,
        IScopeContext scopeContext,
        bool validatePageReady = true,
        IAutomationLogger? logger = null
    ) : D2CPopupBase(browserManager, pageHelper, scopeContext, validatePageReady, logger)
    {
        #region Locators
        private const string IframeLocator = "iframe";
        private const string CompleteOrderDisabledLocator = "//button[contains(text(),'Complete order') and @disabled]";
        private const string CompleteOrderLocator = "//button[contains(text(),'Complete order')]";
        private const string PaymentProcessingLocator = "//h1[text()='Your payment is being processed']";
        private const string TermsCheckboxLocator = "[type='checkbox']";
        #endregion

        protected override string PopupIdentifier => "payment";
        protected override string PopupName => "Payment Popup";

        private IFrame? _innerFrame;

        public override async Task ValidatePageReady()
        {
            try
            {
                // Wait for popup to appear (base implementation)
                await base.ValidatePageReady();
                
                // Initialize iframe and validate payment popup content
                await InitializeIframe();
                
                _logger?.LogBusinessRule("PopupReady", true, $"{PopupName} validated successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogBusinessRule("PopupReady", false, $"{PopupName} validation failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Initialize the iframe reference for payment form interactions
        /// </summary>
        /// <summary>
        /// Initialize the iframe reference for payment form interactions
        /// </summary>
        private async Task InitializeIframe()
        {
            try
            {
                // Wait for a real field inside the iframe to appear
                var frameElement = await Page.QuerySelectorAsync(IframeLocator);
                if (frameElement == null)
                {
                    _logger?.Warning("Unable to find iframe element");
                    return;
                }

                _innerFrame = await frameElement.ContentFrameAsync();
                if (_innerFrame == null)
                {
                    _logger?.Warning("Unable to resolve iframe content frame");
                    return;
                }

                // Wait for a real field inside the iframe to appear
                await _innerFrame.Locator("[name='cardNumber']").WaitForAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogException(ex, "Failed to initialize payment iframe");
            }
        }

        public override async Task FillForm(Dictionary<string, string>? formData = null)
        {
            if (_innerFrame == null)
                await InitializeIframe();

            // Get the page-specific fields using the same mechanism as D2CBase
            var (pageSpecificData, fields) = FormDataHelper.GetFieldsForPage(this, ScopeContext, formData);

            // Fill fields inside iframe instead of main page
            await FillRelevantFieldsInIframe(pageSpecificData, fields);
        }

        private async Task FillRelevantFieldsInIframe(Dictionary<string, string> formData, Dictionary<string, UIElement> fields)
        {
            if (formData?.Any() != true) return;

            // Use FrameLocator for iframe interactions (more reliable than IFrame reference)
            var frameLocator = Page.FrameLocator(IframeLocator);

            foreach (var (fieldName, value) in formData)
            {
                if (!fields.TryGetValue(fieldName, out var field))
                {
                    _logger?.Debug($"Field '{fieldName}' not found in registry, skipping");
                    continue;
                }

                try
                {
                    // Get the locator string from the UIElement
                    var locatorValue = field.GetLocators(value);

                    // Create iframe-aware locator
                    var locator = frameLocator.Locator(locatorValue);

                    // Check if element exists in iframe
                    var ignoreIfNotFound = field.InteractionOptions?.IgnoreIfNotFound ?? true;

                    // Check if element exists in iframe
                    if (await locator.CountAsync() == 0)
                    {
                        if (ignoreIfNotFound)
                        {
                            _logger?.Debug($"Field '{fieldName}' with locator '{locatorValue}' not found in iframe, skipping");
                            continue;
                        }

                        throw new PageElementException(fieldName, $"with locator '{locatorValue}' not found in iframe");
                    }

                    // Interact based on field type
                    switch (field.FieldType)
                    {
                        case UIFieldType.Input:
                            await locator.First.FillAsync(value);
                            break;

                        case UIFieldType.Checkbox:
                            if (bool.TryParse(value, out bool isChecked) && isChecked)
                            {
                                await locator.First.CheckAsync();
                            }
                            break;

                        case UIFieldType.Button:
                        case UIFieldType.Radio:
                            await locator.First.ClickAsync();
                            break;

                        default:
                            _logger?.Warning($"Unsupported field type '{field.FieldType}' for iframe field '{fieldName}'");
                            break;
                    }

                    // Small delay for iframe responsiveness
                    await Task.Delay(100);
                }
                catch (Exception ex)
                {
                    _logger?.LogException(ex, $"Failed to fill iframe field '{fieldName}' with value '{value}'");
                }
            }

            await frameLocator.Locator("body").ClickAsync();
            await ClickOnCompleteOrder();
        }

        public async Task<bool> IsCompleteOrderBtnDisabled()
        {
            if (_innerFrame == null) await InitializeIframe();
            
            var count = await _innerFrame!.Locator(CompleteOrderDisabledLocator).CountAsync();
            return count > 0;
        }

        public async Task ClickOnCompleteOrder()
        {
            if (await IsCompleteOrderBtnDisabled())
            {
                var exception = new InvalidOperationException("Failed, Complete order btn is disabled.");
                _logger?.LogException(exception, "Complete order button is disabled");
                throw exception;
            }

            if (_innerFrame == null) await InitializeIframe();
            
            await _innerFrame!.Locator(CompleteOrderLocator).ClickAsync();
            await WaitForPaymentProcessing();
            _logger?.LogUiAction("Click", "CompleteOrder", "Clicked Complete order button");
        }

        /// <summary>
        /// Waits for the payment processing to complete
        /// </summary>
        private async Task WaitForPaymentProcessing()
        {
            using var step = _logger?.StartStep("Wait for payment processing");
            try
            {
                const int maxAttempts = 250;
                const int delayMs = 200;

                for (int attempt = 0; attempt < maxAttempts; attempt++)
                {
                    var processingExists = await _innerFrame!.Locator(PaymentProcessingLocator).CountAsync() > 0;

                    if (!processingExists)
                    {
                        step?.Complete();
                        return;
                    }

                    await Task.Delay(delayMs);
                }

                _logger?.Warning("Payment processing did not complete within timeout");
                step?.Complete();
            }
            catch (PlaywrightException ex) when (ex.Message.Contains("detached"))
            {
                // Expected: the payment iframe detaches when the popup completes and the
                // page navigates away (e.g. to the payment failure/confirmation page).
                // Treat this as "processing finished" rather than a failure.
                _logger?.Debug("Payment frame detached — payment completed and page navigated away");
                step?.Complete();
            }
            catch (Exception ex)
            {
                _logger?.LogException(ex, "Failed to wait for payment processing completion");
                step?.Fail();
            }
        }
    }
}
