using Bolt.Automation.ApiClients.GetQuoteApi.Interfaces;
using Bolt.Automation.ApiClients.GetQuoteApi.Models.Application;
using Bolt.Automation.ApiClients.Infrastructure;
using Bolt.Automation.Common;
using Bolt.Automation.Common.Enums;
using Bolt.Automation.Common.Logging.Extentions;
using Bolt.Automation.Common.Models.TestData.DataModels.PersonalLine;
using Bolt.Automation.Common.PollyRetry;
using Bolt.Automation.Common.Utils;
using Bolt.Automation.FrontEnds.Projects.ADBX.Pages;
using Bolt.Automation.FrontEnds.Projects.ADBX.Pages.Leads;
using Bolt.Automation.FrontEnds.Projects.Interview.Pages;
using Bolt.Automation.TestDataProvider.Context;
using Bolt.Automation.TestDataProvider.Providers.GetQuoteApiDataProvider;
using Bolt.Automation.TestDataProvider.TestData.ApplicationTestData;
using Bolt.Automation.TestDataProvider.TestData.CommonTestData;
using Bolt.Automation.Tests.TestExtension.Attributes;
using Bolt.Automation.Tests.TestExtension.Base;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using static Bolt.Automation.FrontEnds.FormData.Common.FieldNames;
using static Bolt.Automation.FrontEnds.Projects.ADBX.FormData.ADBX_FieldNames;
using AuthorAttribute = Bolt.Automation.Tests.TestExtension.Attributes.AuthorAttribute;

namespace Bolt.Automation.Tests.Tests.AdbxTests
{
    public class LeadTests : AdbxUITestBase
    {
        public IGetQuoteApi _getQuoteApi = null!;
        private IPollyRetryService _pollyRetry = null!;

        protected override void InitializeComponents()
        {
            var refitApiLocator = _testScope.ServiceProvider.GetRequiredService<RefitApiServiceLocator>();
            _getQuoteApi = refitApiLocator.GetRequiredService<IGetQuoteApi>();
            _pollyRetry = _testScope.ServiceProvider.GetRequiredService<IPollyRetryService>();
        }

        [Test]
        [Tenant(Tenant.BOLTAG)]
        [Category("ADBX")]
        [Category("CRM")]
        [Category("Lead")]
        [TestCaseId(241466)]
        [Author(Author.Andrii)]
        [Description("Verify a newly created lead shows Score = N/A in the Leads Open queue")]
        public async Task BOLTAG_Lead_Score_NA()
        {
            var user = TestContextAccessor.CurrentUserCollection.ServiceAgent;
            var userFullName = $"{user.FirstName} {user.LastName}";
            ScopeContext.Set(ctx => ctx.CurrentUser, user);
            var email = RandomManager.GetRandomEmail();

            await AdbxHelper.LoginAsync(
                TestContextAccessor.CurrentUserCollection.ServiceAgent,
                ScopeContext.Data.UrlDataCollection.FrontEnd.LoginUrl);

            var homePage = PageFactory.CreatePage<ADBX_HomePage>();

            await _logger.ExecuteStepAsync("Create a new quote from the Home Page", async () =>
            {                
                var accountPopup = await homePage.ClickOnNewQuote();
                await accountPopup.FillForm(new Dictionary<string, string> { [AccountEmail] = email });
                await accountPopup.ClickPopupAdd();
                await BrowserManager.SwitchToLastTabAsync(10000);
            }, "Expected result: Account is created, interview start page is displayed");

            ScopeContext.Set(ctx => ctx.FrontEnd, FrontEndType.Interview);

            await _logger.ExecuteStepAsync("Start a Homeowners quote", async () =>
            {

                var startPage = PageFactory.CreatePage<Product_StartPage>();
                await _pageHelper!.InteractWithField(DateOfBirth);
                await startPage.SelectLob("Homeowners");
                await startPage.ClickContinue();
                var marketsPage = PageFactory.CreatePage<Product_MarketsPage>();
            }, "Expected result: Homeowners quote is started and a lead is created");

            await BrowserManager.SwitchToFirstTabAsync();
            ScopeContext.Set(ctx => ctx.FrontEnd, FrontEndType.ADBX);

            var leadsPage = await _logger.ExecuteStepAsync("Open the Leads Open queue in ADBX", async () =>
            {
                var leads = await AdbxHelper.NavigateToMenuAsync<ADBX_LeadsTabPage>(homePage, NavigationType.Leads);
                await _pageHelper!.InteractWithField(QueueTabs, "Open");

                return leads;
            }, "Expected result: Leads Open queue grid is displayed");

            await _logger.ExecuteStepAsync("Search the queue for the new lead and verify Score is N/A", async () =>
            {
                // Lead processing has a backend delay, so re-search until the results table appears.
                // Each attempt waits up to 5s for the table (returning early the moment it shows); if
                // it's still missing, Polly backs off and searches again within the retry budget.
                await _pollyRetry.ExecuteWithExceptionAsync(async () =>
                {
                    await leadsPage.SearchRecentRecords(email);
                    return await _pageHelper!.IsTableDisplayed(3000);
                }, 30, $"Lead with email '{email}' was not found/indexed within the 30 seconds");

                var rowData = await _pageHelper!.GetRowData(1);
                Assert.That(rowData, Does.ContainKey("Score"),
                    "Lead row data should contain a Score column");

                var score = rowData["Score"].Replace('\u00A0', ' ').Trim();
                _logger.LogDataValidation("LeadScore",
                    score == "N/A",
                    "N/A",
                    score,
                    "New lead's Score column should show N/A");
                Assert.That(score, Is.EqualTo("N/A"));
            }, "Expected result: The lead's Score column shows N/A");
        }
    }
}
