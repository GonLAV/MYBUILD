using Bolt.Automation.ApiClients.AdbxApi;
using Bolt.Automation.ApiClients.CaseManagerApi.Interfaces;
using Bolt.Automation.ApiClients.GetQuoteApi.Interfaces;
using Bolt.Automation.ApiClients.GetQuoteApi.Models.Application;
using Bolt.Automation.ApiClients.Infrastructure;
using Bolt.Automation.Common;
using Bolt.Automation.Common.Enums;
using Bolt.Automation.Common.Logging.Extentions;
using Bolt.Automation.Common.Models.TestData.DataModels.PersonalLine;
using Bolt.Automation.Common.PollyRetry;
using Bolt.Automation.Common.Utils;
using Bolt.Automation.FrontEnds.Projects.ADBX.FormData;
using Bolt.Automation.FrontEnds.Projects.ADBX.Pages;
using Bolt.Automation.FrontEnds.Projects.ADBX.Pages.Leads;
using Bolt.Automation.FrontEnds.Projects.ADBX.Pages.Policies;
using Bolt.Automation.FrontEnds.Projects.ADBX.Pages.Quotes;
using Bolt.Automation.FrontEnds.Projects.ADBX.Popups;
using Bolt.Automation.InternalServices.Database.Queries.Main;
using Bolt.Automation.TestDataProvider.Providers.GetQuoteApiDataProvider;
using Bolt.Automation.TestDataProvider.TestData.ApplicationTestData;
using Bolt.Automation.TestDataProvider.TestData.CommonTestData;
using Bolt.Automation.Tests.TestExtension.Attributes;
using Bolt.Automation.Tests.TestExtension.Base;
using Bolt.Automation.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using static Bolt.Automation.FrontEnds.Projects.ADBX.FormData.ADBX_FieldNames;
using AuthorAttribute = Bolt.Automation.Tests.TestExtension.Attributes.AuthorAttribute;

namespace Bolt.Automation.Tests.Tests.AdbxTests
{
    public class PolicyTests : AdbxUITestBase
    {
        private IGetQuoteApi _getQuoteApi = null!;
        protected IMainQueries? _mainQueries;
        protected IPollyRetryService _pollyRetry = null!;

        protected override void ResolveServices()
        {
            var refitApiLocator = _testScope.ServiceProvider.GetRequiredService<RefitApiServiceLocator>();
            _getQuoteApi = refitApiLocator.GetRequiredService<IGetQuoteApi>();
            _mainQueries = _testScope.ServiceProvider.GetService<IMainQueries>();
            _pollyRetry = _testScope.ServiceProvider.GetRequiredService<IPollyRetryService>();
        }

        protected async Task AssertPoliciesExistAsync(
            string quoteNumber,
            string policyNumber,
            string secondPolicyNumber,
            ADBX_PolicySummaryPage policySummaryPage)
        {
            await AdbxHelper.NavigateInnerTabAsync<ADBX_PolicySummaryRenewalsTab>(policySummaryPage, NavigationType.Renewals);

            _logger.Info($"Asserting if first policy - # {policyNumber} exists under Renewals of policy - # {secondPolicyNumber}");
            var isPolicyExist = await _pageHelper!.IsSpecificColumnHaveData(policyNumber, "Policy Number");
            Assert.That(isPolicyExist, Is.True, $"Policy with number {policyNumber} is not found in Renewals tab");
            await policySummaryPage.ClickOnSpecificLink(NavigationType.Quote);

            var quoteSummaryPage = PageFactory.CreatePage<ADBX_QuoteSummaryPage>();
            await AdbxHelper.NavigateInnerTabAsync<ADBX_QuoteSummaryPoliciesTab>(quoteSummaryPage, NavigationType.Policies);

            isPolicyExist = await _pageHelper!.IsSpecificColumnHaveData(policyNumber, "Policy Number");
            var isSecondPolicyExist = await _pageHelper!.IsSpecificColumnHaveData(secondPolicyNumber, "Policy Number");
            _logger.Info($"Asserting if policies - {policyNumber} and {secondPolicyNumber} exist in Policies tab of the Quote {quoteNumber}");
            Assert.That(isPolicyExist && isSecondPolicyExist, Is.True, $"Policy with number {policyNumber} and/or its renewal policy {secondPolicyNumber} not found in Policies tab");
        }

