using Bolt.Automation.ApiClients.GetQuoteApi.Interfaces;
using Bolt.Automation.ApiClients.GetQuoteApi.Models.Application;
using Bolt.Automation.ApiClients.Infrastructure;
using Bolt.Automation.Common;
using Bolt.Automation.Common.Enums;
using Bolt.Automation.Common.Exceptions;
using Bolt.Automation.Common.Logging.Extentions;
using Bolt.Automation.Common.Models.TestData.DataModels.PersonalLine;
using Bolt.Automation.Common.PollyRetry;
using Bolt.Automation.Common.Utils;
using Bolt.Automation.FrontEnds.CommonHelpers;
using Bolt.Automation.FrontEnds.Projects.ADBX.Base;
using Bolt.Automation.FrontEnds.Projects.ADBX.Pages;
using Bolt.Automation.FrontEnds.Projects.ADBX.Pages.Leads;
using Bolt.Automation.FrontEnds.Projects.ADBX.Pages.Quotes;
using Bolt.Automation.FrontEnds.Projects.ADBX.Popups;
using Bolt.Automation.FrontEnds.Projects.D2C.FormData;
using Bolt.Automation.FrontEnds.Projects.D2C.Pages;
using Bolt.Automation.FrontEnds.Projects.Interview.Pages;
using Bolt.Automation.FrontEnds.Projects.STS;
using Bolt.Automation.TestDataProvider.Providers.GetQuoteApiDataProvider;
using Bolt.Automation.TestDataProvider.TestData.CommonTestData;
using Bolt.Automation.Tests.TestExtension.Attributes;
using Bolt.Automation.Tests.TestExtension.Base;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using static Bolt.Automation.FrontEnds.Projects.ADBX.FormData.ADBX_FieldNames;
using AuthorAttribute = Bolt.Automation.Tests.TestExtension.Attributes.AuthorAttribute;
using FieldNames = Bolt.Automation.FrontEnds.FormData.Common.FieldNames;

namespace Bolt.Automation.Tests.Tests.AdbxTests
{
    public class AdbxTcpaTests : AdbxUITestBase
    {
        protected IGetQuoteApi _getQuoteApi = null!;
        protected FrontEndType? FrontEnd { get; set; }

        private IPollyRetryService _pollyRetry = null!;

        protected override void ResolveServices()
        {
            var refitApiLocator = _testScope.ServiceProvider.GetRequiredService<RefitApiServiceLocator>();
            _getQuoteApi = refitApiLocator.GetRequiredService<IGetQuoteApi>();
            _pollyRetry = _testScope.ServiceProvider.GetRequiredService<IPollyRetryService>();
        }

        protected async Task<(ApplicationRequestModel<PersonalLineData> RequestData, string ApplicationId)> CreateTcpaApplicationAsync(bool? iAgreeToReceiveEmailsByBolt)
        {
            var consumerUser = TestContextAccessor.CurrentUserCollection.ConsumerKeller;
            ScopeContext.Set(ctx => ctx.CurrentUser, consumerUser);

            var requestData = new ApplicationRequestModel<PersonalLineData>
            {
                Products = TestDataProvider.TestData.ApplicationTestData.ApplicationTestData.Products.Homeowners,
                Data = PersonalLineDataProvider.GetPersonalHomeData()
            };

            requestData.Data.IAgreeToReceiveEmailsByBolt = iAgreeToReceiveEmailsByBolt;
            requestData.Data.PLSquareFootage = 1000;
            requestData.Data.MailingAddress = AddressData.TX_Thomaston;
            var createApplicationResponse = await _getQuoteApi.CreateAndSubmitApplicationAsync(requestData)
                .EnsureSuccessContentAsync();
            var applicationId = createApplicationResponse.ApplicationId;

            return (requestData, applicationId);
        }

