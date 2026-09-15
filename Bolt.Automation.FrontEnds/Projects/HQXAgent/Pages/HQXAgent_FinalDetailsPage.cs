using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.FormData;
using Bolt.Automation.FrontEnds.FormData.Helpers;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.HQXAgent.Base;
using Bolt.Automation.FrontEnds.Projects.HQXAgent.FormData.FieldsRegistry;

namespace Bolt.Automation.FrontEnds.Projects.HQXAgent.Pages
{
    public class HQXAgent_FinalDetailsPage(IBrowserManager browserManager,
        IPageHelper pageHelper,
        IScopeContext scopeContext,
        bool validatePageReady = true,
        IAutomationLogger? logger = null)
        : HQXAgentBase(browserManager, pageHelper, scopeContext, validatePageReady, logger)
    {
        protected override string PageIdentifier => "FinalDetails";
        protected override string PageName => "PGR Final Details Page";

        protected override Dictionary<string, UIElement> FieldRegistry => FinalDetailsFields.Fields;

        public override async Task FillForm(Dictionary<string, string> formData)
        {
            var (pageSpecificData, fields) = FormDataHelper.GetFieldsForPage(this, ScopeContext, formData);
            await Page.FillRelevantFields(pageSpecificData, fields, ScopeContext);
        }
    }
}