        [Test]
        [Tenant(Tenant.BOLTAG)]
        [Category("ADBX")]
        [Category("CRM")]
        [Category("Policy")]
        [TestCaseId(225735)]
        [Author(Author.Sandy)]
        [Description("Create renewal policy and check info required after requote")]
        public async Task BOLTAG_Renewal_Info_Required()
        {
            var user = TestContextAccessor.CurrentUserCollection.ServiceAgent;
            ScopeContext.Set(ctx => ctx.CurrentUser, user);

            var accountId = TestContextAccessor.GetTestSpecificValue("RenewalsAccountExternalId");
            var requestData = new ApplicationRequestModel<PersonalLineData>
            {
                Products = ApplicationTestData.Products.Homeowners,
                ApplicantId = accountId,
                Data = PersonalLineDataProvider.GetPersonalHomeData()
            };

            var friendlyId = await _logger.ExecuteStepAsync("Creating a new quote via GetQuote API.", async () =>
            {
                var createApplicationResponse = await _getQuoteApi.CreateApplicationAsync(requestData).EnsureSuccessContentAsync();
                return createApplicationResponse.FriendlyId;
            });

            await AdbxHelper.LoginAndNavigateToQuoteAsync(user,
            ScopeContext.Data.UrlDataCollection.FrontEnd.LoginUrl, friendlyId);

            var policyNumber = await _logger.ExecuteStepAsync($"Create initial policy", async () =>
            {
                var (policySummaryPage, policyNumber) = await AdbxHelper.CreatePolicyAsync();
                await policySummaryPage.ClickOnSpecificLink(NavigationType.Quote);
                return policyNumber;
            });

            var (policySummaryPage, secondPolicyNumber) = await _logger.ExecuteStepAsync("Create renewal policy", async () =>
            {
                var (policySummaryPage, secondPolicyNumber) = await AdbxHelper.CreateRenewalPolicyAsync(policyNumber);
                return (policySummaryPage, secondPolicyNumber);
            });

            await _logger.ExecuteStepAsync("Verify initial and renewal policies exist", async () =>
            {
                await AssertPoliciesExistAsync(friendlyId, policyNumber, secondPolicyNumber, policySummaryPage);
            });

            var newQuoteNumber = await _logger.ExecuteStepAsync("Requote renewal policy and validate status", async () =>
            {
                await AdbxHelper.RequoteAsync(secondPolicyNumber);
                var expectedStatus = "Info Required";
                _logger.Info($"Checking database status after click on requote for policy - # {secondPolicyNumber}");
                var policyBinderId = await _mainQueries.Policy.GetPolicyBinderIdByExPolicyNumberAsync(secondPolicyNumber);

                var details = await _mainQueries.RenewalsLogic.GetRenewalsRequoteDetailsWithRetryAsync(
                    _pollyRetry,
                    policyBinderId,
                    expectedStatus
                );

                var newQuoteNumber = details?.NewQuoteId.ToString();
                _logger.Info($"New Quote Id after requote is - {newQuoteNumber}");
                await _pageHelper!.InteractWithField(RenewalsRefreshIcon);
                var renewalsPage = PageFactory.CreatePage<ADBX_RenewalsTabPage>();
                await renewalsPage.SearchRecentRecords(secondPolicyNumber);
                var status = await _pageHelper!.GetTableCellValueAsync("policyNumber", secondPolicyNumber, "renewalStatus");
                Assert.That(status, Is.EqualTo(expectedStatus), $"Policy with number {secondPolicyNumber} renewal status should be '{expectedStatus}'");
                return newQuoteNumber;
            });

            await _logger.ExecuteStepAsync($"Navigate to new quote {newQuoteNumber} from renewals and validate directing to correct page", async () =>
            {
                _logger.Info($"Navigating to the new quote {newQuoteNumber} from Renewals tab via clicking on status link");
                await _pageHelper!.ClickTableCellButtonAsync("policyNumber", secondPolicyNumber, "renewalStatus");
                var quoteSummaryPage = PageFactory.CreatePage<ADBX_QuoteSummaryPage>();
                var currentUrl = BrowserManager.GetCurrentTab()?.Url;
                Assert.That(currentUrl, Does.Contain(newQuoteNumber.ToString()));
            });
        }

