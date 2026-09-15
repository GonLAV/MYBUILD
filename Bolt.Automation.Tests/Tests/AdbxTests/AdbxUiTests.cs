using Bolt.Automation.ApiClients.AdbxApi;
using Bolt.Automation.ApiClients.CaseManagerApi.Interfaces;
using Bolt.Automation.ApiClients.GetQuoteApi.Interfaces;
using Bolt.Automation.ApiClients.Infrastructure;
using Bolt.Automation.Common;
using Bolt.Automation.Common.Enums;
using Bolt.Automation.Common.Logging.Extentions;
using Bolt.Automation.Common.Utils;
using Bolt.Automation.FrontEnds.Projects.ADBX.Pages;
using Bolt.Automation.FrontEnds.Projects.ADBX.Popups;
using Bolt.Automation.InternalServices.Database.Queries.Main;
using Bolt.Automation.Tests.TestExtension.Attributes;
using Bolt.Automation.Tests.TestExtension.Base;
using Bolt.Automation.Tests.TestHelpers.ADBX;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using static Bolt.Automation.FrontEnds.Projects.ADBX.FormData.ADBX_FieldNames;
using AuthorAttribute = Bolt.Automation.Tests.TestExtension.Attributes.AuthorAttribute;
using Bolt.Automation.FrontEnds.Projects.ADBX.Pages.Quotes;

namespace Bolt.Automation.Tests.Tests.AdbxTests
{
    public class AdbxUiTests : AdbxUITestBase
    {
        public FrontEndType? FrontEnd { get; set; }
        public IGetQuoteApi _getQuoteApi = null!;
        protected ICaseManagerApi? _caseManagerApi;
        protected IMainQueries? _mainQueries;
        protected IAdbxApiClientFactory _adbxApifactory = null!;

        protected override void ResolveServices()
        {
            var refitApiLocator = _testScope.ServiceProvider.GetRequiredService<RefitApiServiceLocator>();
            _getQuoteApi = refitApiLocator.GetRequiredService<IGetQuoteApi>();
            _caseManagerApi = refitApiLocator.GetService<ICaseManagerApi>();
            _mainQueries = _testScope.ServiceProvider.GetService<IMainQueries>();
            _adbxApifactory = refitApiLocator.GetRequiredService<IAdbxApiClientFactory>();
        }

        [Test]
        [Tenant(Tenant.USAA)]
        [Category("ADBX")]
        [Category("CRM")]
        [Author(Author.Andrii)]
        [TestCaseId(71392)]
        [Description("Create new note for the quote and check timeline")]
        public async Task USAA_Create_New_Note_Quote_Test()
        {
            string subject = "Other";
            string expectedNoteText = "Auto" + RandomManager.GetRandomString(15);
            var formData = new Dictionary<string, string>
            {
                [NoteSubject] = subject,
                [NoteDescription] = expectedNoteText,
            };

            await AdbxHelper.LoginAsync(
                TestContextAccessor.CurrentUserCollection.Agent,
                ScopeContext.Data.UrlDataCollection.AdbxApi.LoginUrl);

            await _logger.ExecuteStepAsync("Step 1: Navigate to Quotes tab", async () =>
            {
                var homePage = PageFactory.CreatePage<ADBX_HomePage>();
                await homePage.ClickOnMenuTab(NavigationType.Quotes);
                PageFactory.CreatePage<ADBX_QuotesTabPage>();
            }, "Expected result: List of quote records is displayed");

            var quoteSummaryPage = await _logger.ExecuteStepAsync("Step 2: Select first Quote record", async () =>
            {
                await _pageHelper!.SelectTableRowAsync(1);
                var page = PageFactory.CreatePage<ADBX_QuoteSummaryPage>();
                if (await page.IsUpdateAccountInformationPopUpExists())
                {
                    var updateAccountPopup = PageFactory.CreatePage<ADBX_EnterUpdateAccountInformationPopup>();
                    await updateAccountPopup.ClosePopup();
                }
                return page;
            }, "Expected result: Quote data is displayed");

            var notePopup = await _logger.ExecuteStepAsync("Step 3: Click New Note button", async () =>
                await quoteSummaryPage.ClickOnNewNote(),
            "Expected result: 'Add your note information' pop-up appears");

            await _logger.ExecuteStepAsync("Step 4: Fill in note information", async () =>
            {
                _logger.LogJsonWithPreview("Note form data", formData);
                await notePopup.FillForm(formData);
            }, $"Expected result: Subject '{subject}' and Description entered successfully");

            await _logger.ExecuteStepAsync("Step 5: Click Add button and validate note in timeline", async () =>
            {
                await notePopup.ClickPopupAdd();

                var isNoteExists = await quoteSummaryPage.IsTimelineNoteExistsADBX(subject);
                Assert.That(isNoteExists, Is.True,
                    $"Note with subject '{subject}' {(isNoteExists ? "exists" : "does not exist")} in timeline");

                var actualNoteText = await quoteSummaryPage.GetTimeLineNoteBodyTextADBX(subject);
                var noteContentMatches = expectedNoteText.Equals(actualNoteText);

                Assert.That(actualNoteText, Is.EqualTo(expectedNoteText),
                    $"Note content - Expected: '{expectedNoteText}', Actual: '{actualNoteText}'");
            }, "Expected result: Pop-up closed, Quote data displayed, note appears in Timeline");
        }