        protected async Task NavigateToSearchResults(string searchTerm)
        {
            await AdbxHelper.LoginAsync(
          TestContextAccessor.CurrentUserCollection.ServiceAgent,
          ScopeContext.Data.UrlDataCollection.FrontEnd.LoginUrl);

            var homePage = PageFactory.CreatePage<ADBX_HomePage>();
            await homePage.SearchFor(searchTerm);
        }

        protected async Task NavigateToSearchAndSelectRecord(string searchTerm, bool selectLeadsTab = false)
        {
            await NavigateToSearchResults(searchTerm);
            if (selectLeadsTab)
            {
                await _pageHelper!.InteractWithField(LeadsTabSearchResults);
            }
            await _pageHelper!.SelectTableRowAsync(1);
        }

        protected async Task ValidateTcpaConsentStatus<T>(T summaryPage, string expectedConsent, bool refreshPage = false)
            where T : ADBX_BasePage
        {
            if (refreshPage)
            {
                await _pageHelper.RefreshPageAsync();
            }

            var summaryData = await summaryPage.GetSummaryData();
            var currentTcpaConsent = summaryData["TCPAConsent"];

            var pageType = typeof(T).Name.Contains("Account") ? "Account" : "Lead";
            _logger.Info($"Current {pageType} TCPA Consent: '{currentTcpaConsent}', Expected TCPA Consent: '{expectedConsent}'");
            Assert.That(currentTcpaConsent.Trim(), Is.EqualTo(expectedConsent));
        }

        protected async Task EditAccountTcpaSettings(List<string> consentsToRemove)
        {
            var accPopup = PageFactory.CreatePage<ADBX_EnterUpdateAccountInformationPopup>();
            await accPopup.RemoveTcpaConsent(consentsToRemove);
            await accPopup.ClickPopupUpdate();
        }

        protected void SetupConsumerUserContext(bool useKellerUser = false)
        {
            var consumerUser = useKellerUser
                ? TestContextAccessor.CurrentUserCollection.ConsumerKeller
                : TestContextAccessor.CurrentUserCollection.ConsumerOrganicPL;
            ScopeContext.Set(ctx => ctx.CurrentUser, consumerUser);
        }

        [Test]
        [Tenant(Tenant.BOLTAG)]
        [Category("ADBX")]
        [Category("CRM")]
        [Category("TCPA")]
        [Author(Author.Andrii)]
        [RunIn(includeProduction: true)]
        [TestCaseId(216955)]
        [Description("Create a new aaplicant to check that account tcpa status could be exported to file and downloaded. File has correct columns, rows count")]
        public async Task BOLTAG_TCPA_Download_Test()
        {
            SetupConsumerUserContext(useKellerUser: true);

            var applicant = TestDataProvider.TestData.ApplicantTestData.ApplicantTestData.Applicants.RandomApplicant;

            var applicantResult = await _logger.ExecuteStepAsync("Create applicant via API", async () =>
            {
                return await _getQuoteApi.CreateApplicantAsync(applicant).EnsureSuccessContentAsync();
            });

            await NavigateToSearchAndSelectRecord(applicant.Email);

            string timestamp = string.Empty;
            await _logger.ExecuteStepAsync("Access TCPA consent and download", async () =>
            {
                var accSummaryPage = PageFactory.CreatePage<ADBX_AccountSummaryPage>();
                await _pageHelper!.InteractWithField(AccountSummaryTCPAConsent);
                var et = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"));
                timestamp = et.ToString("MM/dd/yyyy") + "\u00A0\u00A0|\u00A0\u00A0" + et.ToString("hh:mm tt") + "\u00A0ET";
                await _pageHelper!.RefreshPageAsync();

            });

            await _logger.ExecuteStepAsync("Validate file download and content", async () =>
            {
                await _pageHelper!.InteractWithField(NotificationsIcon);
                var notificationsPopup = PageFactory.CreatePage<ADBX_NotificationsPopup>();
                var fileDownloadValidator = new FileDownloadValidatorHelper(await BrowserManager.GetPageAsync(), _logger);
                var downloadResult = await fileDownloadValidator.ValidateFileDownload(async () =>
                await notificationsPopup.DownloadExportFile("Your TCPA Consent History export is ready.", timestamp)
                );

                Assert.That(downloadResult.FileName.Contains(applicant.FirstName),
                $"Downloaded file name should contain the applicant's first name. Actual file name: {downloadResult.FileName}, applicant {applicant.FirstName}");

                Assert.That(downloadResult.RowCount, Is.EqualTo(3),
                    $"Should have exactly 3 rows for TCPA consent types. Actual count: {downloadResult.RowCount}");

                var expectedColumns = new List<string>()
                {
                    "Timestamp", "Use Case Type", "Status", "Source", "InitiatingUser", "Details"
                };

                Assert.That(downloadResult.Columns.SequenceEqual(expectedColumns), Is.True,
                    $"Downloaded file should contain the expected columns: {string.Join(",", expectedColumns)}, actual :{string.Join(",", downloadResult.Columns)}");
            });
        }

