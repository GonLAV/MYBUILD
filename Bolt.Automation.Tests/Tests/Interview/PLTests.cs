using System.Net;
using Bolt.Automation.ApiClients.GetQuoteApi.Extensions;
using Bolt.Automation.ApiClients.GetQuoteApi.Interfaces;
using Bolt.Automation.ApiClients.GetQuoteApi.Models.Application;
using Bolt.Automation.ApiClients.Infrastructure;
using Bolt.Automation.ApiClients.SSO;
using Bolt.Automation.Common;
using Bolt.Automation.Common.Enums;
using Bolt.Automation.Common.Exceptions;
using Bolt.Automation.Common.Logging.Extentions;
using Bolt.Automation.Common.Models.RelayStates;
using Bolt.Automation.Common.Models.TestData.Data;
using Bolt.Automation.Common.Models.TestData.DataModels.PersonalLine;
using Bolt.Automation.Common.Utils;
using Bolt.Automation.FrontEnds.CommonHelpers;
using Bolt.Automation.FrontEnds.Executor;
using Bolt.Automation.FrontEnds.Executor.Helpers;
using Bolt.Automation.FrontEnds.Projects.ADBX.Pages;
using Bolt.Automation.FrontEnds.Projects.ADBX.Pages.Quotes;
using Bolt.Automation.FrontEnds.Projects.Interview.FormData;
using Bolt.Automation.FrontEnds.Projects.Interview.Pages;
using Bolt.Automation.InternalServices.Database.Queries.Main;
using Bolt.Automation.TestDataProvider.Providers.GetQuoteApiDataProvider;
using Bolt.Automation.TestDataProvider.TestData.CommonTestData;
using Bolt.Automation.Tests.TestExtension.Attributes;
using Bolt.Automation.Tests.TestExtension.Base;
using Bolt.Automation.Tests.TestHelpers;
using Bolt.Automation.Tests.TestHelpers.ADBX;
using Bolt.Automation.Tests.TestHelpers.Interview;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using static Bolt.Automation.Common.Tenant;
using static Bolt.Automation.TestDataProvider.TestData.ApplicationTestData.ApplicationTestData;
using ADBX_FieldNames = Bolt.Automation.FrontEnds.Projects.ADBX.FormData.ADBX_FieldNames;
using AuthorAttribute = Bolt.Automation.Tests.TestExtension.Attributes.AuthorAttribute;
using BoltEnvironment = Bolt.Automation.Common.Environment;
using FlowType = Bolt.Automation.FrontEnds.Projects.Interview.Flows.FlowType;

namespace Bolt.Automation.Tests.Tests.Interview
{
    public class PLTests : UITestBase
    {
        // The success destination deliberately does not resolve, so the browser never lands there -
        // the test checks that the provider asked for it, carrying the right transaction id.
        private const string SuccessRedirectHost = "successlinktest.com";
        private const string SuccessRedirectUrl = "https://www." + SuccessRedirectHost;

        protected IGetQuoteApi? _getQuoteApi;
        protected IMainQueries? _mainQueries;
        protected ISsoApiFactory _ssoApiFactory = null!;
        public FrontEndType? FrontEnd { get; set; }
        protected InterviewTestHelper _interviewHelper = null!;
        private AdbxTestHelper _adbxHelper = null!;

        public PLTests() : base()
        {
            ScopeContext.Set(ctx => ctx.FrontEnd, FrontEndType.Interview);
        }
        protected override void ResolveServices()
        {
            var refitApiLocator = _uiTestScope.ServiceProvider.GetRequiredService<RefitApiServiceLocator>();
            _getQuoteApi = refitApiLocator.GetService<IGetQuoteApi>();
            _mainQueries = _testScope.ServiceProvider.GetService<IMainQueries>();
            _ssoApiFactory = _testScope.ServiceProvider.GetRequiredService<ISsoApiFactory>();
        }
        protected override void InitializeComponents()
        {
            _interviewHelper = new InterviewTestHelper(_logger, BrowserManager, PageFactory, _pageHelper!, ScopeContext);
            _adbxHelper = new AdbxTestHelper(_logger, BrowserManager, PageFactory, _pageHelper!, ScopeContext);
        }

