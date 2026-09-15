using Bolt.Automation.ApiClients.AdbxApi;
using Bolt.Automation.ApiClients.GetQuoteApi.Interfaces;
using Bolt.Automation.ApiClients.Infrastructure;
using Bolt.Automation.Common;
using Bolt.Automation.Common.Enums;
using Bolt.Automation.Common.Logging.Extentions;
using Bolt.Automation.Common.Utils;
using Bolt.Automation.FrontEnds.Projects.ADBX.Popups;
using Bolt.Automation.InternalServices.Database.Queries.Main;
using Bolt.Automation.Tests.TestExtension.Attributes;
using Bolt.Automation.Tests.TestExtension.Base;
using Bolt.Automation.Tests.TestHelpers.ADBX;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using static Bolt.Automation.FrontEnds.Projects.ADBX.FormData.ADBX_FieldNames;
using AuthorAttribute = Bolt.Automation.Tests.TestExtension.Attributes.AuthorAttribute;

namespace Bolt.Automation.Tests.Tests.AdbxTests
{
    public class ApplicantsTests : AdbxUITestBase
    {
        public FrontEndType? FrontEnd { get; set; }
        public IGetQuoteApi _getQuoteApi = null!;
        protected IAdbxApiClientFactory _adbxApifactory = null!;
        protected IMainQueries? _mainQueries = null!;

        protected override void ResolveServices()
        {
            var refitApiLocator = _testScope.ServiceProvider.GetRequiredService<RefitApiServiceLocator>();
            _getQuoteApi = refitApiLocator.GetRequiredService<IGetQuoteApi>();
            _adbxApifactory = refitApiLocator.GetRequiredService<IAdbxApiClientFactory>();
        }

        [Test]
        [Tenant(Tenant.BOLTAG)]
        [Category("ADBX")]
        [Category("CRM")]
        [Category("GetQuoteApi")]
        [Author(Author.Andrii)]
        [TestCaseId(221397)]
        [Description("Create new applicant via get quote api and update account from UI")]
        public async Task BOLTAG_Update_New_Applicant_UI_Test()
        {
            ScopeContext.Set(ctx => ctx.CurrentUser, TestContextAccessor.CurrentUserCollection.ConsumerKeller);

            var applicant = TestDataProvider.TestData.ApplicantTestData.ApplicantTestData.Applicants.RandomApplicant;

            await _logger.ExecuteStepAsync("Create Applicant via GetQuote API", async () =>
            {
                var applicantResult = await _getQuoteApi.CreateApplicantAsync(applicant);
                applicantResult.EnsureSuccessContent($"Failed to create application : {applicantResult.GetErrorContent()}");

            }, "Applicant is created via GetQuote API");

            await AdbxHelper.LoginAsync(
                TestContextAccessor.CurrentUserCollection.ServiceAgent,
                ScopeContext.Data.UrlDataCollection.AdbxApi.LoginUrl);

            var (isFound, accountSummaryPage) = await AdbxHelper.SearchAndOpenAccountFromHomeAsync(applicant.Email, "Email");

            _logger.LogBusinessRule("Account Found By Email",
                isFound,
                $"Account with first name {applicant.FirstName} should be found by email");
            Assert.That(isFound, Is.True, $"Failed. account with first name {applicant.FirstName} is not found");

            var accountDefaults = await _logger.ExecuteStepAsync("Update Account Information", async () =>
            {
                await Task.Delay(1000);
                await _pageHelper!.InteractWithField(EditAccountButton);

                var updateAccountPopup = PageFactory.CreatePage<ADBX_EnterUpdateAccountInformationPopup>();

                var defaults = AdbxTestHelper.BuildExpectedAccountDefaults();
                defaults.Remove("PopupTCPAConsent");
                defaults.Remove("SummaryTCPAConsent");
                defaults.Remove("BusinessName");

                await updateAccountPopup.FillForm(null);
                await updateAccountPopup.ClickPopupUpdate();

                return defaults;
            }, "Account information is updated with the default values");

            await _logger.ExecuteStepAsync("Verify Updated Account Details", async () =>
            {
                var actualDetails = await accountSummaryPage!.GetSummaryData();

                Assert.Multiple(() =>
                {
                    foreach (var expectedPair in accountDefaults)
                    {
                        var keyExists = actualDetails.ContainsKey(expectedPair.Key);
                        _logger?.LogBusinessRule($"Field Existence - {expectedPair.Key}", keyExists,
                            $"Field '{expectedPair.Key}' {(keyExists ? "exists" : "does not exist")} in account details");
                        Assert.That(keyExists, Is.True,
                            $"Field '{expectedPair.Key}' {(keyExists ? "exists" : "does not exist")} in account details");

                        if (keyExists)
                        {
                            var valueMatches = expectedPair.Value == actualDetails[expectedPair.Key];
                            _logger?.LogBusinessRule($"Field Value - {expectedPair.Key}", valueMatches,
                                $"Field '{expectedPair.Key}' - Expected: '{expectedPair.Value}', Actual: '{actualDetails[expectedPair.Key]}'");
                            Assert.That(actualDetails[expectedPair.Key], Is.EqualTo(expectedPair.Value));
                        }
                    }
                });
            }, "Updated account details match the expected defaults");
        }

