using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.D2C.Base;

namespace Bolt.Automation.FrontEnds.Projects.D2C.Pages
{
    public class D2C_ProgressiveAdditionalQuestionsFQ : D2CBase
    {
        #region Page Properties
        protected override string PageIdentifier => "progressive-additional-questions";
        protected override string PageName => "progressive additional questions Full Quote Page";
        #endregion
        public D2C_ProgressiveAdditionalQuestionsFQ(IBrowserManager browserManager, IPageHelper pageHelper, IScopeContext scopeContext, bool validatePageReady = true, IAutomationLogger? logger = null)
        : base(browserManager, pageHelper, scopeContext, validatePageReady, logger) { }

        public override async Task FillForm(Dictionary<string, string>? formData = null)
        {
            for (int step = 0; step < 3; step++)
            {
                await base.FillForm(formData);

                if (step != 2)
                {
                    await ClickContinue();
                }
            }
        }
    }
}