        [Test]
        [Tenant(UNIFY)]
        [Category("Interview")][Category("Quoting")]
        [TestCaseId(229271)]
        [Author(Author.Gil)]
        [Description("Verifies PL Homeowners quote flow with CA addresses validates Bamboo admitted and notadmitted carriers on markets and submission pages")]
        public async Task UNIFY_PL_Homeowners_BambooCarriers_E2E()
        {
            await _logger.ExecuteStepAsync("Setup test user", async () =>
            {
                var user = TestContextAccessor.CurrentUserCollection.MarketLibAgent;
                ScopeContext.Set(ctx => ctx.CurrentUser, user);
            });

            await _logger.ExecuteStepAsync("Create application via GetQuote API with CA Address 1 (Saint Ann Ave)", async () =>
            {
                var customAddress = new Address
                {
                    AddressLine1 = "5562 Saint Ann Ave",
                    City = "Cypress",
                    State = "CA",
                    ZipCode = "90630"
                };

                var data = PersonalLineDataProvider.GetPersonalHomeData(
                    address: customAddress,
                    homeDetails: HomeDetailsTestData.PersonalHomeDetailsData,
                    homeFeatures: HomeFeaturesTestData.PersonalHomeFeaturesData,
                    policyDetails: PolicyTestData.PersonalHomePolicyDetails
                );

                // Override values directly on the data object
                data.UnderMajorRenovation = false;
                data.UndergroundFuelTank = false;

                var requestData = new ApplicationRequestModel<PersonalLineData>
                {
                    Products = Products.Homeowners,
                    Data = data
                };

                var apiHelper = new GetQuoteApiHelper(_getQuoteApi, ScopeContext);
                var getQuestionnaireResp = await apiHelper.CreateApplication_GetQuestionnaire(requestData);
                await BrowserManager.OpenNewWindowAsync(getQuestionnaireResp.Url);
            });

            await _logger.ExecuteStepAsync("Navigate through Start and Market pages", async () =>
            {
                var startPage = PageFactory.CreatePage<Product_StartPage>();
                await startPage.ClickContinue();

                var marketsPage = PageFactory.CreatePage<Product_MarketsPage>();

                await _logger.ExecuteStepAsync("Verify Bamboo carriers are visible on Market page", async () =>
                {
                    var bambooVisible = await _interviewHelper.IsCarrierVisible("Bamboo");
                    Assert.That(bambooVisible, Is.True, "Bamboo should be visible");

                    var bambooESVisible = await _interviewHelper.IsCarrierVisible("Bamboo  E&S");
                    Assert.That(bambooESVisible, Is.True, "Bamboo E&S should be visible");
                });

                await marketsPage.ClickContinue();
            });

            await _logger.ExecuteStepAsync("Complete HO3 Homeowners Interview Flow - CA Address 1", async () =>
            {
                var resultsPage = await Executor.ExecuteToPage<Product_ResultsPage>(
                    FlowType.InterviewHO3Flow,
                    currentPage: PageFactory.CreatePage<Product_HomePage>(),
                    new Dictionary<string, string>
                    {
                        [FieldNames.PLAllPerilsDeductible] = "$2,500",
                    },
                    fillForms: true
                );

                await _logger.ExecuteStepAsync("Verify Non-Admitted carriers for CA Address 1", async () =>
                {
                    await resultsPage.ClickMarketCategoryTab("Non-Admitted");

                    var foundCarriers = await resultsPage.WaitForCarrierToAppear("Bamboo", timeoutSeconds: 120, pollIntervalMs: 2000);
                    _logger.LogDataValidation("Bamboo Carrier Found", foundCarriers, "true", foundCarriers.ToString(),
                        "Bamboo carrier should appear in Non-Admitted tab");
                    Assert.That(foundCarriers, Is.True, "Bamboo carrier should be found in Non-Admitted tab");

                    var nonAdmittedCarriers = await resultsPage.GetCarriersInCurrentTab();
                    _logger.Info($"Non-Admitted carriers found: {string.Join(", ", nonAdmittedCarriers)}");

                    Assert.That(nonAdmittedCarriers.Any(c => c.Contains("Bamboo", StringComparison.OrdinalIgnoreCase)),
                        Is.True, "Bamboo should be present in Non-Admitted carriers");
                });
            });

            await _logger.ExecuteStepAsync("Change to CA Address 2 (Arnold Way) and verify Admitted carriers", async () =>
            {
                await _logger.ExecuteStepAsync("Navigate to Start page and change address", async () =>
                {
                    await _interviewHelper.ClickProgressBarStep("Start");
                    var startPage = PageFactory.CreatePage<Product_StartPage>();
                    await _pageHelper.InteractWithField(FieldNames.InterviewAddress, "1750 Arnold Way, Alpine, CA 91901");
                });

                await _logger.ExecuteStepAsync("Submit for new quote with CA Address 2", async () =>
                {
                    var startPage = PageFactory.CreatePage<Product_StartPage>();
                    var resultsPage = await startPage.ClickSubmitForQuoteButton();

                    await _logger.ExecuteStepAsync("Verify Admitted carriers for CA Address 2", async () =>
                    {
                        // Wait for quotes to complete and carriers to load
                        var foundCarriers = await resultsPage.WaitForCarrierToAppear("Bamboo", timeoutSeconds: 120, pollIntervalMs: 2000);
                        _logger.LogDataValidation("Bamboo Carrier Found", foundCarriers, "true", foundCarriers.ToString(),
                            "Bamboo carrier should appear in Admitted tab");
                        Assert.That(foundCarriers, Is.True, "Bamboo carrier should be found in Admitted tab");

                        var admittedCarriers = await resultsPage.GetCarriersInCurrentTab();
                        _logger.Info($"Admitted carriers found: {string.Join(", ", admittedCarriers)}");

                        Assert.That(admittedCarriers.Any(c => c.Contains("Bamboo", StringComparison.OrdinalIgnoreCase)),
                            Is.True, "Bamboo should be present in Admitted carriers");
                    });
                });
            });
        }

