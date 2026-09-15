using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using D2CBase = Bolt.Automation.FrontEnds.Projects.D2C.Base.D2CBase;

namespace Bolt.Automation.FrontEnds.Projects.D2C.Pages
{
    public class D2C_PropertiesUsagePage(
        IBrowserManager browserManager,     
        IPageHelper pageHelper,
        IScopeContext scopeContext,
        bool validatePageReady = true,
        IAutomationLogger? logger = null)
        : D2CBase(browserManager, pageHelper, scopeContext, validatePageReady, logger)
    {
        #region Page Properties
        protected override string PageIdentifier => "property-usage";
        protected override string PageName => "Properties Usage Page";
        #endregion
    }
}