        [Test]
        [Tenant(Tenant.BOLTAG)]
        [Category("ADBX")]
        [Category("CRM")]
        [Category("TCPA")]
        [Author(Author.Andrii)]
        [RunIn(includeProduction: true)]
        [TestCaseId(221208)]
        [Description("Create and submit an application with IAgreeToReceiveEmailsByBolt = true and remove all consents for correspondent lead")]
        public async Task BOLTAG_TCPA_Update_Status_Lead_Test()
        {
            var expectedConsent = "No Promotional:NoTransactional:Not SetConversational:Not Set";
            var consentsToRemove = new List<string> { "Promotional", "Transactional", "Conversational" };

            var (requestData, applicationId) = await _logger.ExecuteStepAsync("Create TCPA application", async () =>
            {
                return await CreateTcpaApplicationAsync(true);
            });

            await AdbxHelper.LoginAsync(
                TestContextAccessor.CurrentUserCollection.ServiceAgent,
                ScopeContext.Data.UrlDataCollection.FrontEnd.LoginUrl);

            await _logger.ExecuteStepAsync("Navigate to search results and select lead", async () =>
            {
                // Mongo has an indexing lag (publication -> processing, plus the full-text index
                // build), so the lead may not appear immediately. Retry the
                // search until the lead is indexed and the table renders.
                // ExecuteWithExceptionAsync surfaces one clear message whether the retry budget
                // elapses (Polly throws) or the last attempt simply returns false.
                string? email = requestData.Data?.Email;

                var homePage = PageFactory.CreatePage<ADBX_HomePage>();

                await _pollyRetry.ExecuteWithExceptionAsync(async () =>
                {
                    await homePage.SearchFor(email);
                    await _pageHelper!.InteractWithField(LeadsTabSearchResults);
                    return await _pageHelper!.IsTableDisplayed(3000);
                }, 30, $"Lead with email '{email}' was not found/indexed within the 30 seconds");

                await _pageHelper!.SelectTableRowAsync(1);
            });

            await _logger.ExecuteStepAsync("Edit lead contact information", async () =>
            {
                await _pageHelper!.InteractWithField(LeadMoreOptionsButton);
                await _pageHelper!.InteractWithField(LeadEditContactInfoButton);
                await EditAccountTcpaSettings(consentsToRemove);
            });

            await _logger.ExecuteStepAsync("Validate updated TCPA consent status", async () =>
            {
                var leadSummaryPage = PageFactory.CreatePage<ADBX_LeadSummaryPage>();
                await ValidateTcpaConsentStatus(leadSummaryPage, expectedConsent);
            });
        }

