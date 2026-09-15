using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Microsoft.Playwright;
using D2CBase = Bolt.Automation.FrontEnds.Projects.D2C.Base.D2CBase;

namespace Bolt.Automation.FrontEnds.Projects.D2C.Pages
{
    public class D2C_PaymentPageFQ(IBrowserManager browserManager, IPageHelper pageHelper,IScopeContext scopeContext, bool validatePageReady = true, IAutomationLogger? logger = null) 
        : D2CBase(browserManager, pageHelper, scopeContext,validatePageReady, logger)
    {
        #region Locators
        private const string ChangeButtonLocator = "//button[normalize-space(text())='Change']";
        private const string IframeLocator = "iframe";
        private const string PolicyEffectiveDateLocator = "//div[@class='name' ]/b[text()='Policy effective date']//ancestor::div/div[@class='details-policy-summary']";
        private const string ContinueToPayDisabledLocator = "//button[@disabled]/span[contains(text(),'Continue to payment')]";
        private const string ContinueToPayLocator = "//button//span[contains(text(),'Continue to payment')] | //span[contains(text(),'Approve payment')] | //button//span[contains(text(),'Submit')]";
        #endregion

        #region Page Properties
        protected override string PageIdentifier => "payment";
        protected override string PageName => "Payment Full Quote Page";
        #endregion

        private IFrame? _innerFrame;

        public override async Task ValidatePageReady()
        {
            try
            {
                // Validate page is ready first
                await base.ValidatePageReady();
                
                // Initialize iframe reference
                await InitializeIframe();
                
                _logger?.LogBusinessRule("PageReady", true, $"{PageName} page validated successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogBusinessRule("PageReady", false, $"{PageName} page validation failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Initialize the iframe reference for payment forms
        /// </summary>
        private async Task InitializeIframe()
        {
            try
            {
                var iframeLocator = Page.Locator(IframeLocator);
                var count = await iframeLocator.CountAsync();

                if (count > 0)
                {
                    // Fix: Await the ContentFrameAsync method to get the IFrame instance
                    var iframeElementHandle = await iframeLocator.First.ElementHandleAsync();
                    if (iframeElementHandle != null)
                    {
                        var contentFrame = await iframeElementHandle.ContentFrameAsync();
                        if (contentFrame != null)
                        {
                            _innerFrame = contentFrame;
                            _logger?.Debug("Payment iframe initialized successfully");
                        }
                        else
                        {
                            _logger?.Warning("Unable to get content frame from iframe element");
                        }
                    }
                    else
                    {
                        _logger?.Warning("Unable to get iframe element handle");
                    }
                }
                else
                {
                    _logger?.Warning("No iframe found on payment page");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogException(ex, "Failed to initialize payment iframe");
            }
        }

        /// <summary>
        /// Gets the policy effective date text
        /// </summary>
        /// <returns>The policy effective date or "Not Found" if not available</returns>
        public async Task<string> GetPolicyEffectiveDate()
        {
            try
            {
                var text = await PageHelper.GetFieldValue(LocatorType.XPath, PolicyEffectiveDateLocator);
                return !string.IsNullOrEmpty(text) ? text.Trim() : "Not Found Policy Effective Date";
            }
            catch (Exception ex)
            {
                _logger?.LogException(ex, "Failed to get policy effective date");
                return "Not Found Policy Effective Date";
            }
        }

        /// <summary>
        /// Checks if the "Continue to Pay" button is disabled
        /// </summary>
        /// <returns>True if disabled, false otherwise</returns>
        public async Task<bool> IsContinueToPayBtnDisabled()
        {
            return await PageHelper.ElementExists(LocatorType.XPath, ContinueToPayDisabledLocator);
        }

        /// <summary>
        /// Clicks on the "Continue to Pay" button
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the button is disabled</exception>
        public async Task ClickOnContinueToPay()
        {
            if (await IsContinueToPayBtnDisabled())
            {
                var exception = new InvalidOperationException("Failed, Continue to pay btn is disabled.");
                _logger?.LogException(exception, "Continue to pay button is disabled");
                throw exception;
            }

            await PageHelper.InteractWithElement(
                LocatorType.XPath,
                ContinueToPayLocator,
                ElementAction.Click
            );
           
        }

        /// <summary>
        /// Clicks on the "Change" button to modify payment details
        /// </summary>
        /// <returns>A new instance of D2C_PaymentPlanPageFQ</returns>
        public async Task<D2C_PaymentPlanPageFQ> ClickOnChangePaymentButton()
        {
            await PageHelper.InteractWithElement(
                LocatorType.XPath,
                ChangeButtonLocator,
                ElementAction.Click
            );
            
            _logger?.LogUiAction("Click", "ChangePayment", "Clicked Change payment button");
            return new D2C_PaymentPlanPageFQ(BrowserManager, PageHelper,scopeContext, true, _logger);
        }
    }
}
