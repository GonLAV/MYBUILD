using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Enums;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.ADBX.Base;
using Bolt.Automation.FrontEnds.Projects.ADBX.Popups;

namespace Bolt.Automation.FrontEnds.Projects.ADBX.Pages
{
    public class ADBX_HomePage(IBrowserManager browserManager, IPageHelper pageHelper, IScopeContext scopeContext, bool validatePageReady = true, IAutomationLogger? logger = null)
        : ADBX_BasePage(browserManager, pageHelper, scopeContext, validatePageReady, logger)
    {
        protected override string PageIdentifier => "home";
        protected override string PageName => "ADBX Home Page";

        // The home page is reached via the STS login callback: the browser lands on the
        // /?token=… URL and the Angular app then client-side redirects to /home. Measured in QA
        // that redirect is ~3s idle and ~9–17s under load. The base 30s validation budget splits
        // into ~10s per URL attempt (30000/3), which routinely loses the race with this redirect
        // and throws PageCreationException: ADBX_HomePage. Give this page — and only this page — a
        // longer budget so its own readiness check waits out the redirect, rather than pushing the
        // wait onto every caller.
        private const int HomeRedirectTimeoutMs = 60000;

        public override Task ValidatePageReady()
            => PageHelper.ValidatePageReadyAsync(PageIdentifier, PageName, HomeRedirectTimeoutMs);
    }
}