        [Test]
        [Tenant(Tenant.BOLTAG)]
        [Category("ADBX")]
        [Category("CRM")]
        [Category("TCPA")]
        [Author(Author.Andrii)]
        [RunIn(includeProduction: true)]
        [TestCaseSource(nameof(CreateQuoteTestData))]
        [Description("create and submit new PL application with AgreeToReceiveMailByBolt: null/true/false to check that account tcpa status accordingly")]
        public async Task BOLTAG_TCPA_Create_Quote_Test(string receiveEmailsByBoltStr, string expectedConsent)
        {
            bool? receiveEmailsByBolt = receiveEmailsByBoltStr switch
            {
                "null" => null,
                _ => bool.Parse(receiveEmailsByBoltStr)
            };

            var (requestData, applicationId) = await _logger.ExecuteStepAsync("Create TCPA application", async () =>
            {
                return await CreateTcpaApplicationAsync(receiveEmailsByBolt);
            });

            await NavigateToSearchAndSelectRecord(applicationId);

            var quoteSummaryPage = PageFactory.CreatePage<ADBX_QuoteSummaryPage>();

            await _logger.ExecuteStepAsync("Validate Account TCPA consent status", async () =>
            {
                var accSummaryPage = await quoteSummaryPage.ClickOnLinkedAccount();
                await ValidateTcpaConsentStatus(accSummaryPage, expectedConsent);
            });

            await _logger.ExecuteStepAsync("Validate Lead TCPA consent status", async () =>
            {
                await _pageHelper.ClickBrowserBackButton();
                var leadSummaryPage = await quoteSummaryPage.ClickOnLinkedLead();
                await ValidateTcpaConsentStatus(leadSummaryPage, expectedConsent);
            });

        }

        private static IEnumerable<TestCaseData> CreateQuoteTestData()
        {
            yield return new TestCaseData("null",
                    "No Promotional:Not SetTransactional:Not SetConversational:Not Set")
                .SetProperty("TestCaseId", "233921");
            yield return new TestCaseData("true",
                    "Partial Promotional:YesTransactional:Not SetConversational:Not Set")
                .SetProperty("TestCaseId", "219267");
            yield return new TestCaseData("false",
                    "No Promotional:NoTransactional:Not SetConversational:Not Set")
                .SetProperty("TestCaseId", "233920");
        }

        [Test]
        [Tenant(Tenant.BOLTAG)]
        [Category("ADBX")]
        [Category("CRM")]
        [Category("TCPA")]
        [Author(Author.Andrii)]
        [RunIn(includeProduction: true)]
        [TestCaseSource(nameof(EditStatusAccountTestData))]
        [Description("create an applicant with all 3 concents, remove some of the concents, check TCPA status is No/Partial")]
        public async Task BOLTAG_TCPA_Edit_Status_Account_Test(string consentsToRemoveStr, string expectedTcpaConsent)
        {
            var applicant = TestDataProvider.TestData.ApplicantTestData.ApplicantTestData.Applicants.RandomApplicant;
            applicant.Optins = ["TCPA_Transactional_ThirdParty", "TCPA_Promotional_ThirdParty", "TCPA_Conversational_ThirdParty"];

            SetupConsumerUserContext(useKellerUser: true);

            var consentsToRemoveList = consentsToRemoveStr.Split(',').ToList();

            var applicantResult = await _logger.ExecuteStepAsync("Create applicant with TCPA opt-ins", async () =>
            {
                return await _getQuoteApi.CreateApplicantAsync(applicant).EnsureSuccessContentAsync();
            });

            await NavigateToSearchAndSelectRecord(applicant.Email);

            await _logger.ExecuteStepAsync("Edit account TCPA settings", async () =>
            {
                await Task.Delay(1000);
                await _pageHelper!.InteractWithField(EditAccountButton);
                await EditAccountTcpaSettings(consentsToRemoveList);
            });

            await _logger.ExecuteStepAsync("Validate updated TCPA consent status", async () =>
            {
                var accSummaryPage = PageFactory.CreatePage<ADBX_AccountSummaryPage>();
                await ValidateTcpaConsentStatus(accSummaryPage, expectedTcpaConsent, refreshPage: true);
            });
        }