        [Test]
        [Tenant(Tenant.BOLTAG)]
        [Category("ADBX")]
        [Category("CRM")]
        [Category("GetQuoteApi")]
        [Author(Author.Andrii)]
        [TestCaseId(221399)]
        [Description("Create new applicant via get quote api and update appilcant via get quote api")]
        public async Task BOLTAG_Update_New_Applicant_GetQuoteApi_Test()
        {
            var consumerUser = TestContextAccessor.CurrentUserCollection.ConsumerKeller;
            ScopeContext.Set(ctx => ctx.CurrentUser, consumerUser);

            var applicant = TestDataProvider.TestData.ApplicantTestData.ApplicantTestData.Applicants.RandomApplicant;

            var applicantUpdate = TestDataProvider.TestData.ApplicantTestData.ApplicantTestData.Applicants.RandomApplicantUpdate;

            var applicantId = await _logger.ExecuteStepAsync("Create And Update Applicant Via GetQuote API", async () =>
            {
                var applicantResult = await _getQuoteApi.CreateApplicantAsync(applicant);
                applicantResult.EnsureSuccessContent($"Failed to create application : {applicantResult.GetErrorContent()}");

                var id = applicantResult.Content.Id;

                var applicantUpdateResult = await _getQuoteApi.UpdateApplicantAsync(id, applicantUpdate);
                applicantUpdateResult.EnsureSuccessContent($"Failed to create application : {applicantUpdateResult.GetErrorContent()}", true);

                return id;
            }, "Applicant is created and updated via GetQuote API");

            await _logger.ExecuteStepAsync("Verify Updated Applicant Details", async () =>
            {
                var getApplicantResult = await _getQuoteApi.GetApplicantAsync(applicantId);

                getApplicantResult.EnsureSuccessContent($"Failed to create application : {getApplicantResult.GetErrorContent()}");

                var getApplicant = getApplicantResult.Content;
                var getApplicantAddress = getApplicant.Addresses.First();
                var applicantUpdateAddress = applicantUpdate.Addresses.First();

                Assert.Multiple(() =>
                {
                    Assert.That(getApplicant.Email, Is.EqualTo(applicantUpdate.Email),
                        $"Updated applicant email should match request data. Actual: {getApplicant.Email}, expected :{applicantUpdate.Email}");
                    Assert.That(getApplicant.FirstName, Is.EqualTo(applicantUpdate.FirstName),
                        $"Updated applicant first name should match request data. Actual: {getApplicant.FirstName}, expected :{applicantUpdate.FirstName}");
                    Assert.That(getApplicant.LastName, Is.EqualTo(applicantUpdate.LastName),
                        $"Updated applicant last name should match request data. Actual: {getApplicant.LastName}, expected :{applicantUpdate.LastName}");
                    Assert.That(getApplicant.PhoneNumbers.First(), Is.EqualTo(applicantUpdate.PhoneNumbers.First()),
                        $"Updated applicant phone number should match request data. Actual: {getApplicant.PhoneNumbers.First()}, expected :{applicantUpdate.PhoneNumbers.First()}");
                    Assert.That(getApplicantAddress.AddressLine1, Is.EqualTo(applicantUpdateAddress.AddressLine1),
                        $"Updated applicant address line 1 should match request data. Actual: {getApplicantAddress.AddressLine1}, expected :{applicantUpdateAddress.AddressLine1}");
                    Assert.That(getApplicantAddress.AddressLine2, Is.EqualTo(applicantUpdateAddress.AddressLine2),
                        $"Updated applicant address line 2 should match request data. Actual: {getApplicantAddress.AddressLine2}, expected :{applicantUpdateAddress.AddressLine2}");
                    Assert.That(getApplicantAddress.City, Is.EqualTo(applicantUpdateAddress.City),
                        $"Updated applicant address city should match request data. Actual: {getApplicantAddress.City}, expected :{applicantUpdateAddress.City}");
                    Assert.That(getApplicantAddress.County, Is.EqualTo(applicantUpdateAddress.County),
                        $"Updated applicant address county should match request data. Actual: {getApplicantAddress.County}, expected :{applicantUpdateAddress.County}");
                    Assert.That(getApplicantAddress.State, Is.EqualTo(applicantUpdateAddress.State),
                        $"Updated applicant address state should match request data. Actual: {getApplicantAddress.State}, expected :{applicantUpdateAddress.State}");
                    Assert.That(getApplicantAddress.ZipCode, Is.EqualTo(applicantUpdateAddress.ZipCode),
                        $"Updated applicant address zip code should match request data. Actual: {getApplicantAddress.ZipCode}, expected :{applicantUpdateAddress.ZipCode}");
                });
            }, "Updated applicant details match the update request data");
        }

