using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.Interview.Base;
using Bolt.Automation.FrontEnds.Projects.Interview.Popups;
using static Bolt.Automation.FrontEnds.Projects.Interview.FormData.FieldNames;

namespace Bolt.Automation.FrontEnds.Projects.Interview.Pages
{
    public class Product_CLOfflinePage : InterviewBase
    {
        public Product_CLOfflinePage(
            IBrowserManager browserManager,
            IPageHelper pageHelper,
            IScopeContext scopeContext,
            bool validatePageReady = true,
            IAutomationLogger? logger = null)
            : base(browserManager, pageHelper, scopeContext, validatePageReady, logger) { }

        protected override string PageIdentifier => "CL_Offline";
        protected override string PageName => "CL Offline Page";

        public new async Task<Interview_OfflineRequestPopup> ClickOfflineRequest()
        {
            await PageHelper.InteractWithField(OfflineRequestButton);
            return new Interview_OfflineRequestPopup(BrowserManager, PageHelper, ScopeContext);
        }
    }
}
