using System.Reflection.Metadata;
using System.Security.Cryptography.Xml;
using System.Threading;
using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Enums;
using Bolt.Automation.Common.Exceptions;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.ADBX.Base;
using Bolt.Automation.FrontEnds.Projects.ADBX.FormData;
using Bolt.Automation.FrontEnds.Projects.ADBX.Pages.Leads;
using Bolt.Automation.FrontEnds.Projects.ADBX.Popups;
using Microsoft.Playwright;

namespace Bolt.Automation.FrontEnds.Projects.ADBX.Pages.Quotes
{
    public class ADBX_QuoteSummaryPage(IBrowserManager browserManager, IPageHelper pageHelper, IScopeContext scopeContext, bool validatePageReady = true, IAutomationLogger? logger = null)
        : ADBX_BasePage(browserManager, pageHelper, scopeContext, validatePageReady, logger)
    {
        protected override string PageIdentifier => "quotes";
        protected override string PageName => "Quote Summary Page";

        private const string NewNoteBtnLocator = "//span[text()='NEW NOTE']";
        private const string NamedInsuredLocator = "//div[contains(text(),'Named Insured')]//following-sibling::div/span";
        private const string LeadLocator = "//div[contains(text(),'Lead')]//following-sibling::div/span";

        public async Task<ADBX_AddYourNoteInformationPopUp> ClickOnNewNote()
        {
            await PageHelper.InteractWithElement(
                LocatorType.XPath,
                NewNoteBtnLocator,
                ElementAction.Click);

            await WaitForLoaderToDisappear();

            return new ADBX_AddYourNoteInformationPopUp(BrowserManager, PageHelper, scopeContext);
        }

        public async Task<ADBX_AccountSummaryPage> ClickOnLinkedAccount()
        {
            await PageHelper.InteractWithElement(
                LocatorType.XPath,
                NamedInsuredLocator,
                ElementAction.Click);

            await WaitForLoaderToDisappear();

            return new ADBX_AccountSummaryPage(BrowserManager, PageHelper, ScopeContext, true, _logger);
        }

        public async Task<ADBX_LeadSummaryPage> ClickOnLinkedLead()
        {
            await PageHelper.InteractWithElement(
                LocatorType.XPath,
                LeadLocator,
                ElementAction.Click);

            await WaitForLoaderToDisappear();

            return new ADBX_LeadSummaryPage(BrowserManager, PageHelper, ScopeContext, true, _logger);
        }

        public async Task ClickOnEditQuote(bool updatecontext=true)
        {
            await PageHelper.InteractWithField(ADBX_FieldNames.EditButton);
            await BrowserManager.SwitchToLastTabAsync(10000);
            if (updatecontext)
            {
                ScopeContext.Set(ctx => ctx.FrontEnd, FrontEndType.Interview);
            }

        }

        public async Task<bool> IsTimelineNoteExistsADBX(string noteTitle, int timeout = 30000)
        {
            _logger?.Info($"Checking if note with title '{noteTitle}' exists in the timeline.");
            var noteLocator = Page.Locator($"//div[@class='note ng-star-inserted']//h3[contains(text(),'{noteTitle}')]");

            await noteLocator.First.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Attached,
                Timeout = timeout
            });

            var elementsCount = await noteLocator.CountAsync();
            return elementsCount > 0;
        }

        public async Task<string> GetTimeLineNoteBodyTextADBX(string noteTitle)
        {
            _logger?.Info($"getting first {noteTitle} note text");
            var noteBodyLocator = $"//div[@class='note ng-star-inserted']//h3[contains(text(),'{noteTitle}')]/following-sibling::p";
            var firstElement = Page.Locator(noteBodyLocator).First;
            return await firstElement.TextContentAsync() ?? "";
        }

        public async Task<string> GetDashboardDetailByTitle(string title)
        {
            var locator = $"//div[contains(text(),'{title}')]//following-sibling::div";

            var element = await PageHelper.WaitForElementAsync(Page.Locator(locator), timeout: 10000, waitForVisibility: true);

            if (element != null)
            {
                var value = await element.TextContentAsync();
                return value?.Trim() ?? throw new PageElementException(title, "value is null or empty");
            }

            throw new PageElementException(title, "element not found on page");
        }
    }
}