        [Test]
        [Tenant(Tenant.BOLTAG)]
        [Category("ADBX")]
        [Category("CRM")]
        [Category("GetQuoteApi")]
        [Author(Author.Andrii)]
        [TestCaseId(221400)]
        [Description("Create new applicant via get quote api and update appilcant via get quote api, check updates on account summary page")]
        public async Task BOLTAG_Update_New_Applicant_GetQuoteApi_ADBX_Test()
        {
            ScopeContext.Set(ctx => ctx.CurrentUser, TestContextAccessor.CurrentUserCollection.ConsumerKeller);

            var applicant = TestDataProvider.TestData.ApplicantTestData.ApplicantTestData.Applicants.RandomApplicant;
            var applicantUpdate = TestDataProvider.TestData.ApplicantTestData.ApplicantTestData.Applicants.RandomApplicantUpdate;

            await _logger.ExecuteStepAsync("Create And Update Applicant Via GetQuote API", async () =>
            {
                var applicantResult = await _getQuoteApi.CreateApplicantAsync(applicant);
                applicantResult.EnsureSuccessContent($"Failed to create application : {applicantResult.GetErrorContent()}");

                var applicantId = applicantResult.Content.Id;

                var applicantUpdateResult = await _getQuoteApi.UpdateApplicantAsync(applicantId, applicantUpdate);
                applicantUpdateResult.EnsureSuccessContent($"Failed to create application : {applicantUpdateResult.GetErrorContent()}", true);
            }, "Applicant is created and updated via GetQuote API");

            await AdbxHelper.LoginAsync(
                TestContextAccessor.CurrentUserCollection.ServiceAgent,
                ScopeContext.Data.UrlDataCollection.AdbxApi.LoginUrl);

            var (isFound, accountSummaryPage) = await AdbxHelper.SearchAndOpenAccountFromHomeAsync(applicantUpdate.Email, "Email");

            Assert.That(isFound, Is.True, $"Failed. account with first name {applicantUpdate.FirstName} is not found");

            await _logger.ExecuteStepAsync("Verify Account Summary Details", async () =>
            {
                var actualDetails = await accountSummaryPage!.GetSummaryData();

                var fullAddress = $"{applicantUpdate.Addresses.First().AddressLine1} {applicantUpdate.Addresses.First().City}, {applicantUpdate.Addresses.First().State} US {applicantUpdate.Addresses.First().ZipCode}";
                var expectedSince = DateTime.Today.ToString("MM/dd/yyyy");
                const string expectedTcpaConsent = "No Promotional:Not SetTransactional:Not SetConversational:Not Set";

                Assert.Multiple(() =>
                {
                    Assert.That(actualDetails["Email"], Is.EqualTo(applicantUpdate.Email),
                        $"Account summary email should match updated applicant data. Actual: {actualDetails["Email"]}, expected :{applicantUpdate.Email}");
                    Assert.That(actualDetails["Phone"], Is.EqualTo(applicantUpdate.PhoneNumbers.First().Number),
                        $"Account summary phone should match updated applicant data. Actual: {actualDetails["Phone"]}, expected :{applicantUpdate.PhoneNumbers.First().Number}");
                    Assert.That(actualDetails["Since"], Is.EqualTo(expectedSince),
                        $"Account summary since date should be today's date. Actual: {actualDetails["Since"]}, expected :{expectedSince}");
                    Assert.That(actualDetails["BusinessLine"], Is.EqualTo("Personal"),
                        $"Account summary business line should be Personal. Actual: {actualDetails["BusinessLine"]}, expected :Personal");
                    Assert.That(actualDetails["TCPAConsent"], Is.EqualTo(expectedTcpaConsent),
                        $"Account summary TCPA consent should reflect default no-consent state. Actual: {actualDetails["TCPAConsent"]}, expected :{expectedTcpaConsent}");
                    Assert.That(actualDetails["Address"], Is.EqualTo(fullAddress),
                        $"Account summary address should match updated applicant address. Actual: {actualDetails["Address"]}, expected :{fullAddress}");
                });
            }, "Account summary details match the updated applicant data");
        }

