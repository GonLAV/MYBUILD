using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.ADBX.Base;
using Bolt.Automation.FrontEnds.Projects.ADBX.FormData;
using Bolt.Automation.FrontEnds.Projects.ADBX.Popups;
using Microsoft.Playwright;

namespace Bolt.Automation.FrontEnds.Projects.ADBX.Pages
{
    public class ADBX_AccountsTabPage : ADBX_BasePage
    {
        protected override string PageIdentifier => "accounts";
        protected override string PageName => "Accounts Grid Page";

        private const string NewAccountButton = "//span[contains(text(), 'NEW ACCOUNT')]";

        public ADBX_AccountsTabPage(IBrowserManager browserManager, IPageHelper pageHelper, IScopeContext scopeContext, bool validatePageReady = true, IAutomationLogger? logger = null)
            : base(browserManager, pageHelper, scopeContext, validatePageReady, logger)
        {
            WaitForLoaderToDisappear().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Clicks the "NEW ACCOUNT" button on the Accounts grid and returns the
        /// "Enter Account Information" pop-up.
        /// </summary>
        public async Task<ADBX_EnterUpdateAccountInformationPopup> ClickOnNewAccount()
        {
            await PageHelper.InteractWithElement(
                LocatorType.XPath,
                NewAccountButton,
                ElementAction.Click
            );
            await WaitForLoaderToDisappear();

            return new ADBX_EnterUpdateAccountInformationPopup(BrowserManager, PageHelper, ScopeContext);
        }

        /// <summary>
        /// Clicks the Commercial Lines tab, waits for it to become active and for the
        /// table to finish loading, then selects the specified row.
        /// Use this instead of InteractWithField + SelectTableRowAsync to avoid picking
        /// a PL customer when the tab switch is still in-flight.
        /// </summary>
        public async Task SelectCommercialLinesAccountAsync(int rowIndex = 1)
        {
            await _pageHelper.InteractWithField(ADBX_FieldNames.CommercialLinesTab);
            // Wait for the CL tab button to gain the "active" CSS class before reading the table.
            await Page.WaitForSelectorAsync(
                "li#tab-CL button.tab-button.active",
                new PageWaitForSelectorOptions { Timeout = DefaultTimeout });
            await WaitForLoaderToDisappear();
            await _pageHelper.SelectTableRowAsync(rowIndex);
        }
    }
}
