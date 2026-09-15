using Bolt.Automation.ApiClients.AdbxApi;
using Bolt.Automation.ApiClients.AdbxApi.Entities.Subtenant;
using Bolt.Automation.ApiClients.Infrastructure;
using Bolt.Automation.Common;
using Bolt.Automation.Common.Enums;
using Bolt.Automation.Common.Logging.Extentions;
using Bolt.Automation.Common.Models.Users;
using Bolt.Automation.ExternalServices.Outlook;
using Bolt.Automation.FrontEnds.Projects.ADBX.Pages;
using Bolt.Automation.FrontEnds.Projects.STS;
using Bolt.Automation.Tests.TestExtension.Attributes;
using Bolt.Automation.Tests.TestExtension.Base;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using AuthorAttribute = Bolt.Automation.Tests.TestExtension.Attributes.AuthorAttribute;

namespace Bolt.Automation.Tests.Tests.AdbxTests
{
    public class MfaTests : AdbxUITestBase
    {
        private IOutlookClient _outlookClient = null!;
        private IAdbxApiClientFactory _adbxApiFactory = null!;

        protected override void ResolveServices()
        {
            _outlookClient = _testScope.ServiceProvider.GetRequiredService<IOutlookClient>();
            _adbxApiFactory = _testScope.ServiceProvider.GetRequiredService<IAdbxApiClientFactory>();
        }

        [Test]
        [Tenant(Tenant.BOLTACCESS)]
        [Category("ADBX")]
        [Category("MFA")]
        [TestCaseId(225476)]
        [Author(Author.Andrii)]
        [Description("login as user with verified email, retrieve verification code and check that ADBX is displayed")]
        public async Task BOLTACCESS_Org_Mfa_Enabled_Requires_Verification_Code_Test()
        {
            var principalUserMfaEn = TestContextAccessor.CurrentUserCollection.MfaPrincipal!;
            
            var rootAdmin = TestContextAccessor.CurrentUserCollection.RootAdmin!;

            // Force MFA to the opposite state via API first, so the UI toggle click below
            // exercises the actual enable action instead of a no-op.
            await _logger.ExecuteStepAsync("Preconditions: disable MFA for the MFA Test org via API", async () =>
            {
                ScopeContext.Set(ctx => ctx.CurrentUser, rootAdmin);
                var adbxApi = await _adbxApiFactory.CreateApiClientAsync();
                await adbxApi.UpdateSubtenantMfaSettings(
                    principalUserMfaEn.SubtenantId,
                    new UpdateSubtenantMfaSettingsModel { Email = false })
                    .EnsureSuccessContentAsync(allowNullContent: true);
            }, "Expected result: Email MFA is disabled for the org");

            var homePage = await AdbxHelper.LoginToHomeAsync(rootAdmin, rootAdmin.LoginUrl!);

            var organizationsPage = await _logger.ExecuteStepAsync("Navigate to Organization Management", async () =>
                await AdbxHelper.NavigateToAdminMenuAsync<ADBX_OrganizationsPage>(homePage, "Organization Management"),
                "Expected result: Organization Management page is displayed");

            await OpenOrgAndSetEmailMfaAsync(
                organizationsPage,
                principalUserMfaEn,
                managementItem: "Edit Organization",
                enableMfa: true);

            await AdbxHelper.LoginAsync(principalUserMfaEn, principalUserMfaEn.LoginUrl!);
            var mfaStartedAt = DateTimeOffset.UtcNow;
            var code = await _logger.ExecuteStepAsync("Retrieve verification code from email", async () =>
            {
                var email = await _outlookClient.GetSpecificEmail(
                    "BOLTACCESS Verification",
                    principalUserMfaEn.Email,
                    "Your Verification Code for bolt Platform",
                    mfaStartedAt);
                var code = _outlookClient.ExtractVerificationCode(email.Body.Content);

                Assert.That(code, Is.Not.Null.And.Not.Empty, "Verification code could not be extracted from the email body");

                return code;
            }, "Expected result: 6-digit verification code is retrieved from the BOLTACCESS Verification email");

            await _logger.ExecuteStepAsync("Enter verification code and confirm ADBX organization management page is displayed", async () =>
            {
                var mfaPage = PageFactory.CreatePage<STS_MfaPage>();
                await mfaPage.EnterVerificationCode(code!);
                var orgManagementPage = PageFactory.CreatePage<ADBX_OrganizationManagementPage>();

                Assert.That(orgManagementPage.Page.Url, Does.Contain("/organization-management"),
                    "Code should be accepted and the organization management page should be displayed");
            }, "Expected result: Code is accepted and the organization management page is displayed");
        }