        [Test]
        [Tenant(Tenant.BOLTAG)]
        [Category("ADBX")]
        [Category("CRM")]
        [Category("GetQuoteApi")]
        [Category("Sanity")]
        [Author(Author.Andrii)]
        [RunIn(includeStaging: true)]
        [TestCaseId(221391)]
        [Description("Create new applicant via get quote api and check account summary page on ADBX")]
        public async Task BOLTAG_Create_New_Applicant_Test()
        {
            const string expectedTcpaConsent = "No Promotional:Not SetTransactional:Not SetConversational:Not Set";
            var apiUser = Environment == Common.Environment.Staging
                ? TestContextAccessor.CurrentUserCollection.ConsumerOrganicPL
                : TestContextAccessor.CurrentUserCollection.ConsumerKeller;
            var loginUser = TestContextAccessor.CurrentUserCollection.ServiceAgent;

            var applicant = TestDataProvider.TestData.ApplicantTestData.ApplicantTestData.Applicants.RandomApplicant;

            ScopeContext.Set(ctx => ctx.CurrentUser, apiUser);
            await _logger.ExecuteStepAsync("Create applicant via GetQuote API", async () =>
            {
                ScopeContext.Set(ctx => ctx.CurrentUser, apiUser);
                await _getQuoteApi.CreateApplicantAsync(applicant).EnsureSuccessContentAsync();
            }, "Expected result: Applicant is created successfully");

            await AdbxHelper.LoginAsync(
                loginUser,
                TestContextAccessor.CurrentUrlCollection.FrontEnd.LoginUrl);

            var (isFound, accountSummaryPage) = await AdbxHelper.SearchAndOpenAccountFromHomeAsync(applicant.Email, "Email");

            _logger.LogBusinessRule("Account Found By Email",
                isFound,
                $"Account with first name {applicant.FirstName} should be found by email");
            Assert.That(isFound, Is.True, $"Failed. account with first name {applicant.FirstName} is not found");

            await _logger.ExecuteStepAsync("Verify Account Summary data", async () =>
            {
                var actualDetails = await accountSummaryPage!.GetSummaryData();
                var fullAddress = $"{applicant.Addresses.First().AddressLine1} {applicant.Addresses.First().City}, {applicant.Addresses.First().State} US {applicant.Addresses.First().ZipCode}";
                var expectedSince = DateTime.Today.ToString("MM/dd/yyyy");

                Assert.Multiple(() =>
                {
                    Assert.That(actualDetails["Email"], Is.EqualTo(applicant.Email),
                        $"Account summary email should match created applicant data. Actual: {actualDetails["Email"]}, expected :{applicant.Email}");
                    Assert.That(actualDetails["Phone"], Is.EqualTo(applicant.PhoneNumbers.First().Number),
                        $"Account summary phone should match created applicant data. Actual: {actualDetails["Phone"]}, expected :{applicant.PhoneNumbers.First().Number}");
                    Assert.That(actualDetails["Since"], Is.EqualTo(expectedSince),
                        $"Account summary since date should be today's date. Actual: {actualDetails["Since"]}, expected :{expectedSince}");
                    Assert.That(actualDetails["BusinessLine"], Is.EqualTo("Personal"),
                        $"Account summary business line should be Personal. Actual: {actualDetails["BusinessLine"]}, expected :Personal");
                    Assert.That(actualDetails["TCPAConsent"], Is.EqualTo(expectedTcpaConsent),
                        $"Account summary TCPA consent should reflect default no-consent state. Actual: {actualDetails["TCPAConsent"]}, expected :{expectedTcpaConsent}");
                    Assert.That(actualDetails["Address"], Is.EqualTo(fullAddress),
                        $"Account summary address should match created applicant address. Actual: {actualDetails["Address"]}, expected :{fullAddress}");
                });
            }, "Expected result: Email, Phone, Since, BusinessLine, TCPAConsent, and Address match the created applicant");
        }

