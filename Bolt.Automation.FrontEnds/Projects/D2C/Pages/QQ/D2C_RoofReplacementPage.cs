using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using D2CBase = Bolt.Automation.FrontEnds.Projects.D2C.Base.D2CBase;

namespace Bolt.Automation.FrontEnds.Projects.D2C.Pages
{
    public class D2C_RoofReplacementPage(
        IBrowserManager browserManager,
        IPageHelper pageHelper,
        IScopeContext scopeContext,
        bool validatePageReady = true,
        IAutomationLogger? logger = null)
        : D2CBase(browserManager, pageHelper, scopeContext,validatePageReady, logger)
    {
        #region Page Properties
        /// <summary>
        /// URL fragment that identifies this page. Public so a caller that has to decide which of
        /// several pages the app actually landed on can match on it without re-declaring the
        /// literal — see <c>D2CLobNavigator.ResolvePageAfterHouseDetailsAsync</c>.
        /// </summary>
        public const string UrlPart = "roof-replacement";

        protected override string PageIdentifier => UrlPart;
        protected override string PageName => "Roof Replacement Page";
        #endregion

    }
}