        private static IEnumerable<TestCaseData> EditStatusAccountTestData()
        {
            yield return new TestCaseData("Conversational,Promotional,Transactional",
                    "No Promotional:NoTransactional:NoConversational:No")
                .SetProperty("TestCaseId", "232009");
            yield return new TestCaseData("Conversational,Promotional",
                    "Partial Promotional:NoTransactional:YesConversational:No")
                .SetProperty("TestCaseId", "222701");
        }

        [Test]
        [Tenant(Tenant.BOLTAG)]
        [Category("ADBX")]
        [Category("CRM")]
        [Category("TCPA")]
        [Author(Author.Andrii)]
        [RunIn(includeProduction: true)]
        [TestCaseId(234128)]
        [Description("create an applicant with all 3 concents, check consents on popup and summary page")]
        public async Task BOLTAG_TCPA_Create_Applicant_Full_Concent_Test()
        {
            var applicant = TestDataProvider.TestData.ApplicantTestData.ApplicantTestData.Applicants.RandomApplicant;
            applicant.Optins = ["TCPA_Transactional_ThirdParty", "TCPA_Promotional_ThirdParty", "TCPA_Conversational_ThirdParty"];
            var expectedTcpaConsentsPopup = new List<string>() { "Conversational", "Promotional", "Transactional" };
            var expectedTcpaConsentsPage = "Full Promotional:YesTransactional:YesConversational:Yes";
            SetupConsumerUserContext(useKellerUser: true);

            var applicantResult = await _logger.ExecuteStepAsync("Create applicant with TCPA opt-ins", async () =>
            {
                return await _getQuoteApi.CreateApplicantAsync(applicant).EnsureSuccessContentAsync();
            });

            await NavigateToSearchAndSelectRecord(applicant.Email);

            await _logger.ExecuteStepAsync("Check account TCPA statuses on Update Account info popup", async () =>
            {
                await Task.Delay(1000);
                await _pageHelper!.InteractWithField(EditAccountButton);
                var accPopup = PageFactory.CreatePage<ADBX_EnterUpdateAccountInformationPopup>();
                var actualConcents = await accPopup.GetTcpaConsents();

                Assert.That(actualConcents.SequenceEqual(expectedTcpaConsentsPopup), Is.True,
                    $"Account popup should list all three TCPA consents. Actual:{string.Join(",", actualConcents)}, Expected:{string.Join(",", expectedTcpaConsentsPopup)}");

            });

            await _logger.ExecuteStepAsync("Check account TCPA statuses on Account summary page", async () =>
            {
                var accSummaryPage = PageFactory.CreatePage<ADBX_AccountSummaryPage>();
                await ValidateTcpaConsentStatus(accSummaryPage, expectedTcpaConsentsPage);
            });
        }
        [Test]
        [Tenant(Tenant.BOLTAG)]
        [Category("ADBX")]
        [Category("CRM")]
        [Category("TCPA")]
        [TestCaseId(230683)]
        [Author(Author.Andrii)]
        [RunIn(includeProduction: true)]
        [Description("navigate to the quick consumer flow (d2c), check the both I agree to receive mail by bolt checkobxes on the personal Details Page, Check Account TCPA status on Agent portal(should be Partial)")]
        public async Task BOLTAG_TCPA_D2C_Receive_Email_By_Bolt_Yes_Test()
        {
            var email = RandomManager.GetRandomEmail();
            var frontEndUrls = ScopeContext.Data.UrlDataCollection.FrontEnd;
            var consumerUrl = frontEndUrls?.AdditionalUrls?.GetValueOrDefault("ConsumerShortInterviewPL")
                ?? throw new TestSetupException("ConsumerShortInterviewPL URL not configured in FrontEnd.AdditionalUrls");
            await _logger.ExecuteStepAsync("Setup D2C test data and context", async () =>
            {
                FieldRegistryD2C.Fields[FieldNames.Email].DefaultValue = email;
                FieldRegistryD2C.Fields[FieldNames.AgreeToReceiveEmail].DefaultValue = "true";
                FieldRegistryD2C.Fields[FieldNames.AgreeToReceiveEmailTransactional].DefaultValue = "true";

                ScopeContext.Set(ctx => ctx.FrontEnd, FrontEndType.D2C);
                ScopeContext.Set(ctx => ctx.CurrentUrl, consumerUrl);
            });

            await _logger.ExecuteStepAsync("Execute D2C flow and complete forms", async () =>
            {
                var perosnalDetailsPage = await Executor.Execute<D2C_YourAddressPage, D2C_PersonalDetailsPage>(
                    FrontEnds.Projects.D2C.Flows.FlowType.D2CCondoFlow,
                new Dictionary<string, string>
                {
                    [FieldNames.OnlineAddress] = "545 8th Ave, New York, NY 10018, USA",
                    [FieldNames.PLTypeOfDwelling] = "Condominium"
                },
                    fillForms: true,
                    pagesToSkip: [typeof(D2C_PropertiesUsagePage)]
                );
                await perosnalDetailsPage.FillForm();
                await Task.Delay(1000);
                await perosnalDetailsPage.ClickContinue();
            });

            await BrowserManager.OpenNewTabAsync();
            await NavigateToSearchAndSelectRecord(email);

            await _logger.ExecuteStepAsync("Validate TCPA consent status", async () =>
            {
                var accSummaryPage = PageFactory.CreatePage<ADBX_AccountSummaryPage>();
                await ValidateTcpaConsentStatus(accSummaryPage, "Partial Promotional:YesTransactional:Not SetConversational:Not Set");
            });
        }

