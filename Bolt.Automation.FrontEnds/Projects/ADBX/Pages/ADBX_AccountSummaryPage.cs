using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Enums;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.ADBX.Base;

namespace Bolt.Automation.FrontEnds.Projects.ADBX.Pages
{
    public class ADBX_AccountSummaryPage(IBrowserManager browserManager, IPageHelper pageHelper, IScopeContext scopeContext, bool validatePageReady = true, IAutomationLogger? logger = null)
        : ADBX_BasePage(browserManager, pageHelper, scopeContext,  validatePageReady, logger)
    {
        protected override string PageIdentifier => "account";
        protected override string PageName => "Accounts Summary Page";
        public const string NewQuote = "//app-account//span[contains(.,'NEW QUOTE')]";
        public const string FillInManually = "FILL IN MANUALLY";
        private const string NewQuoteMenuPanel = ".mat-mdc-menu-panel";

        public async Task ClickOnNewQuoteFromExistingAccount(bool updatecontext = true)
        {
            await PageHelper.InteractWithElement(
                LocatorType.XPath,
                NewQuote,
                ElementAction.Click
            );

            // Some agents show a creation-method dropdown after clicking NEW QUOTE
            if (await PageHelper.ElementExists(LocatorType.CSS, NewQuoteMenuPanel, timeout: 1500))
            {
                await PageHelper.InteractWithElement(
                    LocatorType.Text,
                    FillInManually,
                    ElementAction.Click
                );
            }

            await BrowserManager.SwitchToLastTabAsync(10000);

            if (updatecontext)
            {
                ScopeContext.Set(ctx => ctx.FrontEnd, FrontEndType.Interview);
            }
        }
    }
}