        [Test]
        [Tenant(Tenant.BOLTACCESS)]
        [Category("ADBX")]
        [Category("MFA")]
        [Author(Author.Andrii)]
        [TestCaseId(225477)]
        [Description("Log in to an organization with MFA disabled and confirm the ADBX app is displayed without an MFA challenge")]
        public async Task BOLTACCESS_Org_Mfa_Disabled_Logs_In_Without_Verification_Test()
        {
            var principalUserMfaDis = TestContextAccessor.CurrentUserCollection.NoMfaPrincipal!;
            var rootAdmin = TestContextAccessor.CurrentUserCollection.RootAdmin!;

            // Force MFA to the opposite state via API first, so the UI toggle click below
            // exercises the actual disable action instead of a no-op.
            await _logger.ExecuteStepAsync("Preconditions: Enable email MFA for the mfalegalname org via API", async () =>
            {
                ScopeContext.Set(ctx => ctx.CurrentUser, rootAdmin);
                var adbxApi = await _adbxApiFactory.CreateApiClientAsync();
                await adbxApi.UpdateSubtenantMfaSettings(
                    principalUserMfaDis.SubtenantId,
                    new UpdateSubtenantMfaSettingsModel { Email = true })
                    .EnsureSuccessContentAsync(allowNullContent: true);
            }, "Expected result: Email MFA is enabled for the mfalegalname org");

            var homePage = await AdbxHelper.LoginToHomeAsync(rootAdmin, rootAdmin.LoginUrl!);

            var organizationsPage = await _logger.ExecuteStepAsync("Navigate to Organization Management", async () =>
                await AdbxHelper.NavigateToAdminMenuAsync<ADBX_OrganizationsPage>(homePage, "Organization Management"),
                "Expected result: Organization Management page is displayed");

            await OpenOrgAndSetEmailMfaAsync(
                organizationsPage,
                principalUserMfaDis,
                managementItem: "Subscription Information",
                enableMfa: false,
                dismissAppointmentsPopup: false);

            await AdbxHelper.LoginAsync(principalUserMfaDis, principalUserMfaDis.LoginUrl!);

            await _logger.ExecuteStepAsync("Confirm ADBX organization management page is displayed", async () =>
            {
                var orgManagementPage = PageFactory.CreatePage<ADBX_OrganizationManagementPage>();

                Assert.That(orgManagementPage.Page.Url, Does.Contain("/organization-management"),
                    "The ADBX organization management page should be displayed with no MFA challenge");
            }, "Expected result: The ADBX organization management page is displayed");
        }

        [Test]
        [Tenant(Tenant.BOLTACCESS)]
        [Category("ADBX")]
        [Category("MFA")]
        [TestCaseId(253096)]
        [Author(Author.Andrii)]
        [Description("Log in as an agent belonging to a group with MFA status Email and confirm the verification code screen must be completed before ADBX is displayed")]
        public async Task BOLTACCESS_Group_Mfa_Enabled_Requires_Verification_Code_Test()
        {
            var groupMfaAgent = TestContextAccessor.CurrentUserCollection.GroupMfaAgent!;
            var mfaStartedAt = DateTimeOffset.UtcNow;

            await AdbxHelper.LoginAsync(groupMfaAgent, groupMfaAgent.LoginUrl!);

            var code = await _logger.ExecuteStepAsync("Retrieve verification code from email", async () =>
            {
                var email = await _outlookClient.GetSpecificEmail(
                    "BOLTACCESS Verification",
                    groupMfaAgent.Email,
                    "Your Verification Code for bolt Platform",
                    mfaStartedAt);
                var code = _outlookClient.ExtractVerificationCode(email.Body.Content);

                Assert.That(code, Is.Not.Null.And.Not.Empty, "Verification code could not be extracted from the email body");

                return code;
            }, "Expected result: 6-digit verification code is retrieved from the BOLTACCESS Verification email");

            await _logger.ExecuteStepAsync("Enter verification code and confirm the ADBX home page is displayed", async () =>
            {
                var mfaPage = PageFactory.CreatePage<STS_MfaPage>();
                await mfaPage.EnterVerificationCode(code!);
                var homePage = PageFactory.CreatePage<ADBX_HomePage>();

                Assert.That(homePage.Page.Url, Does.Contain("/home"),
                    "Code should be accepted and the ADBX home page should be displayed");
            }, "Expected result: Code is accepted and the ADBX home page is displayed");
        }

        [Test]
        [Tenant(Tenant.BOLTACCESS)]
        [Category("ADBX")]
        [Category("MFA")]
        [TestCaseId(253097)]
        [Author(Author.Andrii)]
        [Description("Log in as an agent belonging to a group with MFA status Not Set and confirm ADBX is displayed with no MFA challenge")]
        public async Task BOLTACCESS_Group_Mfa_Disabled_Logs_In_Without_Verification_Test()
        {
            var groupNoMfaAgent = TestContextAccessor.CurrentUserCollection.GroupNoMfaAgent!;

            var homePage = await AdbxHelper.LoginToHomeAsync(groupNoMfaAgent, groupNoMfaAgent.LoginUrl!);

            await _logger.ExecuteStepAsync("Confirm the ADBX home page is displayed", async () =>
            {
                Assert.That(homePage!.Page.Url, Does.Contain("/home"),
                    "The ADBX home page should be displayed with no MFA challenge");
            }, "Expected result: The ADBX home page is displayed");
        }

        private async Task OpenOrgAndSetEmailMfaAsync(
            ADBX_OrganizationsPage organizationsPage,
            UserTestData targetOrg,
            string managementItem,
            bool enableMfa,
            bool dismissAppointmentsPopup = true)
        {
            await _logger.ExecuteStepAsync($"Search for and open the {targetOrg.Subtenant} organization", async () =>
            {
                await organizationsPage.SearchRecentRecords(targetOrg.Subtenant);
                await _pageHelper.ClickTableCellButtonAsync("id", "", "id");
            }, "Expected result: Organization Management page is displayed for the organization");

            await _logger.ExecuteStepAsync($"{(enableMfa ? "Enable" : "Disable")} Email MFA via {managementItem}", async () =>
            {
                var organizationManagementPage = PageFactory.CreatePage<ADBX_OrganizationManagementPage>();
                await organizationManagementPage.ClickOnManagementItem(managementItem);
                if (dismissAppointmentsPopup)
                {
                    await organizationManagementPage.DismissAppointmentsPopupIfPresent();
                }
                await organizationManagementPage.ExpandSection("Multi-Factor Authentication");
                await organizationManagementPage.SetEmailMfaToggle(enableMfa);
                await organizationManagementPage.ClickConfirmForSection("mfaSection");
            }, $"Expected result: Email MFA is {(enableMfa ? "enabled" : "disabled")} for the org");
        }
    }
}