        [Test]
        [Tenant(Tenant.BOLTAG)]
        [Category("ADBX")]
        [Category("CRM")]
        [Category("Policy")]
        [TestCaseId(225759)]
        [Author(Author.Sandy)]
        [Description("Create renewal policy and check success after requote")]
        public async Task BOLTAG_Renewal_Success()
        {
            var user = TestContextAccessor.CurrentUserCollection.ServiceAgent;
            ScopeContext.Set(ctx => ctx.CurrentUser, user);

            var accountId = TestContextAccessor.GetTestSpecificValue("RenewalsAccountExternalId");
            var requestData = new ApplicationRequestModel<PersonalLineData>
            {
                Products = ApplicationTestData.Products.Homeowners,
                ApplicantId = accountId,
                Data = PersonalLineDataProvider.GetPersonalHomeData(AddressData.OH)
            };
            requestData.Data!.PLFloorNumber = 2;
            requestData.Data!.PL_NumberOfFloors = 2;
            requestData.Data!.UnderMajorRenovation = false;
            requestData.Data!.PL_AdditionalStructures_Trampoline = false;

            var friendlyId = await _logger.ExecuteStepAsync("Creating a new quote,subimt,getsubmission via GetQuote API.", async () =>
            {
                var apiHelper = new GetQuoteApiHelper(_getQuoteApi, ScopeContext);
                var submissionResponse = await apiHelper.CreateAndSubmitApplicationWithPollingAsync(requestData);
                return submissionResponse.FriendlyId;
            });

            await AdbxHelper.LoginAndNavigateToQuoteAsync(user,
                ScopeContext.Data.UrlDataCollection.FrontEnd.LoginUrl, friendlyId);

            var policyNumber = await _logger.ExecuteStepAsync($"Create initial policy", async () =>
            {
                var (policySummaryPage, policyNumber) = await AdbxHelper.CreatePolicyAsync();
                await policySummaryPage.ClickOnSpecificLink(NavigationType.Quote);
                return policyNumber;
            });

            var (policySummaryPage, secondPolicyNumber) = await _logger.ExecuteStepAsync("Create renewal policy", async () =>
            {
                var (policySummaryPage, secondPolicyNumber) = await AdbxHelper.CreateRenewalPolicyAsync(policyNumber);
                return (policySummaryPage, secondPolicyNumber);
            });

            await _logger.ExecuteStepAsync("Verify initial and renewal policies exist", async () =>
            {
                await AssertPoliciesExistAsync(friendlyId, policyNumber, secondPolicyNumber, policySummaryPage);
            });

            var newQuoteNumber = await _logger.ExecuteStepAsync("Requote renewal policy and validate status", async () =>
            {
                await AdbxHelper.RequoteAsync(secondPolicyNumber);
                var expectedStatus = "Success";
                _logger.Info($"Checking database status after click on requote for policy - # {secondPolicyNumber}");
                var policyBinderId = await _mainQueries.Policy.GetPolicyBinderIdByExPolicyNumberAsync(secondPolicyNumber);

                var details = await _mainQueries.RenewalsLogic.GetRenewalsRequoteDetailsWithRetryAsync(
                    _pollyRetry,
                    policyBinderId,
                    expectedStatus
                );

                var newQuoteNumber = details?.NewQuoteId.ToString();
                _logger.Info($"New Quote Id after requote is - {newQuoteNumber}");
                await _pageHelper!.InteractWithField(RenewalsRefreshIcon);
                var renewalsPage = PageFactory.CreatePage<ADBX_RenewalsTabPage>();
                await renewalsPage.SearchRecentRecords(secondPolicyNumber);
                var status = await _pageHelper!.GetTableCellValueAsync("policyNumber", secondPolicyNumber, "renewalStatus");
                Assert.That(status, Is.EqualTo(expectedStatus), $"Policy with number {secondPolicyNumber} renewal status should be '{expectedStatus}'");
                return newQuoteNumber;
            });

            var quoteSummaryPage = await _logger.ExecuteStepAsync($"Navigate to new quote {newQuoteNumber} from renewals and validate directing to correct page", async () =>
            {
                await _pageHelper!.ClickTableCellButtonAsync("policyNumber", secondPolicyNumber, "renewalStatus");
                var quoteSummaryPage = PageFactory.CreatePage<ADBX_QuoteSummaryPage>();
                var currentUrl = BrowserManager.GetCurrentTab()?.Url;
                Assert.That(currentUrl, Does.Contain(newQuoteNumber.ToString()));
                return quoteSummaryPage;
            });

            await _logger.ExecuteStepAsync($"Validating timeline submission note", async () =>
            {
                string subject = "Online Submission";
                var isNoteExists = await quoteSummaryPage.IsTimelineNoteExistsADBX(subject,60000);
                Assert.That(isNoteExists, Is.True,
                    $"Failed. note with subject '{subject}' is not found");
            });
        }

