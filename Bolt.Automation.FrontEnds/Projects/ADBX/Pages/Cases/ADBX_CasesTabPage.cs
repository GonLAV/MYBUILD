using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.ADBX.Base;
using Bolt.Automation.FrontEnds.Projects.ADBX.Popups;

namespace Bolt.Automation.FrontEnds.Projects.ADBX.Pages.Cases
{
    public class ADBX_CasesTabPage : ADBX_BasePage
    {
        protected override string PageIdentifier => "cases";
        protected override string PageName => "Cases Grid Page";

        public ADBX_CasesTabPage(IBrowserManager browserManager, IPageHelper pageHelper, IScopeContext scopeContext, bool validatePageReady = true, IAutomationLogger? logger = null)
        : base(browserManager, pageHelper, scopeContext, validatePageReady, logger)
        {
            WaitForLoaderToDisappear().GetAwaiter().GetResult();
        }

        public async Task<bool> IsQueueDisplayed(string queueName)
        {
            var notes = Page.Locator($"//app-tabs//span[contains(text(), '{queueName}')]");
            return await notes.CountAsync() > 0;
        }

        public async Task<ADBX_AddEditViewPopup> ClickEditView(string viewName)
        {
            var expandElementLocator = "//button[contains(@class, 'more')]/../following-sibling::ul";
            var expandElement = Page.Locator(expandElementLocator);
            bool hasToggledClass = await expandElement.EvaluateAsync<bool>("el => el.classList.contains('toggled')");

            if (!hasToggledClass)
            {
                var moreButtonLocator = "//button[contains(@class, 'more')]";
                await PageHelper.InteractWithElement(
                LocatorType.XPath,
                moreButtonLocator,
                ElementAction.Click);
            }

            var viewElementLocator = $"//span[contains(text(), '{viewName}')]";

            await PageHelper.InteractWithElement(
                LocatorType.XPath,
                viewElementLocator,
                ElementAction.Hover);

            var editButtonLocator = $"//span[contains(text(), '{viewName}')]/../..//div[contains(@class, 'edit')]";

            await PageHelper.InteractWithElement(
                LocatorType.XPath,
                editButtonLocator,
                ElementAction.Click);
            return new ADBX_AddEditViewPopup(BrowserManager, PageHelper, ScopeContext);

        }

    }
}
