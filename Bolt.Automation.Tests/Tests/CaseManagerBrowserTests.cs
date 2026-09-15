using Bolt.Automation.ApiClients.AdbxApi;
using Bolt.Automation.ApiClients.CaseManagerApi.Interfaces;
using Bolt.Automation.ApiClients.GetQuoteApi.Interfaces;
using Bolt.Automation.ApiClients.GetQuoteApi.Models.Application;
using Bolt.Automation.ApiClients.Infrastructure;
using Bolt.Automation.Common;
using Bolt.Automation.Common.Enums;
using Bolt.Automation.Common.Logging.Extentions;
using Bolt.Automation.Common.Models.TestData.DataModels.CommercialLine;
using Bolt.Automation.Common.Utils;
using Bolt.Automation.ExternalServices.CasePortal.Infrastructure;
using Bolt.Automation.FrontEnds.Projects.ADBX.Pages;
using Bolt.Automation.FrontEnds.Projects.ADBX.Pages.Cases;
using Bolt.Automation.FrontEnds.Projects.ADBX.Pages.Leads;
using Bolt.Automation.FrontEnds.Projects.ADBX.Pages.Policies;
using Bolt.Automation.FrontEnds.Projects.ADBX.Pages.Quotes;
using Bolt.Automation.FrontEnds.Projects.ADBX.Popups;
using Bolt.Automation.FrontEnds.Projects.Interview.Pages;
using Bolt.Automation.FrontEnds.Projects.Interview.Popups;
using Bolt.Automation.InternalServices.Database.Queries.Main;
using Bolt.Automation.TestDataProvider.Providers.AdbxApiDataProvider;
using Bolt.Automation.TestDataProvider.Providers.CaseManagerAPIDataProvider;
using Bolt.Automation.TestDataProvider.Providers.GetQuoteApiDataProvider;
using Bolt.Automation.TestDataProvider.TestData.ApplicationTestData;
using Bolt.Automation.Tests.TestExtension.Attributes;
using Bolt.Automation.Tests.TestExtension.Base;
using Bolt.Automation.Tests.TestHelpers.ADBX;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using static Bolt.Automation.Common.Tenant;
using static Bolt.Automation.FrontEnds.Projects.ADBX.FormData.ADBX_FieldNames;
using AuthorAttribute = Bolt.Automation.Tests.TestExtension.Attributes.AuthorAttribute;

namespace Bolt.Automation.Tests.Tests
{
    public class CaseManagerBrowserTests : UITestBase
    {
        private ICaseManagerApi? _caseManagerApi;
        public IGetQuoteApi _getQuoteApi = null!;
        private IAdbxApiClientFactory _adbxApifactory = null!;
        private IMainQueries? _mainQueries;
        private ICasePortalApiClientFactory _casePortalApiClientFactory = null!;
        private AdbxTestHelper _adbxHelper = null!;
        protected override void ResolveServices()
        {
            var refitApiLocator = _testScope.ServiceProvider.GetRequiredService<RefitApiServiceLocator>();
            _mainQueries = _testScope.ServiceProvider.GetService<IMainQueries>();
            _caseManagerApi = refitApiLocator.GetService<ICaseManagerApi>();
            _getQuoteApi = refitApiLocator.GetRequiredService<IGetQuoteApi>();
            _adbxApifactory = _testScope.ServiceProvider.GetRequiredService<IAdbxApiClientFactory>();
            _casePortalApiClientFactory = _testScope.ServiceProvider.GetRequiredService<ICasePortalApiClientFactory>();
        }

        protected override void InitializeComponents()
        {
            _adbxHelper = new AdbxTestHelper(_logger, BrowserManager, PageFactory, _pageHelper!, ScopeContext);
        }

