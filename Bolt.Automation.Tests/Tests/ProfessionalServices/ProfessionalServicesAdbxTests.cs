using Bolt.Automation.Common.Configuration.InjectedConfig;
using Bolt.Automation.Common.Enums;
using Bolt.Automation.Common.Logging.Extentions;
using Bolt.Automation.FrontEnds.Projects.ADBX.Pages;
using Bolt.Automation.FrontEnds.Projects.STS;
using Bolt.Automation.Tests.TestExtension.Attributes;
using Bolt.Automation.Tests.TestExtension.Base;
using Bolt.Automation.Common;
using NUnit.Framework;
using AuthorAttribute = Bolt.Automation.Tests.TestExtension.Attributes.AuthorAttribute;
using static Bolt.Automation.FrontEnds.Projects.ADBX.FormData.ADBX_FieldNames;

namespace Bolt.Automation.Tests.Tests.ProfessionalServices
{
    /// <summary>
    /// Smallest end-to-end Professional Services onboarding test — proves the
    /// <see cref="UIInjectionTestBase"/> foundation works against a newly onboarded tenant
    /// without depending on the static <c>(Tenant, Environment)</c>-keyed data stores.
    ///
    /// Ports the spirit of <c>BOLTAG_Agent_Sidebar_Logout_Test</c> from <c>AdbxSidebarNavigationTests</c>
    /// (smallest existing ADBX test that does login + a single UI action + assert + logging),
    /// but reads URL/credentials from <c>INJECTED_*</c> environment variables instead of
    /// <c>TestContextAccessor</c>.
    /// </summary>
    public class ProfessionalServicesAdbxTests : UIInjectionTestBase
    {
        // Only the Adbx section is required — PartnerPortal/D2C env vars are not consumed
        // by this test, so the validator skips them.
        private static readonly HashSet<string> _requiredSections = new() { nameof(InjectedTestConfig.Adbx) };
        protected override IReadOnlySet<string> RequiredSections => _requiredSections;

        public ProfessionalServicesAdbxTests() : base()
        {
            // Build a UserTestData from injected credentials and surface it on the scope so
            // STS_LoginPage.Login() (which reads ScopeContext.CurrentUser) can use it without
            // needing a UserDataStore lookup.
            //var user = new UserTestData
            //{
            //    Username = _injectedConfig.Adbx.Username,
            //    Password = _injectedConfig.Adbx.Password,
            //};
            //ScopeContext.Set(ctx => ctx.CurrentUser, user);
            ScopeContext.Set(ctx => ctx.CurrentUser, _injectedConfig.BuildAdbxUser());
            ScopeContext.Set(ctx => ctx.FrontEnd, FrontEndType.ADBX);
        }



        [Test]
        [Category("ProfessionalServices")]
        [TestCaseId(900001)]
        [Author(Author.Sandy)]
        [Description("Onboarding smoke: agent can log into ADBX and log out via the sidebar")]
        public async Task ProfessionalServices_Adbx_AgentLogout_SmokeTest()
        {
            var url = _injectedConfig.Adbx.LoginUrl;
            _logger.Info($"Onboarding sanity for tenant '{_injectedConfig.Tenant}' against env '{_injectedConfig.Environment}'");
            _logger.Info($"Target ADBX login URL: {url}");
            
            await _logger.ExecuteStepAsync("Navigate to ADBX login URL", async () =>
            {
                await BrowserManager.NavigateAsync(url);
            });
            ScopeContext.Set(ctx => ctx.CurrentUrl, url);
            await _logger.ExecuteStepAsync("STS login", async () =>
            {
                var loginPage = PageFactory.CreatePage<STS_LoginPage>();
                await loginPage.Login();
            });

            await _logger.ExecuteStepAsync("Sidebar logout", async () =>
            {
                var homePage = PageFactory.CreatePage<ADBX_HomePage>();
                await _pageHelper!.InteractWithField(SideMenuToggle);
                await homePage.ClickOnMenuTab(NavigationType.LogOut);
                await _pageHelper.InteractWithField(LogoutButton);
            });

            Assert.That(
                await _pageHelper!.WaitForNavigationOrUrlContainsAsync("logout"),
                Is.True,
                "LogOut redirect did not land on a URL containing 'logout'");
        }
    }
}
