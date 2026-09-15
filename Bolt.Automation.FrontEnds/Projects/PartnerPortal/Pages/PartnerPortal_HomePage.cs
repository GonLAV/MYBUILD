using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.PartnerPortal.Base;
using Bolt.Automation.FrontEnds.Projects.PartnerPortal.FormData;

namespace Bolt.Automation.FrontEnds.Projects.PartnerPortal.Pages
{
    public class PartnerPortal_HomePage(IBrowserManager browserManager,
        IPageHelper pageHelper,
        IScopeContext scopeContext,
        bool validatePageReady = true,
        IAutomationLogger? logger = null)
        : PartnerPortalBase(browserManager, pageHelper, scopeContext, validatePageReady, logger)
    {
        protected override string PageIdentifier => "home";

        protected override string PageName => "PartnerPortal Home Page";

        public async Task ClickCopyLinkAsync()
        {
            var LinkCopiedMessageLocator = Page.Locator("//app-snackbar-message//span[contains(text(),'Referral link copied')]");
            await _pageHelper.InteractWithField(PartnerPortal_FieldNames.CopyLink);
            await _pageHelper.WaitForElementAsync(LinkCopiedMessageLocator, timeout: 5000, waitForVisibility: true);
        }
    }
}