        [Test]
        [Tenant(Tenant.BOLTAG)]
        [Category("ADBX")]
        [Category("CRM")]
        [Category("TCPA")]
        [TestCaseId(231402)]
        [Author(Author.Andrii)]
        [RunIn(includeProduction: true)]
        [Description("navigate to the full consumer flow (interview),  do not check the I agree to receive mail by bolt on the personal Details Page, Check Account TCPA status on Agent portal(should be No)")]
        public async Task BOLTAG_TCPA_Interview_Receive_Email_By_Bolt_No_Test()
        {
            var email = RandomManager.GetRandomEmail();
            ScopeContext.Set(ctx => ctx.FrontEnd, FrontEndType.Interview);
            var frontEndUrls = ScopeContext.Data.UrlDataCollection.FrontEnd;
            var consumerUrl = frontEndUrls?.AdditionalUrls?.GetValueOrDefault("ConsumerFullInterviewPL")
                ?? throw new TestSetupException("ConsumerFullInterviewPL URL not configured in FrontEnd.AdditionalUrls");

            await _logger.ExecuteStepAsync("Execute Interview homeowners flow", async () =>
            {
                var operatorPage = await Executor.Execute<Product_StartPage, Product_ResultsPage>(
                    FrontEnds.Projects.Interview.Flows.FlowType.InterviewHO3Flow,
                    new Dictionary<string, string>
                    {
                        [FieldNames.Email] = email,
                        [FieldNames.AgreeToReceiveEmail] = "No",
                        [FieldNames.GatedOrLimited] = "No",
                        [FieldNames.OccupancyType] = "Owner Primary",
                    },
                    fillForms: true,
                    startUrl: consumerUrl
                    );
            });
            await BrowserManager.OpenNewTabAsync();

            await NavigateToSearchAndSelectRecord(email);

            await _logger.ExecuteStepAsync("Validate TCPA consent status", async () =>
            {
                var accSummaryPage = PageFactory.CreatePage<ADBX_AccountSummaryPage>();
                await ValidateTcpaConsentStatus(accSummaryPage, "No Promotional:Not SetTransactional:Not SetConversational:Not Set");
            });
        }
    }
}