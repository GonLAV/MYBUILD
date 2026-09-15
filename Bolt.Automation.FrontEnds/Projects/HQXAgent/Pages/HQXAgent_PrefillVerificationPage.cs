using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.FormData.Helpers;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.HQXAgent.Base;

namespace Bolt.Automation.FrontEnds.Projects.HQXAgent.Pages
{
    public class HQXAgent_PrefillVerificationPage(IBrowserManager browserManager,
        IPageHelper pageHelper,
        IScopeContext scopeContext,
        bool validatePageReady = true,
        IAutomationLogger? logger = null)
        : HQXAgentBase(browserManager, pageHelper, scopeContext, validatePageReady, logger)
    {
        protected override string PageIdentifier => "PrefillVerification";
        protected override string PageName => "PGR Prefill Verification Page";

        public override async Task FillForm(Dictionary<string, string> formData)
        {
            var (pageSpecificData, fields) = FormDataHelper.GetFieldsForPage(this, ScopeContext, formData);
            await Page.FillRelevantFields(pageSpecificData, fields, ScopeContext);
        }
    }
}
