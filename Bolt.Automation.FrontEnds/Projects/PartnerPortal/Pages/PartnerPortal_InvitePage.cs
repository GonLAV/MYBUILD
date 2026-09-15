using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.PartnerPortal.Base;
using Bolt.Automation.FrontEnds.Projects.PartnerPortal.FormData;

namespace Bolt.Automation.FrontEnds.Projects.PartnerPortal.Pages
{
    public class PartnerPortal_InvitePage(IBrowserManager browserManager,
        IPageHelper pageHelper,
        IScopeContext scopeContext,
        bool validatePageReady = true,
        IAutomationLogger? logger = null)
        : PartnerPortalBase(browserManager, pageHelper, scopeContext, validatePageReady, logger)
    {

        protected override string PageIdentifier => "invite";
        protected override string PageName => "Partner Portal Invite Page";
        private const string SendInviteEndpoint = "invite/sendinvite";

        public override async Task FillForm(Dictionary<string, string>? formData = null)
        {
            await base.FillForm(formData);
            await PageHelper.WaitForApiResponseAsync(
                ClickContinue,
                SendInviteEndpoint,
                200,
                30000);
        }

        public async Task<PartnerPortal_HomePage> ReturnToHomePage()
        {
            await PageHelper.InteractWithField(PartnerPortal_FieldNames.HomePageButton);
            return new PartnerPortal_HomePage(BrowserManager, PageHelper, scopeContext, true, _logger);
        }
    }
}