        [Test]
        [Tenant(UNIFY)]
        [Category("Interview")][Category("Quoting")][Category("Payment")][Category("FullQuote")][Category("Sanity")]
        [Author(Author.Gil)]
        [TestCaseId(123432)]
        [RunIn(includeStaging: true)]
        [Description("Verifies PL Renters (HO4) full quote and payment E2E flow using the GetQuote API, confirms a successful Lemonade quote is returned, validates payment plan details, completes payment via redirect, and verifies the resulting transaction and policy document")]
        public async Task UNIFY_PL_Renters_FQ_Payment_E2E()
        {
            ScopeContext.Set(ctx => ctx.CurrentUser, TestContextAccessor.CurrentUserCollection.MarketLibAgent);

            var apiHelper = new GetQuoteApiHelper(_getQuoteApi, ScopeContext);
            var data = PersonalLineDataProvider.GetPersonalHomeData(address: AddressData.IN);
            var submissionResponse = await apiHelper.CreateAndSubmitApplicationWithPollingAsync(new ApplicationRequestModel<PersonalLineData>
            {
                Products = Products.Renters,
                Data = data
            });

            Assert.That(
                submissionResponse.Quotes?.Any(x => x.Status == "Success" && x.Carrier == "Lemonade"), Is.True,
                $"Failed, no successful Lemonade quote returned [{JsonConvert.SerializeObject(submissionResponse.Quotes)}]");

            var lemonadeQuote = submissionResponse.Quotes!.First(x => x.Status == "Success" && x.Carrier == "Lemonade");
            var firstPaymentPlan = lemonadeQuote.PaymentPlans!.First(x => x.NumberOfPayments > 1);
            var successQuoteId = lemonadeQuote.Id!;
            var paymentId = firstPaymentPlan.Id;
            var totalAmount = (int)firstPaymentPlan.TotalAmount;
            var firstPaymentAmount = (int)firstPaymentPlan.FirstPaymentAmount;
            firstPaymentPlan.AdditionalData.TryGetValue("effectiveDate", out var effectiveDate);
            firstPaymentPlan.AdditionalData.TryGetValue("expirationDate", out var expirationDate);

            await _logger.ExecuteStepAsync("Validate and select payment plan, then get redirect URL", async () =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(totalAmount, Is.GreaterThan(0), $"Failed, expected TotalAmount > 0, actual [{totalAmount}]");
                    Assert.That(firstPaymentAmount, Is.GreaterThan(0), $"Failed, expected FirstPaymentAmount > 0, actual [{firstPaymentAmount}]");
                    Assert.That(effectiveDate, Is.Not.Null.And.Not.Empty, "Failed, effectiveDate is missing from AdditionalData");
                    Assert.That(expirationDate, Is.Not.Null.And.Not.Empty, "Failed, expirationDate is missing from AdditionalData");
                });

                _logger.Info($"Quote ID: [{successQuoteId}], Payment ID: [{paymentId}], TotalAmount: [{totalAmount}], " +
                             $"FirstPaymentAmount: [{firstPaymentAmount}], EffectiveDate: [{effectiveDate}], ExpirationDate: [{expirationDate}]");

                var selectResp = await _getQuoteApi!.SelectQuotePaymentPlanAsync(successQuoteId, paymentId);
                Assert.That(selectResp.IsSuccessStatusCode, Is.True,
                    $"Failed, expected SelectQuotePaymentPlan to return 204/NoContent, but actual: {selectResp.StatusCode}");
            });

            string redUrl = null!;
            string transactionId = null!;
            await _logger.ExecuteStepAsync("Redirect payment and extract transaction details", async () =>
            {
                var redirectResp = await _getQuoteApi!.PayQuotePaymentPlanAsync(
                    successQuoteId, paymentId,
                    new { successUrl = SuccessRedirectUrl, failureUrl = "https://example-fail.com" }).EnsureSuccessContentAsync();

                redUrl = redirectResp.Url!;
                transactionId = redirectResp.TransactionId!;

                Assert.That(transactionId, Is.Not.Null.And.Not.Empty, "Failed, transactionId is null or empty");
                _logger.Info($"Redirect URL: [{redUrl}], Transaction ID: [{transactionId}]");
            });

            await _logger.ExecuteStepAsync("Navigate to payment page, fill form and complete payment", async () =>
            {
                await BrowserManager.NavigateAsync(redUrl);
                var paymentPage = PageFactory.CreatePage<Product_PaymentIntegrationPage>();

                Assert.Multiple(async () =>
                {
                    Assert.That(await paymentPage.IsTotalAmountDisplayed(totalAmount.ToString()), Is.True,
                        $"Failed, total amount [{totalAmount}] not found on payment page");
                    Assert.That(await paymentPage.IsPayNowDisplayed(firstPaymentAmount.ToString()), Is.True,
                        $"Failed, pay now amount [{firstPaymentAmount}] not found on payment page");
                    Assert.That(await paymentPage.IsCompleteOrderBtnEnabled(), Is.EqualTo(false),
                        "Failed, Complete Order button should be disabled before any data is entered");
                });

                await paymentPage.SetCardNumber("4000056655665556");
                await paymentPage.SetExpirationDate("08/28");
                await paymentPage.SetCVC("1234");

                Assert.That(await paymentPage.IsCompleteOrderBtnEnabled(), Is.EqualTo(false),
                    "Failed, without checkboxes Complete Order button should be disabled");

                await paymentPage.SelectCheckBoxTermsService();
                await paymentPage.SelectCheckBoxGiveback();
                var redirectUrl = await paymentPage.ClickNextAndGetRedirectUrlAsync(SuccessRedirectHost);

                Assert.Multiple(() =>
                {
                    Assert.That(redirectUrl, Does.StartWith(SuccessRedirectUrl).IgnoreCase,
                        $"Failed, expected payment to redirect to [{SuccessRedirectUrl}], actual [{redirectUrl}]");
                    Assert.That(redirectUrl, Does.Contain($"transactionId={transactionId}").IgnoreCase,
                        $"Failed, expected the redirect to carry transactionId [{transactionId}], actual [{redirectUrl}]");
                });
            });

            await _logger.ExecuteStepAsync("Verify payment transaction details", async () =>
            {
                var getTransactionResponse = await _getQuoteApi!.GetPaymentTransactionAsync(transactionId).EnsureSuccessContentAsync();
                var additionalData = getTransactionResponse.AdditionalData!;

                Assert.Multiple(() =>
                {
                    Assert.That(getTransactionResponse.Status, Is.EqualTo("Success"));
                    Assert.That(getTransactionResponse.Amount, Is.EqualTo(firstPaymentAmount));
                    Assert.That(additionalData.GetValueOrDefault("effectiveDate"), Is.EqualTo(effectiveDate));
                    Assert.That(additionalData.GetValueOrDefault("expirationDate"), Is.EqualTo(expirationDate));
                    Assert.That(additionalData.GetValueOrDefault("totalAmount"), Does.Contain(totalAmount.ToString()));
                    Assert.That(additionalData.GetValueOrDefault("policyNumber"), Is.Not.Null.And.Not.Empty);
                });

                var policyDocument = additionalData["policyDocument"]!;
                var downloadHelper = new FileDownloadValidatorHelper(BrowserManager.GetCurrentTab()!, _logger);
                var document = await downloadHelper.ValidateFileDownloadFromUrl(policyDocument);

                Assert.That(document.FileSizeBytes, Is.GreaterThan(0),
                    $"Failed, policy document [{document.FileName}] downloaded empty from [{policyDocument}]");
            });
        }