        [Test]
        [Tenant(Tenant.BOLTAG)]
        [Category("ADBX")]
        [Category("CRM")]
        [Author(Author.Andrii)]
        [TestCaseId(93545)]
        [Description("Create New Personal Account via �New Quote� button and account appeared in Accounts Grid")]
        public async Task BOLTAG_Search_And_Open_Newly_Created_Account_Test()
        {
            await AdbxHelper.LoginAsync(
                TestContextAccessor.CurrentUserCollection.ServiceAgent,
                ScopeContext.Data.UrlDataCollection.AdbxApi.LoginUrl);

            var (email, homePage) = await AdbxHelper.CreateAccountViaNewQuoteAsync();
            var (isFound, _) = await AdbxHelper.OpenAccountFromAccountsTabAsync(homePage, email);

            Assert.That(isFound, Is.True, $"Account with email '{email}' was not found in the Accounts tab");
        }

        [Test]
        [Tenant(Tenant.BOLTAG)]
        [Category("ADBX")]
        [Category("CRM")]
        [TestCaseId(216457)]
        [Author(Author.Andrii)]
        [Description("Create New Personal Account via �New Quote� button and check account summary page")]
        public async Task BOLTAG_Create_Personal_Account_Via_New_Quote_Test_Check_Defaults()
        {
            var accountDefaults = AdbxTestHelper.BuildExpectedAccountDefaults();

            await AdbxHelper.LoginAsync(
                TestContextAccessor.CurrentUserCollection.ServiceAgent,
                ScopeContext.Data.UrlDataCollection.AdbxApi.LoginUrl);

            var (email, homePage) = await AdbxHelper.CreateAccountViaNewQuoteAsync();
            accountDefaults["Email"] = email;

            var (isFound, accountSummaryPage) = await AdbxHelper.OpenAccountFromAccountsTabAsync(homePage, email);

            Assert.That(isFound, Is.True, $"Account with email '{email}' was not found in the Accounts tab");

            await _logger.ExecuteStepAsync("Verify Account Summary data", async () =>
            {
                var actualDetails = await accountSummaryPage!.GetSummaryData();
                var detailsMatchDefaults = actualDetails.All(kvp =>
                    accountDefaults.TryGetValue(kvp.Key, out var value) &&
                    EqualityComparer<string>.Default.Equals(value, kvp.Value));

                _logger.LogDataValidation("Account Summary Data Matches Defaults",
                    detailsMatchDefaults,
                    "true",
                    detailsMatchDefaults.ToString(),
                    $"Account Summary data for '{email}' should match the expected defaults");
                Assert.That(detailsMatchDefaults, Is.True);
            }, "Expected result: Account Summary data matches input, TCPA consent is 'No Promotional:Not SetTransactional:Not SetConversational:Not Set', date reflects today");
        }

    }
}