        [Test]
        [Tenant(Tenant.BOLTAG)]
        [Category("ADBX")]
        [Category("CRM")]
        [Category("Policy")]
        [TestCaseId(233810)]
        [Author(Author.Andrii)]
        [Description("Create new policy via Lead page")]
        public async Task BOLTAG_Create_Policy_From_Lead_Page()
        {
            var user = TestContextAccessor.CurrentUserCollection.ServiceAgent;
            ScopeContext.Set(ctx => ctx.CurrentUser, user);

            var policyFormData = new Dictionary<string, string>
            {
                [PolicyNumber] = "CreateTestLead" + RandomManager.GetRandomString(4) + RandomManager.GetRandomDigits(4),
                [PolicyProduct] = "Homeowners",
                [PolicyCarrier] = "Bamboo",
                [PolicyPremium] = "100",
            };

            _logger.Info($"Policy number is set to {policyFormData[PolicyNumber]}");

            var requestData = new ApplicationRequestModel<PersonalLineData>
            {
                Products = ApplicationTestData.Products.Homeowners,
                Data = PersonalLineDataProvider.GetPersonalHomeData(AddressData.OH)
            };
            requestData.Data!.PLFloorNumber = 2;
            requestData.Data!.PL_NumberOfFloors = 2;
            requestData.Data!.UnderMajorRenovation = false;
            requestData.Data!.PL_AdditionalStructures_Trampoline = false;

            var friendlyId = await _logger.ExecuteStepAsync("Creating a new quote,subimt,getsubmission via GetQuote API.", async () =>
            {
                var apiHelper = new GetQuoteApiHelper(_getQuoteApi, ScopeContext);
                var submissionResponse = await apiHelper.CreateAndSubmitApplicationWithPollingAsync(requestData);
                return submissionResponse.FriendlyId;
            });

            await AdbxHelper.LoginAndNavigateToQuoteAsync(user,
                ScopeContext.Data.UrlDataCollection.FrontEnd.LoginUrl, friendlyId);

            var leadSummaryPage = await _logger.ExecuteStepAsync("Navigate to the linked lead", async () =>
            {
                var quotePage = PageFactory.CreatePage<ADBX_QuoteSummaryPage>();
                return await quotePage.ClickOnLinkedLead();
            }, "Expected result: Lead Summary page is displayed");

            await _logger.ExecuteStepAsync("Add a new policy from the Lead page", async () =>
            {
                var addPolicyPopUp = await leadSummaryPage.ClickOnAddPolicy();
                await addPolicyPopUp.FillForm(policyFormData);
                await addPolicyPopUp.ClickPopupConfirm();
            }, "Expected result: Policy is created and shown under Latest Policies");

            await _pageHelper.RefreshPageAsync();

            await _logger.ExecuteStepAsync("Verify the created policy appears under Latest Policies", async () =>
            {
                var expectedPremium = $"${policyFormData[PolicyPremium]}.00";

                var policies = await leadSummaryPage.GetLatestPolicies();

                Assert.That(policies, Has.Count.EqualTo(1),
                    $"Expected exactly one policy under Latest Policies. Actual: {policies.Count}, expected :1");

                Assert.Multiple(() =>
                {
                    Assert.That(policies[0].PolicyNumber, Is.EqualTo(policyFormData[PolicyNumber]),
                        $"Created policy number should match the submitted policy number. Actual: {policies[0].PolicyNumber}, expected :{policyFormData[PolicyNumber]}");
                    Assert.That(policies[0].Product, Is.EqualTo(policyFormData[PolicyProduct]),
                        $"Created policy product should match the submitted product. Actual: {policies[0].Product}, expected :{policyFormData[PolicyProduct]}");
                    Assert.That(policies[0].Carrier, Is.EqualTo(policyFormData[PolicyCarrier]),
                        $"Created policy carrier should match the submitted carrier. Actual: {policies[0].Carrier}, expected :{policyFormData[PolicyCarrier]}");
                    Assert.That(policies[0].Premium, Is.EqualTo(expectedPremium),
                        $"Created policy premium should match the submitted premium. Actual: {policies[0].Premium}, expected :{expectedPremium}");
                });
            });

            await _logger.ExecuteStepAsync("Navigate to Latest Policies and verify the created policy appears on Lead Policies grid", async () =>
            {
                await leadSummaryPage.ClickSeeAllLatest("Policies");
                var leadPoliciesPage = PageFactory.CreatePage<ADBX_LeadPoliciesPage>();

                var policyDetails = await _pageHelper.GetRowData(1);

                var expectedEffectiveDate = DateTime.Today.ToString("MM/dd/yyyy") + "\u00A0ET";
                var expectedExpirationDate = DateTime.Today.AddYears(1).ToString("MM/dd/yyyy") + "\u00A0ET";
                var expectedDateCreated = DateTime.Today.ToString("MM/dd/yyyy") + "\u00A0ET";

                var expectedPremium = $"${policyFormData[PolicyPremium]}.00";

                Assert.Multiple(() =>
                {
                    Assert.That(policyDetails["Policy Number"], Is.EqualTo(policyFormData[PolicyNumber]),
                        $"Policy Number column should match the submitted policy number. Actual: {policyDetails["Policy Number"]}, expected :{policyFormData[PolicyNumber]}");
                    Assert.That(policyDetails["Carrier"], Is.EqualTo(policyFormData[PolicyCarrier]),
                        $"Carrier column should match the submitted carrier. Actual: {policyDetails["Carrier"]}, expected :{policyFormData[PolicyCarrier]}");
                    Assert.That(policyDetails["Premium"], Is.EqualTo(expectedPremium),
                        $"Premium column should match the submitted premium. Actual: {policyDetails["Premium"]}, expected :{expectedPremium}");
                    Assert.That(policyDetails["Effective Date"], Is.EqualTo(expectedEffectiveDate),
                        $"Effective Date column should be today's date. Actual: {policyDetails["Effective Date"]}, expected :{expectedEffectiveDate}");
                    Assert.That(policyDetails["Expiration Date"], Is.EqualTo(expectedExpirationDate),
                        $"Expiration Date column should be one year from today. Actual: {policyDetails["Expiration Date"]}, expected :{expectedExpirationDate}");
                    Assert.That(policyDetails["Product"], Is.EqualTo(policyFormData[PolicyProduct]),
                        $"Product column should match the submitted product. Actual: {policyDetails["Product"]}, expected :{policyFormData[PolicyProduct]}");
                    Assert.That(policyDetails["Status"], Is.EqualTo("Active"),
                        $"Status column should be 'Active'. Actual: {policyDetails["Status"]}, expected :Active");
                    Assert.That(policyDetails["Date Created"], Is.EqualTo(expectedDateCreated),
                        $"Date Created column should be today's date. Actual: {policyDetails["Date Created"]}, expected :{expectedDateCreated}");
                });
            });
        }

