using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.FormData.Common;
using Bolt.Automation.FrontEnds.FormData.Helpers;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.HQXAgent.Base;

namespace Bolt.Automation.FrontEnds.Projects.HQXAgent.Pages
{
    public class HQXAgent_YourAddressPage(IBrowserManager browserManager,
        IPageHelper pageHelper,
        IScopeContext scopeContext,
        bool validatePageReady = true,
        IAutomationLogger? logger = null)
        : HQXAgentBase(browserManager, pageHelper, scopeContext, validatePageReady, logger)
    {

        #region Page Properties
        protected override string PageIdentifier => "address";
        protected override string PageName => "PGR Address Page";
        #endregion

        public override async Task FillForm(Dictionary<string, string> formData)
        {
            var (pageSpecificData, fields) = FormDataHelper.GetFieldsForPage(this, ScopeContext, formData);
            await Page.FillRelevantFields(pageSpecificData, fields, ScopeContext);
        }

        public async Task SetAddress(string address)
        {
            await PageHelper.InteractWithField(FieldNames.OnlineAddress, address);
        }

    }
}