//This test should be updated by QA so i am commenting for now.
// When it is revived: use FlowType.InterviewPLAgentFlow, which already is Start -> Markets -> Home,
// instead of the pagesToSkip/pagesToAdd pair below. The DistanceToFireHydrant and ArchitectureStyle
// overrides are also redundant now - both registry defaults were corrected to valid options.
        // [Test]
        // [Tenant(KRAFTLAKEX)]
        // [Category("Interview")][Category("Quoting")]
        // [RunIn(includeStaging: true)]
        // [Author(Author.Sandy)]
        // [TestCaseId(235615)]
        // [Description("Verify Acord80 download via application forms both for agent and uw")]
        // public async Task KLX_HO3_Application_Forms_Acord_Download()
        // {
        //     var user = TestContextAccessor.CurrentUserCollection.LSP1
        //         ?? throw new TestSetupException("KLX LSP1 user not configured");
        //     ScopeContext.Set(ctx => ctx.CurrentUser, user);

        //     var loginUrl = ScopeContext.Data.UrlDataCollection.FrontEnd?.LoginUrl
        //         ?? throw new TestSetupException("KLX LoginUrl not configured");
        //     await _adbxHelper.LoginAsync(user, loginUrl);
        //     ScopeContext.Set(ctx => ctx.FrontEnd, FrontEndType.ADBX);

        //     await _logger.ExecuteStepAsync("Create new quote from existing account", async () =>
        //     {
        //         var homePage = PageFactory.CreatePage<ADBX_HomePage>();
        //         var accountsPage = await _adbxHelper.NavigateToMenuAsync<ADBX_AccountsTabPage>(homePage, NavigationType.Accounts);
        //         await _pageHelper!.SelectTableRowAsync(1);
        //         var accountSummaryPage = PageFactory.CreatePage<ADBX_AccountSummaryPage>();
        //         await accountSummaryPage.ClickOnNewQuoteFromExistingAccount();
        //     });

        //     var resultsPage = await _logger.ExecuteStepAsync("Execute HO3 Home flow to Results page", async () =>
        //     {
        //         return await Executor.ExecuteToPage<Product_ResultsPage>(
        //             FlowType.InterviewHO3Flow,
        //             PageFactory.CreatePage<Product_StartPage>(),
        //             new Dictionary<string, string>
        //             {
        //                 [FieldNames.DistanceToFireHydrant] = "0-500 Feet",
        //                 [FieldNames.ArchitectureStyle] = "Colonial",
        //             },
        //             fillForms: true,
        //             pagesToSkip: [typeof(Product_LobsPage)],
        //             pagesToAdd: [new PageInsertion(typeof(Product_MarketsPage), typeof(Product_StartPage))]);
        //     });
        // }

        [Test]
        [Tenant(KRAFTLAKEX)]
        [Category("ADBX")][Category("CRM")][Category("Sanity")]
        [TestCaseId(179749)]
        [RunIn(includeStaging: true)]
        [Author(Author.Andrii)]
        [Description("Create New Personal Auto quote, submit, get questionnaire link to interview and check rates")]
        public async Task KraftLakeX_Auto_E2E()
        {
            var personalInfo = PersonalInfo.GetRandomPersonalInfo();

            var data = PersonalLineDataProvider.GetPersonalAutoData(
                AddressData.CA,
                [Vehicles.Vin_2B3LJ44V59H599429],
                [Drivers.KLXTestDriver],
                personalInfo);

            data.DeclinationReason = "CustomerrejectedFarmersquote";
            data.QuoteNumberSpecification = "45454555";
            data.IsMailAddress = false;
            data.ForeclosureOrRepossessionOrBankruptcy = false;
            data.InsuranceFraud = false;
            data.EffectiveDate = DateTime.Today.AddDays(3).ToString("yyyy-MM-dd");
            data.PriorCarrierExpirationDateAuto = DateTime.Today.AddDays(3).ToString("yyyy-MM-dd");
            var requestData = new ApplicationRequestModel<PersonalLineData>
            {
                Products = Products.PersonalAuto,
                Data = data
            };

            var user = Environment == Common.Environment.Uat
                ? TestContextAccessor.CurrentUserCollection.LSP1
                : TestContextAccessor.CurrentUserCollection.Agent;
            ScopeContext.Set(ctx => ctx.CurrentUser, user);
            
            string applicationExternalId = string.Empty;
            await _logger.ExecuteStepAsync("Create and submit application via GetQuote API", async () =>
            {
                var apiHelper = new GetQuoteApiHelper(_getQuoteApi, ScopeContext);
                var submissionResponse = await apiHelper.CreateAndSubmitApplicationWithPollingAsync(requestData, false);
                applicationExternalId = submissionResponse.ApplicationId;
            });

            await _logger.ExecuteStepAsync("Get link via Questionnaire and navigate to the interview", async () =>
            {
                var getQuestionnaireResp = await _getQuoteApi.GetQuestionnaireAsync(applicationExternalId);
                var url = getQuestionnaireResp.Content.Url;
                await BrowserManager.NavigateAsync(url);
            });

            await _logger.ExecuteStepAsync("Check rates on the Results page", async () =>
            {
                var resultsPage = PageFactory.CreatePage<Product_ResultsPage>();
                var rates = await _interviewHelper.LogRatesBusinessRule(resultsPage);

                Assert.That(rates.Admitted, Is.Not.Empty, "Failed. Should be at least 1 admitted carrier");
            });

        }

        [Test]
        [Tenant(KRAFTLAKEX)]
        [Category("ADBX")][Category("CRM")][Category("Sanity")]
        [TestCaseId(242574)]
        [RunIn(includeStaging: true)]
        [Author(Author.Andrii)]
        [Description("Create an applicant, create a quote for applicant, patch, submit the application, edit quote via dashboard and check rates")]
        public async Task KraftLakeX_Home_E2E()
        {
            var apiHelper = new GetQuoteApiHelper(_getQuoteApi, ScopeContext);
            ScopeContext.Set(ctx => ctx.FrontEnd, FrontEndType.ADBX);
            var user = Environment == Common.Environment.Uat
                ? TestContextAccessor.CurrentUserCollection.LSP1
                : TestContextAccessor.CurrentUserCollection.Agent;
            ScopeContext.Set(ctx => ctx.CurrentUser, user);

            string applicantId = string.Empty;
            string applicationExternalId = string.Empty;
            string friendlyId = string.Empty;
            var effectiveDate = DateTime.Today.AddDays(2).ToString("yyyy-MM-dd");
            var priorCarrierExpirationDate = DateTime.Today.AddDays(2).ToString("yyyy-MM-dd");

            await _logger.ExecuteStepAsync("Create Applicant via GetQuote API", async () =>
            {
                var applicant = TestDataProvider.TestData.ApplicantTestData.ApplicantTestData.Applicants.RandomApplicant;
                var applicantResponse = await _getQuoteApi.CreateApplicantAsync(applicant)
                                              .EnsureSuccessContentAsync();
                applicantId = applicantResponse.Id;
            }, "Expected result: Applicant created successfully");

            await _logger.ExecuteStepAsync("Create Application via GetQuote API", async () =>
            {
                var data = PersonalLineDataProvider.GetPersonalHomeData();
                data.PLFloorNumber = null;
                data.PL_NumberOfFloors = null;
                data.DeclinationReason = "CustomerrejectedFarmersquote";
                data.QuoteNumberSpecification = "45454555";
                data.UnderMajorRenovation = false;
                data.InCitySuburbDistrict = "City";
                data.DistanceToCoast = "500";
                data.UndergroundFuelTank = false;
                data.PriorLiabilityCoverageHome = "Threehundredthousand";
                data.NumOfDwelling = "1";
                data.WindstormDeductible = "OnePercent";
                data.CreditCheckPermission = true;
                data.InsuranceFraud = false;
                data.NumberOfChildernsUnder18 = 0;

                var requestData = new ApplicationRequestModel<PersonalLineData>
                {
                    Products = Products.Homeowners,
                    ApplicantId = applicantId,
                    Data = data
                };
                var applicationResponse = await _getQuoteApi.CreateApplicationAsync(requestData)
                                                .EnsureSuccessContentAsync();
                applicationExternalId = applicationResponse.Id;
                friendlyId = applicationResponse.FriendlyId;
            }, "Expected result: Application created successfully");

            await _logger.ExecuteStepAsync("Update Application via GetQuote API", async () =>
            {
                var updateData = new PersonalLineData
                {
                    EffectiveDate = effectiveDate,
                    PriorCarrierExperationDate = priorCarrierExpirationDate
                };
                var updateRequestData = new ApplicationRequestModel<PersonalLineData>
                {
                    Data = updateData
                };
                var updateResponse = await _getQuoteApi.PatchApplicationAsync(applicationExternalId, updateRequestData)
                                           .EnsureSuccessContentAsync(); ;
            }, $"Expected result: Application updated with EffectiveDate: {effectiveDate} and PriorCarrierExpirationDate: {priorCarrierExpirationDate}");

            await _logger.ExecuteStepAsync("Submit Application via GetQuote API", async () =>
            {
                var submitResponse = await apiHelper.SubmitApplicationWithPollingAsync(applicationExternalId);
            }, "Expected result: Application submitted successfully");

            await _adbxHelper.LoginAsync(
                user,
                TestContextAccessor.CurrentUrlCollection.FrontEnd.LoginUrl);
            await _logger.ExecuteStepAsync("Navigate to KraftlakeX dashboard and search by friendlyId", async () =>
            {
                var homePage = PageFactory.CreatePage<ADBX_HomePage>();
                await homePage.SearchFor(friendlyId);
            }, "Expected result: Navigation to ADBX dashboard completed and search performed");

            await _logger.ExecuteStepAsync("Open quote and click Edit Quote button", async () =>
            {
                await _pageHelper!.SelectTableRowAsync(1);
                var quoteSummaryPage = PageFactory.CreatePage<ADBX_QuoteSummaryPage>();
                await quoteSummaryPage.ClickOnEditQuote();
            }, "Expected result: Quote opened from search results and Edit Quote button clicked");

            var resultPage = PageFactory.CreatePage<Product_ResultsPage>();

            await _logger.ExecuteStepAsync("Go back to Applicant page and edit phone", async () =>
            {
                await resultPage.ClickBack();
                var applicantPage = PageFactory.CreatePage<Product_ApplicantPage>();
                await _pageHelper.InteractWithField(Bolt.Automation.FrontEnds.Projects.Interview.FormData.FieldNames.PrimaryPhoneNumber);
                await applicantPage.ClickContinue();
            });

            await _logger.ExecuteStepAsync("Go back to Results  page and check rates", async () =>
            {
                resultPage = PageFactory.CreatePage<Product_ResultsPage>();
                // Reopening the quote re-rates it, and carriers land on the page one at a time -
                // reading straight away sees only whichever ones have already failed fast.
                await resultPage.WaitForRatingToComplete();
                var rates = await _interviewHelper.LogRatesBusinessRule(resultPage);

                Assert.That(rates.Admitted, Is.Not.Empty, "Failed. Should be at least 1 admitted carrier");
            });
        }

        [Test]
        [Tenant(COMPARION)]
        [Category("Interview")][Category("Quoting")][Category("Sanity")]
        [TestCaseId(223497)]
        [Author(Author.Viktor)]
        [Description("Comparion CT Personal Auto: create the application through the GetQuote API with a CT risk address, SSO into the interview, and confirm the Rule Engine lets one vehicle and two drivers run through to the Results page")]
        public async Task COMPARION_PL_Auto_CT_RuleEngine_E2E()
        {
            // The interview labels the LOB "Personal Auto" on the markets banner and product tab,
            // which is not the same token as Products.PersonalAuto sent to the API.
            const string ExpectedProduct = "Personal Auto";

            ScopeContext.Set(ctx => ctx.CurrentUser, TestContextAccessor.CurrentUserCollection.Agent);

            var requestData = new ApplicationRequestModel<PersonalLineData>
            {
                Products = Products.PersonalAuto,
                // Provider defaults are already one vehicle and two drivers; the assignments between
                // them are what the Rule Engine must carry through without corrupting.
                Data = PersonalLineDataProvider.GetPersonalAutoData(AddressData.CT, assignDriversToVehicles: true)
            };

            var externalId = await _logger.ExecuteStepAsync("Create the CT Personal Auto application via GetQuote API", async () =>
            {
                var response = await _getQuoteApi!.CreateApplicationAsync(requestData);
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created),
                    $"Failed, expected 201 from GetQuote API, actual [{response.StatusCode}] {response.GetErrorContent()}");

                return response.Content!.MapIdentifiers(ScopeContext).Id!;
            }, "Expected result: GetQuote API returns 201 Created");

            // Comparion's retrieve-quote relay state takes the bare application id — no &tenant= suffix,
            // so the relay state is built inline rather than via SsoHelper.CreateQuoteRetrieveRelayState.
            var ssoHelper = new SsoHelper(_logger, ScopeContext, _ssoApiFactory);
            await ssoHelper.LoginAsync(
                TestContextAccessor.CurrentUserCollection.Agent,
                new RelayStateTestData { Value = TestContextAccessor.CurrentRelayStateCollection.RetrieveQuoteRelayState.Value + externalId },
                BrowserManager);

            var marketsPage = await _logger.ExecuteStepAsync("Continue past the Start page", async () =>
            {
                var startPage = PageFactory.CreatePage<Product_StartPage>();

                // fillForms:false is the point of the test — the API already supplied the operators,
                // vehicles and their assignments, so re-filling would paper over a Rule Engine that
                // dropped or corrupted them. Reaching Markets at all is the assertion: a blocking
                // initiator on Vehicles or Drivers leaves us on Start and page validation throws.
                return await Executor.ExecuteToPage<Product_MarketsPage>(
                    FlowType.InterviewPLAgentAutoFlow, startPage, fillForms: false);
            }, "Expected result: navigation to the next page succeeds, so no Rule Engine validation blocked it");

            await _logger.ExecuteStepAsync("Verify the Rule Engine carried the product and state through", async () =>
            {
                var summary = await marketsPage.GetResultsSummary();
                var carrierCount = await marketsPage.GetCarrierCount(ExpectedProduct);

                Assert.Multiple(() =>
                {
                    Assert.That(summary, Does.Contain(ExpectedProduct),
                        $"Failed, expected the markets summary to name the product, actual [{summary}]");
                    Assert.That(summary, Does.Contain(AddressData.CT.State),
                        $"Failed, expected the markets summary to name the risk state, actual [{summary}]");
                    Assert.That(carrierCount, Is.GreaterThan(0),
                        $"Failed, expected at least one available carrier for {ExpectedProduct}, actual [{carrierCount}]");
                });
            }, $"Expected result: markets are returned for {ExpectedProduct} in {AddressData.CT.State}");

            await _logger.ExecuteStepAsync("Submit the quote and reach the Results page", async () =>
            {
                await Executor.ExecuteToPage<Product_ResultsPage>(
                    FlowType.InterviewPLAgentAutoFlow, marketsPage, fillForms: false);
            }, "Expected result: the quote submits and the Results page loads (TC step 5: no need to check getting rates)");
        }

        [Test]
        [Tenant(UNIFY)]
        [Category("Interview")][Category("Quoting")]
        [TestCaseId(110599)]
        [Author(Author.Gil)]
        [RunIn(includeStaging: true)]
        [Description("Unify GQ consumer-to-agent continuation: create applicant, create/patch/submit a PersonalHome application via GetQuote API, then continue as a MarketsLib agent - edit the quote from the dashboard and verify rates display")]
        public async Task UNIFY_PL_Home_ConsumerApi_To_AgentRates_E2E()
        {
            var apiHelper = new GetQuoteApiHelper(_getQuoteApi, ScopeContext);
            ScopeContext.Set(ctx => ctx.FrontEnd, FrontEndType.ADBX);
            var user = TestContextAccessor.CurrentUserCollection.MarketLibAgent;
            ScopeContext.Set(ctx => ctx.CurrentUser, user);

            string applicantId = string.Empty;
            string applicationExternalId = string.Empty;
            string friendlyId = string.Empty;
            var effectiveDate = DateTime.Today.AddDays(2).ToString("yyyy-MM-dd");
            var priorCarrierExpirationDate = DateTime.Today.AddDays(2).ToString("yyyy-MM-dd");

            await _logger.ExecuteStepAsync("Create Applicant via GetQuote API", async () =>
            {
                var applicant = TestDataProvider.TestData.ApplicantTestData.ApplicantTestData.Applicants.RandomApplicant;
                var applicantResponse = await _getQuoteApi.CreateApplicantAsync(applicant)
                                              .EnsureSuccessContentAsync();
                applicantId = applicantResponse.Id;
            }, "Expected result: Applicant created successfully");

            await _logger.ExecuteStepAsync("Create Application via GetQuote API", async () =>
            {
                var data = PersonalLineDataProvider.GetPersonalHomeData(
                    address: AddressData.CA);
                // Payload-specific identifying values
                data.DateOfBirth = "1980-09-11";
                data.PrimaryPhoneNumber = "555-656-4564";
                data.UnderMajorRenovation = false;
                data.UndergroundFuelTank = false;
                data.SinkholeInvestigationOrClaim = false;
                data.InCitySuburbDistrict = "City";
                data.WindstormDeductible = "OnePercent";
                data.PriorLiabilityCoverageHome = "Threehundredthousand";
                data.InsuranceFraud = false;
                data.BasementType = "Basement";
                data.NumOfDwelling = "1";
                data.PL_AdditionalStructures_Trampoline = false;
                data.DistanceToCoast = "2500";
                data.CurrentPersonalAutoCarrier = "AAA";
                data.NumberOfChildernsUnder18 = 1;

                var requestData = new ApplicationRequestModel<PersonalLineData>
                {
                    Products = Products.Homeowners,
                    ApplicantId = applicantId,
                    Data = data
                };
                var applicationResponse = await _getQuoteApi.CreateApplicationAsync(requestData)
                                                .EnsureSuccessContentAsync();
                applicationExternalId = applicationResponse.Id;
                friendlyId = applicationResponse.FriendlyId;
            }, "Expected result: Application created successfully");

            await _logger.ExecuteStepAsync("Update Application via GetQuote API", async () =>
            {
                var updateRequestData = new ApplicationRequestModel<PersonalLineData>
                {
                    Data = new PersonalLineData
                    {
                        EffectiveDate = effectiveDate,
                        PriorCarrierExperationDate = priorCarrierExpirationDate,
                    }
                };
                await _getQuoteApi.PatchApplicationAsync(applicationExternalId, updateRequestData)
                    .EnsureSuccessContentAsync();
            }, $"Expected result: Application updated with EffectiveDate: {effectiveDate} and PriorCarrierExpirationDate: {priorCarrierExpirationDate}");

            await _logger.ExecuteStepAsync("Submit Application via GetQuote API and poll submission", async () =>
            {
                await apiHelper.SubmitApplicationWithPollingAsync(applicationExternalId);
            }, "Expected result: Submission Status = Completed");

            await _adbxHelper.LoginAsync(
                user,
                TestContextAccessor.CurrentUrlCollection.FrontEnd.MarketsLib);

            await _logger.ExecuteStepAsync($"Search quote by friendlyId {friendlyId}", async () =>
            {

                var homePage = PageFactory.CreatePage<ADBX_HomePage>();
                await homePage.SearchFor(friendlyId);
            }, "Expected result: Quote found and displayed in search results");

            await _logger.ExecuteStepAsync("Open quote and click Edit Quote button", async () =>
            {
                await _pageHelper!.SelectTableRowAsync(1);
                var quoteSummaryPage = PageFactory.CreatePage<ADBX_QuoteSummaryPage>();
                await quoteSummaryPage.ClickOnEditQuote();
            }, "Expected result: Navigated to the Interview / Results flow for the selected quote");

            var resultPage = PageFactory.CreatePage<Product_ResultsPage>();

            await _logger.ExecuteStepAsync("Go back to Applicant page and edit phone", async () =>
            {
                await resultPage.ClickBack();
                var applicantPage = PageFactory.CreatePage<Product_ApplicantPage>();
                await _pageHelper!.InteractWithField(FieldNames.PrimaryPhoneNumber);
                await applicantPage.ClickContinue();
            }, "Expected result: Applicant page displayed; after edit the quote enters pre-submission loading");

            await _logger.ExecuteStepAsync("Go back to Results page and check rates", async () =>
            {
                resultPage = PageFactory.CreatePage<Product_ResultsPage>();
                var rates = await _interviewHelper.LogRatesBusinessRule(resultPage);

                Assert.That(rates.Admitted, Is.Not.Empty, "Failed. Rates are not displayed on results page");
            }, "Expected result: Results page displayed and at least one rate is present");
        }

        /// <summary>
        /// SetArgDisplayNames keeps the NUnit case name short. The name is used twice in every
        /// artifact path - a file named after the test, inside a directory named after it - so the
        /// full argument list would push that pair past the 260-character Windows path limit.
        /// </summary>
        private static IEnumerable<TestCaseData> SalesEnvironmentNewAccountCases()
        {
            yield return new TestCaseData("Homeowners", "33333 Lyndon B Johnson Fwy, Dallas, TX 75241", "05/05/1985", "2156098090")
                .SetArgDisplayNames("HO3", "TX")
                .SetProperty("TestCaseId", "252328");
            yield return new TestCaseData("Condo", "3333 S 7th St, Phoenix, AZ 85040", "05/05/1980", "2160890808")
                .SetArgDisplayNames("HO6", "AZ")
                .SetProperty("TestCaseId", "252361");
        }

        [Test]
        [Tenant(UNIFY)]
        [Category("Interview")][Category("Quoting")][Category("ADBX")][Category("Sanity")]
        [Author(Author.Helen)]
        [RunIn(BoltEnvironment.Staging)]
        [TestCaseSource(nameof(SalesEnvironmentNewAccountCases))]
        [Description("Unify sales-environment agent creates a PL account from ADBX New Quote and takes the agent interview through the Markets page to rates")]
        public async Task UNIFY_SalesEnvironment_PL_NewAccount_To_Rates_E2E(string lob, string address, string dateOfBirth, string phone)
        {
            ScopeContext.Set(ctx => ctx.FrontEnd, FrontEndType.ADBX);

            var user = TestContextAccessor.CurrentUserCollection.SalesEnvironmentAdmin
                ?? throw new TestSetupException("UNIFY Staging SalesEnvironmentAdmin user not configured");

            await _adbxHelper.LoginAsync(user, user.LoginUrl);

            var (_, startPage) = await _adbxHelper.StartQuoteFromNewAccountAsync<Product_StartPage>(new Dictionary<string, string>
            {
                [ADBX_FieldNames.AccountSource] = "Sales Environment",
                // Staging needs real-looking names; the registry defaults bake NameSelector at static
                // init, before the environment is known, so they are resolved here instead.
                [ADBX_FieldNames.AccountFirstName] = NameSelector.GetFirstName(),
                [ADBX_FieldNames.AccountLastName] = NameSelector.GetLastName(),
                [ADBX_FieldNames.AccountPhone] = phone,
                [ADBX_FieldNames.AccountAddress] = address,
            });

            var resultsPage = await _logger.ExecuteStepAsync(
                "Fill in the interview pages - Let's Start, Markets, Home, Structure, Features, Policy and Applicant",
                async () => await Executor.ExecuteToPage<Product_ResultsPage>(
                    FlowType.InterviewPLAgentFlow,
                    currentPage: startPage,
                    new Dictionary<string, string>
                    {
                        [FieldNames.InterviewAddress] = address,
                        [FieldNames.DateOfBirth] = dateOfBirth,
                        [FieldNames.Lob] = lob,
                    },
                    fillForms: true,
                    // Starting from a blank account means nothing pre-answers these, unlike the
                    // API-seeded flows: PriorPersonalHomeLiability populates only after the
                    // prior-carrier answers, and NumberOfMortgagees is untagged on purpose.
                    pageCallbacks: PageCallbackManager.For<Product_PolicyPage>(async _ =>
                    {
                        await _pageHelper!.InteractWithField(FieldNames.PriorPersonalHomeLiability, "$300,000");
                        await _pageHelper!.InteractWithField(FieldNames.NumberOfMortgagees, "0");
                    })
                    // The form fill does not reliably land BusinessOrDaycare in CI, and /Home will not
                    // advance without it. Re-answering once the page has settled is idempotent.
                    .And<Product_HomePage>(async _ =>
                        await _pageHelper!.InteractWithField(FieldNames.BusinessOrDaycare, "No"))),
                "Expected result: Agent answered every interview page and reached the rates page");

            await _logger.ExecuteStepAsync("Check rates on the Results page", async () =>
            {
                // Carriers land on the page one at a time; reading before rating finishes sees only
                // whichever ones have failed fast.
                await resultsPage.WaitForRatingToComplete();
                var rates = await _interviewHelper.LogRatesBusinessRule(resultsPage);

                Assert.That(rates.Admitted, Is.Not.Empty, "Failed. No carrier returned a rate on the results page");
            }, "Expected result: At least one carrier returns a rate");
        }
    }
}