        [Test]
        [Tenant(KRAFTLAKEX)]
        [TestCaseId(198617)]
        [Category("CaseManagerApi")]
        [Author(Author.Sandy)]
        [Description("Gets a sale case deeplink from CaseManager API and verifies login navigates to the correct ADBX lead")]
        public async Task KraftLake_SaleCase_Deeplink_Login_ABDX()
        {
            var underwriter = await _logger.ExecuteStepAsync("Set casemanager user", async () =>
            {
                var uw = TestContextAccessor.CurrentUserCollection.Underwriter;
                ScopeContext.Set(ctx => ctx.CurrentUser, uw);
                return uw;
            });

            var resp = await _logger.ExecuteStepAsync("Get deeplink for sale case to login ADBX", async () =>
            {
                var caseExternalId = TestContextAccessor.GetTestSpecificValue("DeeplinkCaseExternalId");
                var result = await _caseManagerApi.GetCaseDeeplinkAsync(nameof(KRAFTLAKEX), caseExternalId, true)
                    .EnsureSuccessContentAsync("Failed to get sale case deeplink");
                return result;
            });

            await _logger.ExecuteStepAsync("Navigate to deeplink and verify URL", async () =>
            {
                await BrowserManager.NavigateAsync(resp.Deeplink);
                PageFactory.CreatePage<ADBX_LeadSummaryPage>();
                var currentUrl = BrowserManager.GetCurrentTab()?.Url;
                var leadId = TestContextAccessor.GetTestSpecificValue("DeeplinkCaseLeadId");
                Assert.That(currentUrl, Does.Contain(leadId));
            });
        }

        [Test]
        [Tenant(KRAFTLAKEX)]
        [TestCaseId(198636)]
        [Category("CaseManagerApi")]
        [Author(Author.Sandy)]
        [Description("Gets a service case deeplink from CaseManager API and verifies login navigates to the correct ADBX policy")]
        public async Task KraftLake_ServiceCase_Deeplink_Login_ABDX()
        {
            await ServiceCaseDeeplinkLoginAsync(nameof(KRAFTLAKEX));
        }

        [Test]
        [Tenant(BOLTACCESS)]
        [TestCaseId(198646)]
        [Category("CaseManagerApi")]
        [Author(Author.Sandy)]
        [Description("Gets a service case deeplink from CaseManager API and verifies login navigates to the correct ADBX policy")]
        public async Task BoltAccess_ServiceCase_Deeplink_Login_ABDX()
        {
            await ServiceCaseDeeplinkLoginAsync(nameof(BOLTACCESS));
        }

        private async Task ServiceCaseDeeplinkLoginAsync(string tenant)
        {
            var underwriter = await _logger.ExecuteStepAsync("Set casemanager user", async () =>
            {
                var uw = TestContextAccessor.CurrentUserCollection.Underwriter;
                ScopeContext.Set(ctx => ctx.CurrentUser, uw);
                return uw;
            });

            var resp = await _logger.ExecuteStepAsync("Get deeplink for service case to login ADBX", async () =>
            {
                var caseExternalId = TestContextAccessor.GetTestSpecificValue("DeeplinkServiceCaseExternalId");
                var result = await _caseManagerApi.GetCaseDeeplinkAsync(tenant, caseExternalId, false)
                    .EnsureSuccessContentAsync("Failed to get service case deeplink");
                return result;
            });

            await _logger.ExecuteStepAsync("Navigate to deeplink and verify URL", async () =>
            {
                await BrowserManager.NavigateAsync(resp.Deeplink);
                PageFactory.CreatePage<ADBX_PolicySummaryPage>();
                var currentUrl = BrowserManager.GetCurrentTab()?.Url;
                var policyId = TestContextAccessor.GetTestSpecificValue("DeeplinkServiceCasePolicyId");
                Assert.That(currentUrl, Does.Contain(policyId));
            });
        }


