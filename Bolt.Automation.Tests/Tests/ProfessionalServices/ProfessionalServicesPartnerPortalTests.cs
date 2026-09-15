using System.Globalization;
using Bolt.Automation.Common.Configuration.InjectedConfig;
using Bolt.Automation.Common.Enums;
using Bolt.Automation.Common.Logging.Extentions;
using Bolt.Automation.ExternalServices.Outlook;
using Bolt.Automation.FrontEnds.Projects.PartnerPortal.FormData;
using Bolt.Automation.FrontEnds.Projects.PartnerPortal.Pages;
using Bolt.Automation.FrontEnds.Projects.PartnerPortal.Popups;
using Bolt.Automation.Tests.TestExtension.Attributes;
using Bolt.Automation.Tests.TestExtension.Base;
using Bolt.Automation.Tests.TestHelpers.ADBX;
using Microsoft.Extensions.DependencyInjection;
using Bolt.Automation.Common;
using NUnit.Framework;
using AuthorAttribute = Bolt.Automation.Tests.TestExtension.Attributes.AuthorAttribute;

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
    public class ProfessionalServicesPartnerPortalTests : UIInjectionTestBase
    {
        // Only the PartnerPortal section is required — Adbx/D2C env vars are not consumed
        // by this test, so the validator skips them.
        private static readonly HashSet<string> _requiredSections = new() { nameof(InjectedTestConfig.PartnerPortal) };
        protected override IReadOnlySet<string> RequiredSections => _requiredSections;
        public IOutlookClient _outlookClient = null!;
        private AdbxTestHelper _adbxHelper = null!;
        protected override void ResolveServices()
        {
            _outlookClient = _testScope.ServiceProvider.GetRequiredService<IOutlookClient>();
        }
        protected override void InitializeComponents()
        {
            _adbxHelper = new AdbxTestHelper(_logger, BrowserManager, PageFactory, _pageHelper!, ScopeContext);
        }

        public ProfessionalServicesPartnerPortalTests() : base()
        {
            ScopeContext.Set(ctx => ctx.FrontEnd, FrontEndType.PartnerPortal);
        }

        [Test]
        [Category("ProfessionalServices")]
        [TestCaseId(139990000)]
        [Author(Author.Sandy)]
        [Description("Create invite via UI and check email receive")]
        public async Task ProfessionalServices_PartnerPortal_SendInvite_EmailReceive()
        {
            string EmailTitle = "Your quote is waiting!";
            
            var user = _injectedConfig.BuildPartnerPortalUser();
            ScopeContext.Set(ctx => ctx.CurrentUser, user);

            var firstNameValue = FieldRegistryPartnerPortal.Fields["FirstName"].DefaultValue;

            await _adbxHelper.LoginAsync(
                   user,
                   user.LoginUrl);

            var homePage = PageFactory.CreatePage<PartnerPortal_HomePage>();

            var referralsCount = await _logger.ExecuteStepAsync("Read referrals count (before)", async () =>
            {
                var referralsCountText = await _pageHelper!.GetFieldValue(PartnerPortal_FieldNames.ReferralsCount);
                int.TryParse(referralsCountText?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var count);
                _logger?.Debug($"Referrals count (before): '{referralsCountText}' => {count}");
                return count;
            });

            await _logger.ExecuteStepAsync("Create invite", async () =>
            {
                await homePage.FillForm();
                await homePage.ClickContinue();

                var invitePage = PageFactory.CreatePage<PartnerPortal_InvitePage>();
                await invitePage.FillForm();

                var popup = PageFactory.CreatePage<PartnerPortal_InviteSentPopup>();
                await popup.ClosePopup();

                await invitePage.ReturnToHomePage();
            });

            var referralsCountNew = await _logger.ExecuteStepAsync("Read referrals count (after)", async () =>
            {
                var referralsCountNewText = await _pageHelper!.GetFieldValue(PartnerPortal_FieldNames.ReferralsCount);
                int.TryParse(referralsCountNewText?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var count);
                _logger?.Debug($"Referrals count (after): '{referralsCountNewText}' => {count}");
                return count;
            });

            await _logger.ExecuteStepAsync("Verify email received", async () =>
            {
                var resp = await _outlookClient.IsEmailReceived(EmailTitle, firstNameValue);
                Assert.That(resp, Is.True, $"Expected to receive email with title: {EmailTitle} but did not receive it.");
            });

            await _logger.ExecuteStepAsync("Verify referrals count increased", async () =>
            {
                Assert.That(referralsCountNew, Is.GreaterThan(referralsCount),
                    $"Expected referrals count to increase by 1 after sending invite, but it did not. Before: {referralsCount}, After: {referralsCountNew}");
            });
        }


        //used external variables 
//        <RunSettings>
//	<RunConfiguration>
//		<EnvironmentVariables>
//			<INJECTED_TENANT>BOLTAG</INJECTED_TENANT>
//			<INJECTED_ENVIRONMENT>Qa</INJECTED_ENVIRONMENT>
//			<INJECTED_PARTNER_PORTAL_URL>https://partnerportal-qa.boltqa.com/BOLTAG/automationpp/login</INJECTED_PARTNER_PORTAL_URL>
//			<INJECTED_PARTNER_PORTAL_USERNAME>automationpp.agent @boltinc.com</INJECTED_PARTNER_PORTAL_USERNAME>
//			<INJECTED_PARTNER_PORTAL_PASSWORD></INJECTED_PARTNER_PORTAL_PASSWORD>
//			<INJECTED_PARTNER_PORTAL_SOURCE>automationpp</INJECTED_PARTNER_PORTAL_SOURCE>
//	</EnvironmentVariables>
//	</RunConfiguration>
//</RunSettings>
    }
}
