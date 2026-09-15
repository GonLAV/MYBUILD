using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Exceptions;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.FormData.Enums;
using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.Interview.Base;
using Microsoft.Playwright;

namespace Bolt.Automation.FrontEnds.Projects.Interview.Pages
{
    public class Product_PaymentIntegrationPage : InterviewBase
    {
        protected override string PageIdentifier => "lemonade";
        protected override string PageName => "lemonade Payment Integration Page";

        #region Locators
        private const string CheckBoxTermsServiceLocator = "[id='tservice']";
        private const string CheckBoxGivebackLocator = "[id='giveback']";
        private const string CardNumberLocator = "//input[@id='cardNumber']";
        private const string ExpDateLocator = "//input[@id='expDate']";
        private const string CvcLocator = "//input[@name='cvc']";
        private const string SubmitButtonLocator = "//button[@id='submit-button']";
        private const string SubmitButtonDisabledLocator = "//button[@id='submit-button' and @disabled]";
        private const string GivebackLinkLocator = "//a[contains(text(),'Giveback')]";
        private const string TermsOfServiceLinkLocator = "//a[contains(text(),'terms of service')]";
        private const string TotalAmountLocator = "//div[@class='payment-plan']/div[contains(@class,'payment')]//label[text()='Total amount']/following-sibling::div";
        private const string PayNowLocator = "//div[@class='payment-plan']/div[contains(@class,'payment')]//label[text()='Pay now']/following-sibling::div";
        private const string PaymentTypeLocator = "//div[@class='payment-description']";
        private const string FirstNameLocator = "//input[@id='fname']";
        private const string LastNameLocator = "//input[@id='surname']";
        #endregion

        public Product_PaymentIntegrationPage(
            IBrowserManager browserManager,
            IPageHelper pageHelper,
            IScopeContext scopeContext,
            bool validatePageReady = true,
            IAutomationLogger? logger = null)
            : base(browserManager, pageHelper, scopeContext, validatePageReady, logger)
        {
        }

        public async Task SelectCheckBoxTermsService()
        {
            var isChecked = await Page.EvaluateAsync<bool>("() => document.getElementById('tservice').checked");
            if (!isChecked)
                await PageHelper.InteractWithElement(LocatorType.CSS, CheckBoxTermsServiceLocator, ElementAction.Click);
        }

        public async Task SelectCheckBoxGiveback()
        {
            var isChecked = await Page.EvaluateAsync<bool>("() => document.getElementById('giveback').checked");
            if (!isChecked)
                await PageHelper.InteractWithElement(LocatorType.CSS, CheckBoxGivebackLocator, ElementAction.Click);
        }

        public async Task<bool> IsCompleteOrderBtnEnabled()
        {
            var disabledBtn = Page.Locator(SubmitButtonDisabledLocator);
            return await disabledBtn.CountAsync() == 0;
        }

        public async Task ClickOnGivebackLink()
        {
            await PageHelper.InteractWithElement(LocatorType.XPath, GivebackLinkLocator, ElementAction.Click);
        }

        public async Task ClickOnTermsOfServiceLink()
        {
            await PageHelper.InteractWithElement(LocatorType.XPath, TermsOfServiceLinkLocator, ElementAction.Click);
        }

        /// <summary>
        /// Submits the payment and returns the URL the provider redirected to - read off the navigation
        /// request, so an unresolvable destination still yields the target. See kb framework:navigation-waits.
        /// </summary>
        // 45s, not the usual 20: the provider settles the payment before it redirects, and a green run
        // has already been seen at 17.7s.
        public async Task<string> ClickNextAndGetRedirectUrlAsync(string redirectUrlPart, int timeout = 45000)
        {
            try
            {
                var request = await Page.RunAndWaitForRequestAsync(
                    async () => await Page.Locator(SubmitButtonLocator).ClickAsync(),
                    r => r.IsNavigationRequest && r.Url.Contains(redirectUrlPart, StringComparison.OrdinalIgnoreCase),
                    new PageRunAndWaitForRequestOptions { Timeout = timeout });

                _logger?.Info($"Payment redirect requested: [{request.Url}]");
                return request.Url;
            }
            catch (TimeoutException ex)
            {
                throw new NavigationException(
                    $"Payment was submitted but the browser never requested a redirect containing " +
                    $"'{redirectUrlPart}' within {timeout}ms - the provider did not redirect. " +
                    $"Current page: [{Page.Url}]", ex);
            }
        }

        public async Task<bool> IsTotalAmountDisplayed(string totalAmount)
        {
            var element = Page.Locator(TotalAmountLocator);
            if (await element.CountAsync() == 0) return false;
            return (await element.InnerTextAsync()).Contains(totalAmount);
        }

        public async Task<bool> IsPayNowDisplayed(string price)
        {
            var element = Page.Locator(PayNowLocator);
            if (await element.CountAsync() == 0) return false;
            return (await element.InnerTextAsync()).Contains(price);
        }

        public async Task<bool> IsPaymentDescriptionDisplayed(string paymentType)
        {
            var element = Page.Locator(PaymentTypeLocator);
            if (await element.CountAsync() == 0) return false;
            return (await element.InnerTextAsync()).Contains(paymentType);
        }

        public async Task<bool> IsFirstNameDisplayed(string firstName)
        {
            var element = Page.Locator(FirstNameLocator);
            if (await element.CountAsync() == 0) return false;
            return (await element.GetAttributeAsync("value") ?? string.Empty).Contains(firstName);
        }

        public async Task<bool> IsLastNameDisplayed(string lastName)
        {
            var element = Page.Locator(LastNameLocator);
            if (await element.CountAsync() == 0) return false;
            return (await element.GetAttributeAsync("value") ?? string.Empty).Contains(lastName);
        }

        public async Task SetCardNumber(string cardNumber)
        {
            await PageHelper.InteractWithElement(LocatorType.XPath, CardNumberLocator, UIFieldType.Input, cardNumber);
        }

        public async Task SetExpirationDate(string expirationDate)
        {
            await PageHelper.InteractWithElement(LocatorType.XPath, ExpDateLocator, UIFieldType.Input, expirationDate);
        }

        public async Task SetCVC(string cvc)
        {
            await PageHelper.InteractWithElement(LocatorType.XPath, CvcLocator, UIFieldType.Input, cvc);
        }
    }
}