        [Test]
        [Tenant(Tenant.KRAFTLAKEX)]
        [Category("ADBX")]
        [Category("CRM")]
        [Category("GetQuoteApi")]
        [Category("Sanity")]
        [RunIn(includeStaging: true)]
        [Author(Author.Andrii)]
        [TestCaseId(242729)]
        [Description("Create new applicant via get quote api and check account summary page on ADBX")]
        public async Task KRAFTLAKEX_Create_New_Applicant_Test()
        {
            var user = Environment == Common.Environment.Uat
                ? TestContextAccessor.CurrentUserCollection.LSP1
                : TestContextAccessor.CurrentUserCollection.Agent;

            var applicant = TestDataProvider.TestData.ApplicantTestData.ApplicantTestData.Applicants.RandomApplicant;

            ScopeContext.Set(ctx => ctx.CurrentUser, user);
            await _logger.ExecuteStepAsync("Create applicant via GetQuote API", async () =>
            {
                await _getQuoteApi.CreateApplicantAsync(applicant).EnsureSuccessContentAsync();
            }, "Expected result: Applicant is created successfully");

            await AdbxHelper.LoginAsync(
                    user,
                    TestContextAccessor.CurrentUrlCollection.FrontEnd.LoginUrl);

            var (isFound, accountSummaryPage) = await AdbxHelper.SearchAndOpenAccountFromHomeAsync(applicant.Email, "Email");

            _logger.LogBusinessRule("Account Found By Email",
                isFound,
                $"Account with first name {applicant.FirstName} should be found by email");
            Assert.That(isFound, Is.True, $"Failed. account with first name {applicant.FirstName} is not found");

            await _logger.ExecuteStepAsync("Verify Account Summary data", async () =>
            {
                var actualDetails = await accountSummaryPage!.GetSummaryData();
                var fullAddress = $"{applicant.Addresses.First().AddressLine1} {applicant.Addresses.First().City}, {applicant.Addresses.First().State} US {applicant.Addresses.First().ZipCode}";
                var expectedSince = DateTime.Today.ToString("MM/dd/yyyy");

                Assert.Multiple(() =>
                {
                    Assert.That(actualDetails["Email"], Is.EqualTo(applicant.Email),
                        $"Account summary email should match created applicant data. Actual: {actualDetails["Email"]}, expected :{applicant.Email}");
                    Assert.That(actualDetails["Phone"], Is.EqualTo(applicant.PhoneNumbers.First().Number),
                        $"Account summary phone should match created applicant data. Actual: {actualDetails["Phone"]}, expected :{applicant.PhoneNumbers.First().Number}");
                    Assert.That(actualDetails["Since"], Is.EqualTo(expectedSince),
                        $"Account summary since date should be today's date. Actual: {actualDetails["Since"]}, expected :{expectedSince}");
                    Assert.That(actualDetails["BusinessLine"], Is.EqualTo("Personal"),
                        $"Account summary business line should be Personal. Actual: {actualDetails["BusinessLine"]}, expected :Personal");
                    Assert.That(actualDetails["Address"], Is.EqualTo(fullAddress),
                        $"Account summary address should match created applicant address. Actual: {actualDetails["Address"]}, expected :{fullAddress}");
                });
            }, "Expected result: Email, Phone, Since, BusinessLine, and Address match the created applicant");
        }
    }
}