        [Retry(2)]
        [Test]
        [Tenant(BOLTACCESS)]
        [TestCaseId(166516)]
        [Category("Sanity")]
        [Category("CaseManagerApi")]
        [Author(Author.Sandy)]
        [Description("Submits an offline WC request with file, and verifies the agent message and attachments are synced to ADBX and Case Portal.")]
        public async Task BoltAccess_WC_Offline_Request()
        {
            var testUser = TestContextAccessor.CurrentUserCollection.AgentCL;
            // Step 1: Prepare test user and request data
            var (url, requestData) = await _logger.ExecuteStepAsync("Prepare test user and request data", async () =>
            {
                var loginUrl = testUser.LoginUrl;
                ScopeContext.Set(ctx => ctx.CurrentUser, testUser);
                ScopeContext.Set(ctx => ctx.FrontEnd, FrontEndType.ADBX);
                var reqData = new ApplicationRequestModel<CommercialLineData>
                {
                    Products = ApplicationTestData.Products.WorkersCompensation,
                    Data = CommercialLineDataProvider.GetCommercialLineData()
                };
                _logger.Info($"Test user: {testUser.UserExternalId}, LoginUrl: {loginUrl}");
                return (loginUrl, reqData);
            });

            // Step 2: Create quote via GetQuote API
            var friendlyId = await _logger.ExecuteStepAsync("Create quote via GetQuote API", async () =>
            {
                var response = await _getQuoteApi.CreateApplicationAsync(requestData).EnsureSuccessContentAsync();
                _logger.Info($"Quote created: {response.FriendlyId}");
                return response.FriendlyId;
            });

            // Step 3: Login and navigate to quote
            await _adbxHelper.LoginAndNavigateToQuoteAsync(testUser,
    url, friendlyId);

            // Step 4: Complete quote flow and submit offline request
            var text = await _logger.ExecuteStepAsync("Complete quote flow and submit offline request", async () =>
            {
                var accPopup = PageFactory.CreatePage<ADBX_EnterUpdateAccountInformationPopup>();
                await accPopup.ClosePopup();
                var quoteSummaryPage = PageFactory.CreatePage<ADBX_QuoteSummaryPage>();
                await quoteSummaryPage.ClickOnEditQuote();
                ScopeContext.Set(ctx => ctx.FrontEnd, FrontEndType.Interview);
                var businessPage = PageFactory.CreatePage<ProductBusinessProfilePageCL>();
                await businessPage.ClickContinue();
                var lobsPage = PageFactory.CreatePage<ProductSelectionPageCL>();
                await lobsPage.ClickContinue();
                var marketPage = PageFactory.CreatePage<ProductMarketResultsPageCL>();
                await marketPage.ClickOfflineRequest();
                var offlineRequestPopup = PageFactory.CreatePage<Interview_OfflineRequestPopup>();
                var offlineText = "AutoTest " + RandomManager.GetRandomString(8);
                await offlineRequestPopup.AddText(offlineText);
                await offlineRequestPopup.AddFileAsync();
                await offlineRequestPopup.ClickContinue();
                await offlineRequestPopup.ClickPopupConfirm();
                Assert.That(await offlineRequestPopup.IsRequestSubmittedButtonExists(), Is.True);
                _logger.Info($"Offline request submitted with text: {offlineText}");
                return offlineText;
            });

            // Step 5: Validate case submission in DB
            var (policyData, externalId) = await _logger.ExecuteStepAsync("Validate case submission in DB", async () =>
            {
                var polData = await _mainQueries.Policy.GetPolicyByFriendlyIdAsync(friendlyId);
                var csData = await _mainQueries.Case.GetCaseInfoByApplicationIdAsync(polData.Id);
                Assert.That(csData.IsSubmitted, Is.True, "Case not submitted successfully");
                _logger.Info($"PolicyId: {polData.Id}, CaseExternalId: {csData.ExternalId}");
                return (polData, csData.ExternalId);
            });

            // Step 6: Extract agent message from lead communications and Validate agent message text
            var (agentMessage, leadCommunications) = await _logger.ExecuteStepAsync("Extract agent message from lead communications", async () =>
            {
                var api = await _adbxApifactory.CreateApiClientAsync();
                var communications = await api.GetLeadCommunications(policyData.LeadId.ToString())
                    .EnsureSuccessContentAsync("Failed to get lead policies ");
                var agentMessageObj = communications.CommunicationsList?.FirstOrDefault(x => x.SentTo == "Bolt Access");
                Assert.That(agentMessageObj?.Description, Is.EqualTo(text),
             "Message sent by External agent has wrong description");
                return (agentMessageObj, communications);
            });

            // Step 7: Validate agent message and subject in portal
            await _logger.ExecuteStepAsync("Validate agent message and subject in portal", async () =>
            {
                var casePortalUser = TestContextAccessor.CurrentUserCollection.CasePortalUser;
                ScopeContext.Set(ctx => ctx.CurrentUser, casePortalUser);
                var portalApi = await _casePortalApiClientFactory.CreateApiClient();
                var result = await portalApi.GetAllMessagesByCaseIdAsync(nameof(BOLTACCESS), externalId, casePortalUser.UserExternalId)
                    .EnsureSuccessContentAsync();

                var messages = result?.ObjectProcessed?.Messages;
                Assert.That(messages?.Any(x => x?.Note?.Text == agentMessage?.Description), Is.True,
                    "No matching message found for the given description");
                Assert.That(messages?.Any(x => x?.Note?.Subject != null && x.Note.Subject.Contains(agentMessage?.Subject)), Is.True,
                    "No matching message found for the given subject");
            });
        }


