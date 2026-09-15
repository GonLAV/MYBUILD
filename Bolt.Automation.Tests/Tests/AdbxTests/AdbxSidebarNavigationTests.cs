using Bolt.Automation.Common;
using Bolt.Automation.Common.Enums;
using Bolt.Automation.Common.Logging.Extentions;
using Bolt.Automation.FrontEnds.Projects.ADBX.Pages;
using Bolt.Automation.FrontEnds.Projects.ADBX.Pages.Cases;
using Bolt.Automation.FrontEnds.Projects.ADBX.Pages.Communications;
using Bolt.Automation.FrontEnds.Projects.ADBX.Pages.Leads;
using Bolt.Automation.FrontEnds.Projects.ADBX.Pages.Policies;
using Bolt.Automation.FrontEnds.Projects.ADBX.Pages.Quotes;
using Bolt.Automation.FrontEnds.Projects.ADBX.Popups;
using Bolt.Automation.FrontEnds.Projects.STS;
using Bolt.Automation.TestDataProvider.Context;
using Bolt.Automation.Tests.TestExtension.Attributes;
using Bolt.Automation.Tests.TestExtension.Base;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using static Bolt.Automation.FrontEnds.Projects.ADBX.FormData.ADBX_FieldNames;
using AuthorAttribute = Bolt.Automation.Tests.TestExtension.Attributes.AuthorAttribute;

namespace Bolt.Automation.Tests.Tests.AdbxTests
{
    public class AdbxSidebarNavigationTests : AdbxUITestBase
    {

