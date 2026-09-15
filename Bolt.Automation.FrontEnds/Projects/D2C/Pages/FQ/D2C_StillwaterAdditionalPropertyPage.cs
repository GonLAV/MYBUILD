using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.D2C.Base;

namespace Bolt.Automation.FrontEnds.Projects.D2C.Pages
{
    public class D2C_StillwaterAdditionalPropertyPage : D2CBase
    {
        #region Page Properties
        protected override string PageIdentifier => "additional-property-information-stillwater";
        protected override string PageName => "Stillwater Additional Property Information Full Quote Page";
        #endregion

        public D2C_StillwaterAdditionalPropertyPage(IBrowserManager browserManager, IPageHelper pageHelper, IScopeContext scopeContext, bool validatePageReady = true, IAutomationLogger? logger = null)
            : base(browserManager, pageHelper, scopeContext, validatePageReady, logger) { }
    }
}