        [Test]
        [Tenant(Tenant.KRAFTLAKEX)]
        [Category("ADBX")]
        [Category("CRM")]
        [Author(Author.Andrii)]
        [TestCaseId(226717)]
        [Description("Update Service Case as Underwirter uing CM api adn check updates on ADBX")]
        public async Task Kraftlake_Update_Service_Request_Show_Case_Workflow()
        {
            var policyId = TestContextAccessor.GetTestSpecificValue("PolicyId");
            var lspUser = Environment == Common.Environment.Uat
                    ? TestContextAccessor.CurrentUserCollection.Agent
                    : TestContextAccessor.CurrentUserCollection.LSP1;
            var underwriter = TestContextAccessor.CurrentUserCollection.Underwriter;

            // Set the LSP1/agent user for adbx
            _logger?.Info("Create Underwirter service Case for Policy using adbx api");
            ScopeContext.Set(ctx => ctx.CurrentUser, lspUser);

            var createCaseResponse = await _logger.ExecuteStepAsync("Create case for policy", async () =>
            {
                var policyCaseData = PolicyDataProvider.CreateUnderwiterPolicyCaseData(lspUser.Id);
                var api = await _adbxApifactory.CreateApiClientAsync();
                var createCaseResponse = await api.CreateUnderwriterCaseForPolicy
                    (policyId, policyCaseData)
                    .EnsureSuccessContentAsync();
                return createCaseResponse;
            });

            // Wait for CM Case ID using the new helper method
            var caseData = await _logger.ExecuteStepAsync("Wait for CM Case ID to be populated", async () =>
            {
                return await _mainQueries.CaseLogic.WaitForCmCaseIdAsync(createCaseResponse.Id.ToString());
            }, "Expected result: Case data retrieved with populated CM Case ID");

            var updateCaseModel = CaseDataProvider.UpdateServiceCaseData
                (caseData?.ExternalId, caseData?.CmCaseId, "Policy Issued");

            ScopeContext.Set(ctx => ctx.CurrentUser, underwriter);
            await _logger.ExecuteStepAsync("Update Case using Case Manager API as underwriter", async () =>
            {
                var updateCaseResponse = await _caseManagerApi.UpdateCaseAsync
                (nameof(Tenant.KRAFTLAKEX), updateCaseModel)
                .EnsureSuccessContentAsync();
            });

            // Set the LSP1/agent user for adbx
            ScopeContext.Set(ctx => ctx.FrontEnd, FrontEndType.ADBX);
            _logger?.Info("Login and check updates on case summary page, update case popup");
            await _adbxHelper.LoginAsync(
            lspUser,
            ScopeContext.Data.UrlDataCollection.FrontEnd.LoginUrl);

            var homePage = PageFactory.CreatePage<ADBX_HomePage>();
            await homePage.SearchFor(caseData?.CmCaseId);
            await _pageHelper!.SelectTableRowAsync(1);
            await _logger.ExecuteStepAsync("Check updates on case summary page", async () =>
            {
                var caseSummaryPage = PageFactory.CreatePage<ADBX_CaseSummaryPage>();
                var details = await caseSummaryPage.GetSummaryData();
                var actualStage = details["Stage"];

                Assert.That(actualStage.Equals(updateCaseModel.CaseWorkflow), Is.True,
                    $"Failed. actual stage is {actualStage}, expected {updateCaseModel.CaseWorkflow}");
            });

            await _logger.ExecuteStepAsync("Check Case Stage Dropdown is disabled on EditCase popup", async () =>
            {
                await _pageHelper.InteractWithField(EditCaseButton);
                var popupStage = await _pageHelper.GetFieldValue(CaseStageDropdown);
                var isStageEnabled = await _pageHelper.IsFieldEnabled("CaseStageDropdown");

                Assert.That(popupStage.Equals(updateCaseModel.CaseWorkflow), Is.True,
                    $"Failed. actual popup Stage is {popupStage}, expected {updateCaseModel.CaseWorkflow}");

                _logger.LogBusinessRule("Case Stage Dropdown Disabled",
                    !isStageEnabled,
                    isStageEnabled ? "Case stage dropdown is enabled, expected disabled" : "Case stage dropdown is disabled as expected");
                Assert.That(isStageEnabled, Is.False,
                        $"Failed. Case stage dropdown should be disabled. Actual: enabled");
            });
        }
    }
}