        [Test]
        [Tenant(Tenant.BOLTAG)]
        [Category("ADBX")]
        [Category("CRM")]
        [Author(Author.Andrii)]
        [TestCaseId(225991)]
        [Description("Check agent sidebar navigation")]
        public async Task BOLTAG_Agent_Sidebar_Navigation_Test()
        {
            await AdbxHelper.LoginAsync(
                TestContextAccessor.CurrentUserCollection.ServiceAgent,
                ScopeContext.Data.UrlDataCollection.FrontEnd.LoginUrl);

            var homePage = PageFactory.CreatePage<ADBX_HomePage>();
            await _logger.ExecuteStepAsync("Navigate Through ADBX Agent Sidebar Menu", async () =>
            {
                await _pageHelper.InteractWithField(SideMenuToggle);

                homePage = await AdbxHelper.NavigateToMenuAsync<ADBX_HomePage>(homePage, NavigationType.Home);

                var accountsPage = await AdbxHelper.NavigateToMenuAsync<ADBX_AccountsTabPage>(homePage, NavigationType.Accounts);

                var leadsPage = await AdbxHelper.NavigateToMenuAsync<ADBX_LeadsTabPage>(homePage, NavigationType.Leads);

                var quotesPage = await AdbxHelper.NavigateToMenuAsync<ADBX_QuotesTabPage>(homePage, NavigationType.Quotes);

                var casesPage = await AdbxHelper.NavigateToMenuAsync<ADBX_CasesTabPage>(homePage, NavigationType.Service);

                var policiesPage = await AdbxHelper.NavigateToMenuAsync<ADBX_PoliciesTabPage>(homePage, NavigationType.Policies);

                var renewalsPage = await AdbxHelper.NavigateToMenuAsync<ADBX_RenewalsTabPage>(homePage, NavigationType.Renewals);

                var marketFinderPage = await AdbxHelper.NavigateToMenuAsync<ADBX_MarketFinderPage>(homePage, NavigationType.MarketFinder);

                var notificationsPage = await AdbxHelper.NavigateToMenuAsync<ADBX_NotificationsPage>(homePage, NavigationType.Notifications);
            }, "Agent can navigate through every sidebar menu item");
        }
        [Test]
        [Tenant(Tenant.BOLTAG)]
        [Category("ADBX")]
        [Category("CRM")]
        [Author(Author.Andrii)]
        [TestCaseId(226009)]
        [Description("Check agent sidebar logout")]
        public async Task BOLTAG_Agent_Sidebar_Logout_Test()
        {
            await AdbxHelper.LoginAsync(
            TestContextAccessor.CurrentUserCollection.ServiceAgent,
            ScopeContext.Data.UrlDataCollection.FrontEnd.LoginUrl);

            await _logger.ExecuteStepAsync("Execute Agent Sidebar Logout Flow", async () =>
            {
                var homePage = PageFactory.CreatePage<ADBX_HomePage>();
                await _pageHelper.InteractWithField(SideMenuToggle);
                await homePage.ClickOnMenuTab(NavigationType.LogOut);
                await _pageHelper.InteractWithField(LogoutButton);
                Assert.That(await _pageHelper.WaitForNavigationOrUrlContainsAsync("logout"), Is.True,
                               $"LogOut redirect returned wrong page. Expcected : logout, Actual ;{_currentPage.Url}");

            }, "Agent is redirected to the logout page");
        }
        [Test]
        [Tenant(Tenant.BOLTAG)]
        [Category("ADBX")]
        [Category("CRM")]
        [Author(Author.Andrii)]
        [TestCaseId(226008)]
        [Description("Check service manager sidebar user update")]
        public async Task BOLTAG_ServiceManager_Sidebar_User_Update_Test()
        {
            await AdbxHelper.LoginAsync(
                TestContextAccessor.CurrentUserCollection.ServiceManager,
                ScopeContext.Data.UrlDataCollection.FrontEnd.LoginUrl);

            var defaultFirstName = string.Empty;
            var defaultLastName = string.Empty;
            var defaultTimeZone = string.Empty;

            var userPopup = await _logger.ExecuteStepAsync("Open Sidebar User Self-Edit popup", async () =>
            {
                var homePage = PageFactory.CreatePage<ADBX_HomePage>();
                await _pageHelper.InteractWithField(SideMenuToggle);
                await _pageHelper.InteractWithField(SideMenuUserSelfEdit);
                var userPopup = PageFactory.CreatePage<ADBX_UserSelfEditPopUp>();

                return userPopup;
            }, "Sidebar User Self-Edit popup is opened");

            await _logger.ExecuteStepAsync("Update user first name, last name and timezone", async () =>
            {
               await userPopup.FillForm();
               defaultFirstName = await _pageHelper!.GetFieldValue(UserSelfEditFirstName);
               defaultLastName = await _pageHelper!.GetFieldValue(UserSelfEditLastName);
               defaultTimeZone = await _pageHelper!.GetFieldValue(UserSelfEditTimeZone);
               await userPopup.ClickPopupConfirm();
            }, "First name, last name and timezone are updated");

            await _logger.ExecuteStepAsync("Open user Self Edit popup and check updates", async () =>
            {
                await _pageHelper.InteractWithField(SideMenuUserSelfEdit);
                var actualFirstName = await _pageHelper!.GetFieldValue(UserSelfEditFirstName);
                var actualLastName = await _pageHelper!.GetFieldValue(UserSelfEditLastName);
                var actualTimeZone = await _pageHelper!.GetFieldValue(UserSelfEditTimeZone);

                Assert.Multiple(() =>
                {
                    Assert.That(actualFirstName, Is.EqualTo(defaultFirstName),
                        $"FirstName was not updated. Expected: {defaultFirstName}, Actual: {actualFirstName}");
                    Assert.That(actualLastName, Is.EqualTo(defaultLastName),
                        $"LastName was not updated. Expected: {defaultLastName}, Actual: {actualLastName}");
                    Assert.That(actualTimeZone, Is.EqualTo(defaultTimeZone),
                        $"TimeZone was not updated. Expected: {defaultTimeZone}, Actual: {actualTimeZone}");
                });
            }, "First name, last name and timezone are updated correctly");

        }
        [Test]
        [Tenant(Tenant.BOLTAG)]
        [Category("ADBX")]
        [Category("CRM")]
        [Author(Author.Andrii)]
        [TestCaseId(226011)]
        [Description("Check service manager sidebar navigation")]
        public async Task BOLTAG_Service_Manager_Sidebar_Navigation_Test()
        {
            await AdbxHelper.LoginAsync(
               TestContextAccessor.CurrentUserCollection.ServiceManager,
               ScopeContext.Data.UrlDataCollection.FrontEnd.LoginUrl);

            await _logger.ExecuteStepAsync("Navigate Through ADBX Service Manager Admin Sidebar Menu", async () =>
            {
                var homePage = PageFactory.CreatePage<ADBX_HomePage>();
                await _pageHelper.InteractWithField(SideMenuToggle);
                var usersPage = await AdbxHelper.NavigateToAdminMenuAsync<ADBX_UsersTabPage>(homePage, "User Management");
                var usersTableDisplayed = await _pageHelper!.IsTableDisplayed();

                _logger.LogBusinessRule("UsersTableDisplayed",
                    usersTableDisplayed,
                    "Users table should be displayed after navigating to User Management");
                Assert.That(usersTableDisplayed, Is.True);

                var groupsPage = await AdbxHelper.NavigateToAdminMenuAsync<ADBX_GroupsTabPage>(homePage, "Group Management");
                var groupsTableDisplayed = await _pageHelper!.IsTableDisplayed();

                Assert.That(groupsTableDisplayed, Is.True,
                    "Groups table should be displayed after navigating to Group Management");

                var emailTemplatesPage = await AdbxHelper.NavigateToAdminMenuAsync<ADBX_CommunicationsEmailTemplatePage>(homePage, "Communications Templates");
                var emailTemplatesTableDisplayed = await _pageHelper!.IsTableDisplayed();

                Assert.That(emailTemplatesTableDisplayed, Is.True,
                    "Communications Templates table should be displayed after navigating to Communications Templates");

            }, "Service manager can navigate through admin sidebar menus and see the expected tables");
        }
    }
}
