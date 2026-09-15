using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.ADBX.Popups;
using Bolt.Automation.FrontEnds.Projects.PartnerPortal.Base;
using Bolt.Automation.FrontEnds.Projects.PartnerPortal.Pages;

namespace Bolt.Automation.FrontEnds.Projects.PartnerPortal.Popups
{
    public class PartnerPortal_InviteSentPopup(IBrowserManager browserManager,
        IPageHelper pageHelper,
        IScopeContext scopeContext,
        bool validatePageReady = true,
        IAutomationLogger? logger = null)
        : PartnerPortalPopupBase(browserManager, pageHelper, scopeContext, validatePageReady, logger)
    {
        protected override string PopupIdentifier => "invite";
        protected override string PopupName => "Your invite was sent";

        private const string GoToLeadsLocator = "//a[contains(@class,'back-to-progress-page-popup')]";

        public async Task<PartnerPortal_LeadsPage> ClickOnGoToLeads()
        {
            await PageHelper.InteractWithElement(
                LocatorType.XPath,
                GoToLeadsLocator,
                ElementAction.Click);

            return new PartnerPortal_LeadsPage(BrowserManager, PageHelper, scopeContext);
        }
    }
}