        [Test]
        [Tenant(Tenant.BOLTAG)]
        [Category("ADBX")]
        [Category("CRM")]
        [Category("Policy")]
        [TestCaseId(233776)]
        [Author(Author.Andrii)]
        [Description("Create new policy via Quote page with Sold Note")]
        public async Task BOLTAG_Create_Policy_From_Quote_Page_Sold_Note()
        {
            var user = TestContextAccessor.CurrentUserCollection.ServiceAgent;
            var userFullName = $"{user.FirstName} {user.LastName}";
            ScopeContext.Set(ctx => ctx.CurrentUser, user);
            var policyFormData = new Dictionary<string, string>
            {
                [PolicyProduct] = Environment == Common.Environment.Qa ? "Package" : "Personal Package",
                [PolicyCarrier] = Environment == Common.Environment.Qa ? "Hartford" : "National General Custom 360",
                [PolicyTerm] = "6 months",
                [PolicyNumber] = "CreateTestNote" + RandomManager.GetRandomString(3) + RandomManager.GetRandomDigits(4),
                [PolicyPremium] = "100",
                [PolicyPackageProducts] = "Homeowners|Personal Auto",
            };
            _logger.Info($"Policy number is set to {policyFormData[PolicyNumber]}");
            var requestData = new ApplicationRequestModel<PersonalLineData>
            {
                Products = ApplicationTestData.Products.Homeowners,
                Data = PersonalLineDataProvider.GetPersonalHomeData(AddressData.OH)
            };

            var friendlyId = await _logger.ExecuteStepAsync("Create a new quote via GetQuote API", async () =>
            {
                var createResponse = await _getQuoteApi.CreateApplicationAsync(requestData);
                return createResponse.Content.FriendlyId;
            });

            await AdbxHelper.LoginAndNavigateToQuoteAsync(user,
                ScopeContext.Data.UrlDataCollection.FrontEnd.LoginUrl, friendlyId);
            var quotePage = PageFactory.CreatePage<ADBX_QuoteSummaryPage>();
            if (await quotePage.IsUpdateAccountInformationPopUpExists())
            {
                var updateAccountPopup = PageFactory.CreatePage<ADBX_EnterUpdateAccountInformationPopup>();
                await updateAccountPopup.ClosePopup();
            }

            await _logger.ExecuteStepAsync("Create new note", async () =>
            {
                var addNotePopup =  await quotePage.ClickOnNewNote();
                var formData = new Dictionary<string, string>
                {
                    [NoteSubject] = "Sold"
                };
                await addNotePopup.FillForm(formData);
                await addNotePopup.ClickPopupAdd();
            });

            await _logger.ExecuteStepAsync("Create new policy", async () =>
            {

                var addPolicyPopup = PageFactory.CreatePage<ADBX_AddPolicyInformationPopUp>();
                await addPolicyPopup.FillForm(policyFormData);
                await _pageHelper!.InteractWithField(PolicyTerm);
                await addPolicyPopup.ClickPopupConfirm();
            });

            await _logger.ExecuteStepAsync("Check Policy details", async () =>
            {
                var expectedEffectiveDate = DateTime.Today.ToString("MM/dd/yyyy");
                var expectedExpirationDate = DateTime.Today.AddMonths(6).ToString("MM/dd/yyyy");
                var expectedPremium = $"${policyFormData[PolicyPremium]}.00";

                var policyPage = PageFactory.CreatePage<ADBX_PolicySummaryPage>();
                var policyDetails = await policyPage.GetSummaryData();
                var actulaPolicyNumber = await _pageHelper.GetFieldValue(FieldRegistryADBX.Fields[PageTitle]);

                var actualPolicyNumber = actulaPolicyNumber.Replace("Policy ", "");

                Assert.Multiple(() =>
                {
                    Assert.That(actualPolicyNumber, Is.EqualTo(policyFormData[PolicyNumber]),
                        $"Displayed policy number should match the submitted policy number. Actual: {actualPolicyNumber}, expected :{policyFormData[PolicyNumber]}");
                    Assert.That(policyDetails["Product"], Is.EqualTo(policyFormData[PolicyProduct]),
                        $"Policy product should match the submitted product. Actual: {policyDetails["Product"]}, expected :{policyFormData[PolicyProduct]}");
                    Assert.That(policyDetails["ProductPackages"], Is.EqualTo("Homeowners, Personal Auto"),
                        $"Policy should list both package products. Actual: {policyDetails["ProductPackages"]}, expected :Homeowners, Personal Auto");
                    Assert.That(policyDetails["Carrier"], Is.EqualTo(policyFormData[PolicyCarrier]),
                        $"Policy carrier should match the submitted carrier. Actual: {policyDetails["Carrier"]}, expected :{policyFormData[PolicyCarrier]}");
                    Assert.That(policyDetails["EffectiveDate"], Is.EqualTo(expectedEffectiveDate),
                        $"Effective date should be today's date. Actual: {policyDetails["EffectiveDate"]}, expected :{expectedEffectiveDate}");
                    Assert.That(policyDetails["ExpirationDate"], Is.EqualTo(expectedExpirationDate),
                        $"Expiration date should be 6 months from today. Actual: {policyDetails["ExpirationDate"]}, expected :{expectedExpirationDate}");
                    Assert.That(policyDetails["Premium"], Is.EqualTo(expectedPremium),
                        $"Policy premium should match the submitted premium. Actual: {policyDetails["Premium"]}, expected :{expectedPremium}");
                    Assert.That(policyDetails["Status"], Is.EqualTo("Active"),
                        $"Policy status should be 'Active'. Actual: {policyDetails["Status"]}, expected :Active");
                    Assert.That(policyDetails["AssignedTo"], Is.EqualTo(userFullName),
                        $"Policy should be assigned to the current user. Actual: {policyDetails["AssignedTo"]}, expected :{userFullName}");
                });
            });
        }
    }

}
