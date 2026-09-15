using Bolt.Automation.Common;
using Bolt.Automation.Common.Enums;
using Bolt.Automation.Common.Logging.Extentions;
using Bolt.Automation.FrontEnds.Projects.ADBX.Base;
using Bolt.Automation.FrontEnds.Projects.ADBX.Pages;
using Bolt.Automation.FrontEnds.Projects.ADBX.Pages.Leads;
using Bolt.Automation.TestDataProvider.Context;
using Bolt.Automation.Tests.TestExtension.Attributes;
using Bolt.Automation.Tests.TestExtension.Base;
using NUnit.Framework;
using AuthorAttribute = Bolt.Automation.Tests.TestExtension.Attributes.AuthorAttribute;

namespace Bolt.Automation.Tests.Tests.AdbxTests
{
    public class PermissionTests : AdbxUITestBase
    {

        /// <summary>
        /// Navigates to the given path off the ADBX front-end base URL for the current tenant/environment,
        /// then creates <typeparamref name="T"/> for the caller to assert against — construction blocks
        /// until the page's own <c>PageIdentifier</c> shows up in the URL, absorbing client-side redirects.
        /// </summary>
        private async Task<T> NavigateAndCreatePageAsync<T>(string relativePath) where T : ADBX_BasePage
        {
            var baseUrl = ScopeContext.Data.UrlDataCollection.FrontEnd.BaseUrl;
            await BrowserManager.NavigateAsync($"{baseUrl}{relativePath}");

            return PageFactory.CreatePage<T>();
        }

        [Test]
        [Ignore("adbx-enable-permission-guard flag is temporary off + 253248")]
        [Tenant(Tenant.BOLTAG)]
        [Category("ADBX")]
        [Category("CRM")]
        [Category("Permissions")]
        [Author(Author.Andrii)]
        [TestCaseId(250798)]
        [Description("Verify Admin without permission is redirected to Access Denied for restricted pages")]
        public async Task BOLTAG_Admin_No_Permission_Redirects_To_AccessDenied_Test()
        {
            await AdbxHelper.LoginAsync(
                TestContextAccessor.CurrentUserCollection.Admin,
                ScopeContext.Data.UrlDataCollection.FrontEnd.LoginUrl);

            await _logger.ExecuteStepAsync("Execute BOLTAG Admin No Permission Redirects To Access Denied Flow (/accounts, /custom-interview-fields)", async () =>
            {
                var homePage = _pageFactory.CreatePage<ADBX_HomePage>();

                var accountsAccessDeniedPage = await NavigateAndCreatePageAsync<ADBX_AccessDeniedPage>("/accounts");
                Assert.That(accountsAccessDeniedPage.Page.Url, Does.Contain("access-denied"),
                    "Admin without permission should be redirected to Access Denied for the accounts page");

                var customFieldsAccessDeniedPage = await NavigateAndCreatePageAsync<ADBX_AccessDeniedPage>("/custom-interview-fields");
                Assert.That(customFieldsAccessDeniedPage.Page.Url, Does.Contain("access-denied"),
                    "Admin without permission should be redirected to Access Denied for the custom interview fields page");
            }, "Admin without permission is redirected to Access Denied for restricted pages");
        }

        [Test]
        [Tenant(Tenant.BOLTAG)]
        [Category("ADBX")]
        [Category("CRM")]
        [Category("Permissions")]
        [Author(Author.Andrii)]
        [TestCaseId(250812)]
        [Description("Verify Admin with permission can access Leads and Search pages without redirect")]
        public async Task BOLTAG_Admin_Has_Permission_Access_Leads_And_Search_Test()
        {
            await AdbxHelper.LoginAsync(
                TestContextAccessor.CurrentUserCollection.Admin,
                ScopeContext.Data.UrlDataCollection.FrontEnd.LoginUrl);

            await _logger.ExecuteStepAsync("Execute BOLTAG Admin Has Permission Access Leads And Search Flow", async () =>
            {
                var queueId = TestContextAccessor.GetTestSpecificValue("OpenLeadsQueueIdBoltag");

                var homePage = _pageFactory.CreatePage<ADBX_HomePage>();

                var leadsPage = await NavigateAndCreatePageAsync<ADBX_LeadsTabPage>($"/new-leads?activeTab={queueId}");
                Assert.That(leadsPage.Page.Url, Does.Contain($"/new-leads?activeTab={queueId}"),
                    "Admin with permission should access the Leads page without redirect");

                var searchPage = await NavigateAndCreatePageAsync<ADBX_SearchResultsPage>("/search");
                Assert.That(searchPage.Page.Url, Does.Contain("search"),
                    "Admin with permission should access the Search page without redirect");
            }, "Admin with permission can access Leads and Search pages without redirect");
        }
    }
}
