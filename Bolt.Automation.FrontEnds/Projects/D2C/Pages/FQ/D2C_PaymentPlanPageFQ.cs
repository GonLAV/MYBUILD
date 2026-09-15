using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using D2CBase = Bolt.Automation.FrontEnds.Projects.D2C.Base.D2CBase;

namespace Bolt.Automation.FrontEnds.Projects.D2C.Pages
{
    public class D2C_PaymentPlanPageFQ(
        IBrowserManager browserManager,
        IPageHelper pageHelper,
        IScopeContext scopeContext,
        bool validatePageReady = true,
        IAutomationLogger? logger = null)
        : D2CBase(browserManager, pageHelper, scopeContext, validatePageReady, logger)
    {
        #region Locators
        private const string PayByCardButtonLocator = "//button[text()=' Pay by card ']";
        private const string PayWithEscrowButtonLocator = "//button[text()=' Pay with Escrow ']";
        private const string FirstPaymentPlanLocator =
            "ul.payment-plan-wrapper li.payment-plan-single:first-child div.header";
        #endregion

        #region Page Properties
        protected override string PageIdentifier => "payment-plan";
        protected override string PageName => "Payment Plan Full Quote Page";
        #endregion

        public override async Task FillForm(Dictionary<string, string>? formData = null)
        {
            await ClickOnPayByCard();
            await SelectFirstPaymentPlan();
        }

        public async Task SelectFirstPaymentPlan()
        {
            await PageHelper.InteractWithElement(
                LocatorType.CSS,
                FirstPaymentPlanLocator,
                ElementAction.Click
            );

            _logger?.LogUiAction("Click", "PaymentPlan", "Selected the first payment plan");
        }

        /// <summary>
        /// Clicks on the "Pay by card" button
        /// </summary>
        public async Task ClickOnPayByCard()
        {
            await PageHelper.InteractWithElement(
                LocatorType.XPath,
                PayByCardButtonLocator,
                ElementAction.Click
            );
            
            _logger?.LogUiAction("Click", "PayByCard", "Clicked Pay by card button");
        }

        /// <summary>
        /// Clicks on the "Pay with Escrow" button
        /// </summary>
        public async Task ClickOnPayWithEscrow()
        {
            await PageHelper.InteractWithElement(
                LocatorType.XPath,
                PayWithEscrowButtonLocator,
                ElementAction.Click
            );
            
            _logger?.LogUiAction("Click", "PayWithEscrow", "Clicked Pay with Escrow button");
        }
    }
}
