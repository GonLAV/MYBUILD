using Bolt.Automation.ApiClients.AdbxApi;
using Bolt.Automation.ApiClients.GetQuoteApi.Interfaces;
using Bolt.Automation.ApiClients.GetQuoteApi.Models.Application;
using Bolt.Automation.ApiClients.Infrastructure;
using Bolt.Automation.Common;
using Bolt.Automation.Common.Enums;
using Bolt.Automation.Common.Exceptions;
using Bolt.Automation.Common.Logging.Extentions;
using Bolt.Automation.Common.Models.TestData.DataModels.CommercialLine;
using Bolt.Automation.Common.Utils;
using Bolt.Automation.FrontEnds.CommonHelpers;
using Bolt.Automation.FrontEnds.Executor.Helpers;
using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;
using Bolt.Automation.FrontEnds.Projects.ADBX.FormData;
using Bolt.Automation.FrontEnds.Projects.ADBX.Pages.Policies;
using Bolt.Automation.FrontEnds.Projects.ADBX.Pages.Quotes;
using Bolt.Automation.FrontEnds.Projects.Interview.Flows;
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
using NUnit.Framework;
using System.Net;
using static Bolt.Automation.Common.Tenant;
using static Bolt.Automation.FrontEnds.Projects.ADBX.FormData.ADBX_FieldNames;
using static Bolt.Automation.TestDataProvider.TestData.ApplicationTestData.ApplicationTestData;
using AuthorAttribute = Bolt.Automation.Tests.TestExtension.Attributes.AuthorAttribute;
using BoltEnvironment = Bolt.Automation.Common.Environment;
using FieldNames = Bolt.Automation.FrontEnds.Projects.Interview.FormData.FieldNames;

namespace Bolt.Automation.Tests.Tests.Interview
{
    public class CLTests : UITestBase
    {
        /// <summary>The commercial address the BoltAccess CL tests quote against.</summary>
        private const string BoltAccessClAddress = "2036 Oak Grove Ln, Aubrey, TX 76227";

        private IMainQueries? _mainQueries;
        private IAdbxApiClientFactory _adbxApiFactory = null!;
        private IGetQuoteApi _getQuoteApi = null!;
        private AdbxTestHelper _adbxHelper = null!;
        private InterviewTestHelper _interviewHelper = null!;
        private SalesEnvironmentCLTestHelper _salesEnvHelper = null!;

        public CLTests() : base()
        {
            ScopeContext.Set(ctx => ctx.FrontEnd, FrontEndType.Interview);
        }

        protected override void ResolveServices()
        {
            var refitApiLocator = _uiTestScope.ServiceProvider.GetRequiredService<RefitApiServiceLocator>();
            _mainQueries = _testScope.ServiceProvider.GetService<IMainQueries>();
            _adbxApiFactory = _testScope.ServiceProvider.GetRequiredService<IAdbxApiClientFactory>();
            _getQuoteApi = refitApiLocator.GetRequiredService<IGetQuoteApi>();
        }

        protected override void InitializeComponents()
        {
            base.InitializeComponents();
            _adbxHelper = new AdbxTestHelper(_logger, BrowserManager, PageFactory, _pageHelper!, ScopeContext);
            _interviewHelper = new InterviewTestHelper(_logger, BrowserManager, PageFactory, _pageHelper!, ScopeContext);
            _salesEnvHelper = new SalesEnvironmentCLTestHelper(ScopeContext, _adbxHelper, PageFactory, TestContextAccessor);
        }

        [Test]
        [Tenant(BOLTAG)]
        [Category("Sanity")]
        [Category("Interview")]
        [Category("Quoting")]
        [Category("CRM")]
        [TestCaseId(133598)]
        public async Task BOLTAG_CL_Consumer_WC_E2E_Test()
        {
            var frontEndUrls = ScopeContext.Data.UrlDataCollection.FrontEnd;
            var consumerUrl = frontEndUrls?.AdditionalUrls?.GetValueOrDefault("ConsumerInterviewCL")
                ?? throw new TestSetupException("ConsumerDashboard URL not configured in FrontEnd.AdditionalUrls");
            ScopeContext.Set(ctx => ctx.CurrentUrl, consumerUrl);

            _logger.Info($"Navigating to consumer interview: {consumerUrl}");
            await BrowserManager.NavigateAsync(consumerUrl);

            await _logger.ExecuteStepAsync("Execute WC Interview Flow", async () =>
            {
                var resultsPage = await Executor.Execute<Product_StartPage, Product_ResultsPage>(
                    FlowType.InterviewWCFlow,
                    new Dictionary<string, string>
                    {
                        [FieldNames.InterviewAddress] = "507 Kouns Dr Nw, LINN, OR, 97321",
                        [FieldNames.EposNaicDescription] = "Flight Training",
                        [FieldNames.FederalIDNumber] = new Random().Next(100000000, 999999999).ToString(),
                        [FieldNames.DateOfBirth] = DateTime.Today.AddYears(-40).ToString("MM/dd/yyyy"),
                        [FieldNames.CurrentPremium] = "100000",
                    },
                    fillForms: true,
                    startUrl: consumerUrl
                );

                await _logger.ExecuteStepAsync("Verify Rates Returned", async () =>
                {
                    var rates = await _interviewHelper.LogRatesBusinessRule(resultsPage);
                    Assert.That(rates.Admitted, Is.Not.Empty, "No rates returned");
                });

                await _logger.ExecuteStepAsync("Validate Quote via ADBX API", async () =>
                {
                    var friendlyId = await _pageHelper.GetFieldValue(LocatorType.XPath, "//div[contains(@class,'card-wrapper')]");
                    _logger.Info($"Quote FriendlyId: {friendlyId}");

                    var applicationId = await _mainQueries.Policy.GetPolicyIdByFriendlyIdAsync(friendlyId);
                    Assert.That(applicationId, Is.Not.Null);

                    var user = TestContextAccessor.CurrentUserCollection.Agent;
                    ScopeContext.Set(ctx => ctx.CurrentUser, user);

                    var adbxApi = await _adbxApiFactory.CreateApiClientAsync();
                    var quoteResponse = await adbxApi.GetQuoteByQuoteId(applicationId);

                    Assert.That(quoteResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

                    var source = quoteResponse.Content?.SourceKeyword;
                    Assert.That(source, Is.Not.Null);

                    _logger.LogDataValidation("Source Keyword",
                        source.Equals("organic-cl", StringComparison.OrdinalIgnoreCase),
                        "organic-cl", source,
                        "Quote source should be organic-cl for consumer CL flow");

                    Assert.That(source, Is.EqualTo("organic-cl").IgnoreCase);
                });
            });
        }

        [Test]
        [RunIn(includeStaging: true)]
        [Tenant(KRAFTLAKEX)]
        [Category("KLX")]
        [Category("CL")]
        [Category("Quoting")]
        [Category("Interview")]
        [Author(Author.Viktor)]
        [TestCaseId(240782)]
        [Description("KLX LSP agent submits a commercial-line auto quote through the old interview to rates")]
        public async Task KLX_CLAuto_E2E_SubmitToRates()
        {
            ScopeContext.Set(ctx => ctx.FrontEnd, FrontEndType.ADBX);

            var user = TestContextAccessor.CurrentUserCollection.Agent
                ?? throw new TestSetupException("KLX Staging Agent user not configured");
            var loginUrl = ScopeContext.Data.UrlDataCollection.FrontEnd?.LoginUrl
                ?? throw new TestSetupException("KLX Staging LoginUrl not configured");


            var address = AddressData.TX;

            await _adbxHelper.LoginAsync(user, loginUrl);


            await _logger.ExecuteStepAsync("Start a Commercial CL quote", async () =>
            {
                await _adbxHelper.SelectCommercialAccountAndStartQuoteAsync();
            });

            var resultsPage = await Executor.ExecuteToPage<Product_ResultsPage>(
                FlowType.InterviewBoltAccessCLAutoFlow,
                PageFactory.CreatePage<ProductBusinessProfilePageCL>(),
                new Dictionary<string, string>
                {
                    [FieldNames.InterviewAddress] = $"{address.AddressLine1}, {address.City}, {address.State}, {address.ZipCode}",
                    // Business contact / identity
                    [FieldNames.OrganizationName] = "Test Automation",
                    [FieldNames.EposNaicDescription] = "Auto Hauling, Long-Distance",
                    // Business — Corporation w/ FEIN, zero payroll
                    [FieldNames.LegalEntity] = "Corporation",
                    [FieldNames.FederalIDNumber] = "478500001",
                    [FieldNames.AnnualPayroll] = "0",

                    // Vehicle — utility trailer used commercially
                    [FieldNames.VIN] = "4RAVS1629TK143345",
                    [FieldNames.AnnualMileage] = "201-300 miles",
                    [FieldNames.TruckSubCategory] = "Utility Trailer",
                    [FieldNames.VehicleOwnerShip] = "Less than 1 month",
                    [FieldNames.PrimaryUseOfVehicle] = "Business Only",

                    [FieldNames.CurrentBopCarrier] = "My insurance company is not listed",
                    [FieldNames.CL_BIPD] = "$100,000 CSL",
                    [FieldNames.CombinedUninsuredUnderinsuredMotorist] = "$100,000",
                    [FieldNames.UninsuredMotoristPropertyDamage] = "100,000",
                    [FieldNames.CL_ComprehensiveDeductible] = "100",
                    [FieldNames.CL_CollisionDeductible] = "$1,000",
                    [FieldNames.CL_CurrentVehicleValue] = "45000",
                    [FieldNames.CLOperatorLicenseState] = "Texas"
                },
                fillForms: true,
                pagesToAdd: [new(typeof(ProductAdditionalQuestionsProgressivePageCL), After: typeof(ProductMarketSelectionsPageCL))]
                );

            await _logger.ExecuteStepAsync("Verify rates returned", async () =>
            {
                var rates = await _interviewHelper.LogRatesBusinessRule(resultsPage);

                Assert.That(rates.Admitted, Is.Not.Empty, "No rates returned on the result page");
            });
        }

        [Test]
        [Tenant(KRAFTLAKEX)]
        [Category("CL")]
        [Category("Quoting")]
        [Category("Interview")]
        [Author(Author.Gil)]
        [TestCaseId(237437)]
        [Description("KraftlakeX CL BOP - Nail Salons industry triggers markets page with offline request flow")]
        public async Task KLX_CL_BOP_MarketsPage_OfflineRequest()
        {
            ScopeContext.Set(ctx => ctx.CurrentUser, TestContextAccessor.CurrentUserCollection.LSP1CL);
            var submissionResponse = await _logger.ExecuteStepAsync("Create BOP application via API with Nail Salons industry", async () =>
            {
                var data = CommercialLineDataProvider.GetCommercialLineData(address: AddressData.TX);
                data.BoltIndustry = "81211300 - Nail Salons";
                data.CLCertifyFarmersDeclined = true;
                data.CL_IneligibleReason = "SeasonalOperations";
                data.OrganizationName = "AutoTest " + RandomManager.GetRandomString(8);

                var apiHelper = new GetQuoteApiHelper(_getQuoteApi, ScopeContext);
                var response = await apiHelper.CreateApplication_GetQuestionnaire(new ApplicationRequestModel<CommercialLineData>
                {
                    Products = Products.BOP,
                    Data = data
                });
                return response;
            });

            var offlineRequest = await _logger.ExecuteStepAsync("Navigate to interview and advance to markets page than click Offline Request on markets page", async () =>
            {
                var marketPage = await Executor.Execute<ProductBusinessProfilePageCL, ProductMarketResultsPageCL>(
                FlowType.InterviewBOPFlow,
                fillForms: false,
                startUrl: submissionResponse.Url
            );
                var offlineRequest = await marketPage.ClickOfflineRequest();
                return offlineRequest;
            });

            var offlineRequestPopup = await _logger.ExecuteStepAsync("Open offline request popup and fill form", async () =>
                await _interviewHelper.SubmitOfflineRequestAsync(offlineRequest));

            await _interviewHelper.VerifyOfflineRequestSubmittedAsync(offlineRequestPopup);
        }

        [Test]
        [Tenant(KRAFTLAKEX)]
        [Category("CL")]
        [Category("Quoting")]
        [Category("Interview")]
        [Author(Author.Gil)]
        [TestCaseId(235689)]
        [Description("KraftlakeX CL BOP - Verify Application Forms popup shows ACORD 125 and 126 and download works")]
        public async Task KLX_CL_BOP_ResultsPage_ApplicationForms_Download()
        {
            ScopeContext.Set(ctx => ctx.CurrentUser, TestContextAccessor.CurrentUserCollection.LSP1CL);
            var data = CommercialLineDataProvider.GetCommercialLineData(address: AddressData.TX);
            data.BoltIndustry = "56159901 - Ticket Offices";
            data.CLCertifyFarmersDeclined = true;
            data.AnnualPayroll = 100000;

            var apiHelper = new GetQuoteApiHelper(_getQuoteApi, ScopeContext);
            var submissionResponse = await apiHelper.CreateApplication_GetQuestionnaire(new ApplicationRequestModel<CommercialLineData>
            {
                Products = Products.BOP,
                Data = data
            });

            var resultsPage = await _logger.ExecuteStepAsync("Execute BOP Interview Flow to results page", async () =>
            {
                return await Executor.Execute<ProductBusinessProfilePageCL, Product_ResultsPage>(
                    FlowType.InterviewBOPFlow,
                    fillForms: true,
                    startUrl: submissionResponse.Url,
                    pageCallbacks: PageCallbackManager.For<ProductMarketSelectionsPageCL>(async page =>
                    {
                        await _pageHelper!.InteractWithField(FieldNames.QuoteAllEligible, "false");
                    }, PageCallbackManager.CallbackTiming.AfterFillForm)
                );
            });

            var applicationFormsPopup = await _logger.ExecuteStepAsync("Open Application Forms popup and verify ACORD forms", async () =>
            {
                var applicationFormsPopup = await resultsPage.ClickApplicationFormsButton();
                var acordForms = await applicationFormsPopup.GetAcordFormsList();
                _logger.LogDataValidation("ACORD 125 Present", acordForms.Any(f => f.Contains("125")), "True", acordForms.Any(f => f.Contains("125")).ToString(), "ACORD Form 125 should be listed");
                _logger.LogDataValidation("ACORD 126 Present", acordForms.Any(f => f.Contains("126")), "True", acordForms.Any(f => f.Contains("126")).ToString(), "ACORD Form 126 should be listed");
                Assert.Multiple(() =>
                {
                    Assert.That(acordForms.Any(f => f.Contains("125")), Is.True, "ACORD Form 125 not found");
                    Assert.That(acordForms.Any(f => f.Contains("126")), Is.True, "ACORD Form 126 not found");
                });
                return applicationFormsPopup;
            });

            await _logger.ExecuteStepAsync("Download all ACORD forms and verify download", async () =>
            {
                var page = BrowserManager.GetCurrentTab()!;
                var downloadHelper = new FileDownloadValidatorHelper(page, _logger);

                var result = await downloadHelper.ValidateFileDownload(
                    downloadTriggerAction: async () =>
                    {
                        await _pageHelper!.InteractWithField(FieldNames.DownloadAllFormsButtonPopup);
                    },
                    expectedFileExtension: ".pdf"
                );

                _logger.LogDataValidation("ACORD PDF Downloaded", result.FileSizeBytes > 0, "Non-empty", $"{result.FileSizeBytes} bytes", "Downloaded ACORD forms PDF should not be empty");
                Assert.That(result.FileSizeBytes, Is.GreaterThan(0), "Downloaded ACORD forms PDF is empty");
            });

            await _logger.ExecuteStepAsync("Close Application Forms popup", async () =>
            {
                await applicationFormsPopup.ClosePopup();
            });
        }

        [Test]
        [Tenant(BOLTACCESS)]
        [Category("CL")]
        [Category("Quoting")]
        [Category("Interview")]
        [Author(Author.Gil)]
        [TestCaseId(237634)]
        [Description("BoltAccess CL Auto quote — verifies Progressive appears on Market Availability page and offline request submits successfully")]
        public async Task BOLTACCESS_CL_CLAuto_Progressive_Offline_Request()
        {
            var user = TestContextAccessor.CurrentUserCollection.AgentCL;
            await _adbxHelper.StartCommercialQuoteFromExistingAccountAsync(user);

            var resultsPage = await _logger.ExecuteStepAsync("Execute Commercial auto Interview Flow to results page", async () =>
            {
                ScopeContext.Set(ctx => ctx.FrontEnd, FrontEndType.Interview);
                return await Executor.ExecuteToPage<Product_ResultsPage>(
                FlowType.InterviewBoltAccessCLAutoFlow,
                PageFactory.CreatePage<ProductBusinessProfilePageCL>(),
                new Dictionary<string, string>
                {
                    [FieldNames.InterviewAddress] = BoltAccessClAddress,
                    [FieldNames.EposNaicDescription] = "Air traffic Control",
                    [FieldNames.CL_BIPD] = " $300,000 CSL ",
                    [FieldNames.UninsuredMotoristPropertyDamage] = " $300,000 ",
                    [FieldNames.CombinedUninsuredUnderinsuredMotorist] = " $300,000 CSL ",
                },
                fillForms: true,
                pageCallbacks: PageCallbackManager.For<ProductMarketResultsPageCL>(async page =>
                {
                    var progressiveVisible = await _interviewHelper.IsCarrierVisible("Progressive");
                    Assert.That(progressiveVisible, Is.True, "Progressive should be visible on market selections page");
                }, PageCallbackManager.CallbackTiming.AfterFillForm));
            });

            await resultsPage.ClickMarketCategoryTab("Additional Carriers");
            await resultsPage.ClickOfflineRequest();

            await _logger.ExecuteStepAsync("Complete Block Bind operator and vehicle pages", async () =>
            {
                var operatorPage = PageFactory.CreatePage<BlockBindOperatorPageCL>();
                await operatorPage.ClickContinue();

                var vehiclePage = PageFactory.CreatePage<BlockBindVehiclePageCL>();
                await vehiclePage.SetVIN("4T1BF1FK0FU105897");
                await vehiclePage.ClickContinue();
            }, "Expected result: Operator page continued and vehicle VIN submitted, advancing past the Block Bind vehicle page");

            await _interviewHelper.SubmitAdditionalCarriersOfflineRequestAsync();
        }


        [Test]
        [Tenant(BOLTACCESS)]
        [Category("CL")]
        [Category("Quoting")]
        [Category("Interview")]
        [Author(Author.Sandy)]
        [TestCaseId(236042)]
        [Description("BoltAccess CL BOP quote — Additional Carriers Offline Request")]
        public async Task BOLTACCESS_CL_BOP_Additional_Carriers_Offline_Request()
        {
            var user = TestContextAccessor.CurrentUserCollection.AgentCL;
            await _adbxHelper.StartCommercialQuoteFromExistingAccountAsync(user);

            var resultsPage = await _logger.ExecuteStepAsync("Execute Bop Flow to results page by unselecting Quote all eligible", async () =>
            {
                return await Executor.ExecuteToPage<Product_ResultsPage>(
                FlowType.InterviewBOPFlow,
                PageFactory.CreatePage<ProductBusinessProfilePageCL>(),
                new Dictionary<string, string>
                {
                    [FieldNames.InterviewAddress] = BoltAccessClAddress,
                    [FieldNames.EposNaicDescription] = "Limousine Service",
                },
                fillForms: true,
                pageCallbacks: PageCallbackManager.For<ProductMarketSelectionsPageCL>(async page =>
                {
                    await _pageHelper!.InteractWithField(FieldNames.QuoteAllEligible, "false");
                }, PageCallbackManager.CallbackTiming.AfterFillForm));
            });

            await _interviewHelper.SubmitAdditionalCarriersOfflineRequestAsync();
        }

        [Test]
        [Tenant(BOLTACCESS)]
        [Category("CL")]
        [Category("Quoting")]
        [Category("Interview")]
        [Author(Author.Sandy)]
        [TestCaseId(236071)]
        [Description("BoltAccess CL BOP quote — Admitted Carriers Request Application")]
        public async Task BOLTACCESS_CL_BOP_Admitted_Carriers_Request_Application()
        {
            var user = TestContextAccessor.CurrentUserCollection.AgentCL;
            await _adbxHelper.StartCommercialQuoteFromExistingAccountAsync(user);

            var resultsPage = await _logger.ExecuteStepAsync("Execute Bop Flow to results page", async () =>
            {
                return await Executor.ExecuteToPage<Product_ResultsPage>(
                FlowType.InterviewBOPFlow,
                PageFactory.CreatePage<ProductBusinessProfilePageCL>(),
                new Dictionary<string, string>
                {
                    [FieldNames.InterviewAddress] = BoltAccessClAddress,
                    [FieldNames.EposNaicDescription] = "Hobby and Art Supply Stores",
                },
                fillForms: true,
                pagesToAdd: [new(typeof(ProductAdditionalQuestionsTravelersPageCL), After: typeof(ProductMarketSelectionsPageCL))],
                pageCallbacks: PageCallbackManager.For<ProductMarketSelectionsPageCL>(async page =>
                {
                    await _pageHelper!.InteractWithField(FieldNames.QuoteAllEligible, "false");
                    await page.SelectCarrierAsync("Travelers");
                }, PageCallbackManager.CallbackTiming.AfterFillForm));
            });

            await _logger.ExecuteStepAsync("Wait for submission completion and Travelers rate to appear on Admitted tab", async () =>
            {
                var appeared = await resultsPage.WaitForCarrierToAppear("Travelers");
                Assert.That(appeared, Is.True, "Travelers carrier did not appear on Admitted tab");
            });

            await _logger.ExecuteStepAsync("Submit Request Application for Travelers and verify button turned to request submitted", async () =>
            {
                var popup = await resultsPage.ClickRequestApplication();
                await popup.ClickSubmitRequest();
                await popup.ClickConfirm();
                var isSubmitted = await popup.IsRequestSubmittedButtonExists();
                Assert.That(isSubmitted, Is.True, "Button should show 'Request Submitted' (greyed out)");
            }, "Expected result: Request Application popup submitted, button transitions to 'Request Submitted'");

            await _logger.ExecuteStepAsync("Validate case submission in DB", async () =>
            {
                var polData = await _mainQueries!.Policy.GetPolicyByFriendlyIdAsync(ScopeContext.Data.FriendlyId);
                var csData = await _mainQueries.Case.GetCaseInfoByApplicationIdAsync(polData!.Id);
                Assert.That(csData!.IsSubmitted, Is.True, "Case not submitted successfully");
                _logger.Info($"PolicyId: {polData.Id}, CaseExternalId: {csData.ExternalId}");
            });
        }

        [Test]
        [Tenant(BOLTACCESS)]
        [Category("CL")]
        [Category("Quoting")]
        [Category("Interview")]
        [Author(Author.Sandy)]
        [TestCaseId(237268)]
        [Description("BoltAccess CL GL quote — Admitted Carriers Request Application")]
        public async Task BOLTACCESS_CL_GL_Acord_Carriers()
        {
            var user = TestContextAccessor.CurrentUserCollection.AgentCL;
            await _adbxHelper.StartCommercialQuoteFromExistingAccountAsync(user);

            var resultsPage = await _logger.ExecuteStepAsync("Execute GL Flow to results page", async () =>
            {
                return await Executor.ExecuteToPage<Product_ResultsPage>(
                FlowType.InterviewBoltAccessGLFlow,
                PageFactory.CreatePage<ProductBusinessProfilePageCL>(),
                new Dictionary<string, string>
                {
                    [FieldNames.InterviewAddress] = BoltAccessClAddress,
                    [FieldNames.EposNaicDescription] = "Hobby and Art Supply Stores",
                },
                fillForms: true,
                pageCallbacks: PageCallbackManager.For<ProductSelectionPageCL>(async page =>
                {
                    await page.SelectLob("General Liability");
                }
                , PageCallbackManager.CallbackTiming.AfterFillForm));
            });

            await _logger.ExecuteStepAsync("Click Acord tab and verify ACORD 125 and 126 are listed", async () =>
            {
                await resultsPage.ClickMarketCategoryTab("ACORD");
                var acordForms = await resultsPage.GetAcordFormsList();

                Assert.Multiple(() =>
                {
                    Assert.That(acordForms.Any(f => f.Contains("125")), Is.True, "ACORD Form 125 not found");
                    Assert.That(acordForms.Any(f => f.Contains("126")), Is.True, "ACORD Form 126 not found");
                });
            });

            await _logger.ExecuteStepAsync("Download all ACORD forms and verify PDF download", async () =>
            {
                var page = BrowserManager.GetCurrentTab()!;
                var downloadHelper = new FileDownloadValidatorHelper(page, _logger);

                var result = await downloadHelper.ValidateFileDownload(
                    downloadTriggerAction: async () =>
                    {
                        await _pageHelper!.InteractWithField(FieldNames.DownloadAllFormsButton);
                    },
                    expectedFileExtension: ".pdf"
                );

                _logger.LogDataValidation("ACORD PDF Downloaded", result.FileSizeBytes > 0, "Non-empty", $"{result.FileSizeBytes} bytes", "Downloaded ACORD forms PDF should not be empty");
                Assert.That(result.FileSizeBytes, Is.GreaterThan(0), "Downloaded ACORD forms PDF is empty");
            });
        }

        [Test]
        [Tenant(BOLTACCESS)]
        [Category("CL")]
        [Category("Quoting")]
        [Category("Interview")]
        [Author(Author.Gil)]
        [TestCaseId(237500)]
        [Description("BoltAccess CL Auto quote in a state with no online market (Anchorage, AK) — Progressive appears in the appetite, nothing quotes online, and the Declination tab points at the Offline Request; verifies both tab messages and submits an offline request")]
        public async Task BOLTACCESS_CL_CLAuto_Progressive_StateNotSupported_OfflineRequest()
        {
            var user = TestContextAccessor.CurrentUserCollection.AgentCL;
            await _adbxHelper.StartCommercialQuoteFromExistingAccountAsync(user);

            var resultsPage = await _logger.ExecuteStepAsync("Execute Commercial Auto Interview Flow with an unsupported state (Anchorage, AK)", async () =>
            {
                ScopeContext.Set(ctx => ctx.FrontEnd, FrontEndType.Interview);
                return await Executor.ExecuteToPage<Product_ResultsPage>(
                    FlowType.InterviewBoltAccessCLAutoFlow,
                    PageFactory.CreatePage<ProductBusinessProfilePageCL>(),
                    new Dictionary<string, string>
                    {
                        [FieldNames.InterviewAddress] = "505 W 2nd Ave, Anchorage, AK 99501",
                        [FieldNames.EposNaicDescription] = "48811100 - Air Traffic Control",
                        [FieldNames.CL_BIPD] = " $300,000 CSL ",
                    },
                    fillForms: true,
                    pageCallbacks: PageCallbackManager.For<ProductMarketResultsPageCL>(async page =>
                    {
                        var progressiveVisible = await _interviewHelper.IsCarrierVisible("Progressive");
                        Assert.That(progressiveVisible, Is.True, "Progressive should appear as available in the appetite on the Market Availability page");
                    }, PageCallbackManager.CallbackTiming.AfterFillForm)
                    .And<ProductMarketSelectionsPageCL>(async page =>
                    {
                        await page.EnsureQuoteAllEligibleSelectedAsync();
                    }, PageCallbackManager.CallbackTiming.AfterFillForm));
            }, "Expected result: Market Availability page shows Progressive as a carrier available in the appetite");

            await _logger.ExecuteStepAsync("Verify admitted and declination tab messages on the result page", async () =>
            {
                await resultsPage.ClickMarketCategoryTab("Admitted");
                var admittedMessage = await resultsPage.IsMessageVisible("Online quotes are unavailable");

                await resultsPage.ClickMarketCategoryTab("Declination");
                // Progressive stopped carrying a per-carrier "only available for offline quoting" UUD when
                // bug 248647 removed the Wave 10 states (AK/MA/SD/VT) from the ODM rule; the tab now points
                // at the Offline Request instead. CA/HI/WA still get the old carrier-specific wording.
                var declinationMessage = await resultsPage.IsMessageVisible("No online quotes received");

                Assert.Multiple(() =>
                {
                    Assert.That(admittedMessage, Is.True, "Admitted tab message 'Online quotes are unavailable' not found");
                    Assert.That(declinationMessage, Is.True, "Declination tab message 'No online quotes received' not found");
                });
            }, "Expected result: Admitted tab shows \"Online quotes are unavailable\"; Declination tab shows \"No online quotes received. Consider using the Offline Request…\"");

            await _logger.ExecuteStepAsync("Open Additional Carriers tab and verify the Offline request button is offered", async () =>
            {
                await resultsPage.ClickMarketCategoryTab("Additional Carriers");
                await resultsPage.ClickOfflineRequest();
            });

            await _logger.ExecuteStepAsync("Complete Block Bind operator and vehicle pages", async () =>
            {
                var operatorPage = PageFactory.CreatePage<BlockBindOperatorPageCL>();
                await operatorPage.ClickContinue();

                var vehiclePage = PageFactory.CreatePage<BlockBindVehiclePageCL>();
                await vehiclePage.SetVIN("4T1BF1FK0FU105897");
                await vehiclePage.ClickContinue();
            }, "Expected result: Operator page continued and vehicle VIN submitted, advancing past the Block Bind vehicle page");

            await _interviewHelper.SubmitAdditionalCarriersOfflineRequestAsync();
        }

        [Test]
        [Tenant(BOLTACCESS)]
        [Category("CL")]
        [Category("Quoting")]
        [Category("Interview")]
        [Author(Author.Gil)]
        [TestCaseId(235799)]
        [Description("BoltAccess CL New Flow BOP — submits an Offline request from  Markets Availability page and verifies the request is submitted")]
        public async Task BOLTACCESS_CL_BOP_MarketsAvailability_OfflineRequest()
        {
            var user = TestContextAccessor.CurrentUserCollection.AgentCL;
            await _adbxHelper.StartCommercialQuoteFromExistingAccountAsync(user);

            var marketResultsPage = await _logger.ExecuteStepAsync("Select BOP LOB and advance to the Markets Availability page", async () =>
            {
                ScopeContext.Set(ctx => ctx.FrontEnd, FrontEndType.Interview);
                return await Executor.ExecuteToPage<ProductMarketResultsPageCL>(
                    FlowType.InterviewBOPFlow,
                    PageFactory.CreatePage<ProductBusinessProfilePageCL>(),
                    fillForms: true);
            }, "Expected result: Markets Availability page shows the relevant BOP carriers and the \"Do you already have a paper application\" card with an \"Offline request\" button");

            var offlineRequestPopup = await _logger.ExecuteStepAsync("Open the Offline Request popup from the paper-application card and fill it", async () =>
            {
                var paperApplicationCardShown = await _pageHelper!.ElementExists(FieldNames.PaperApplicationCard, timeout: 8000);
                _logger.LogDataValidation("Paper Application Card", paperApplicationCardShown, "true", paperApplicationCardShown.ToString(),
                    "Markets Availability page should show the 'Do you already have a paper application?' card before the Offline request is opened");
                Assert.That(paperApplicationCardShown, Is.True, "'Do you already have a paper application?' card was not shown before clicking the Offline request button");

                var popup = await marketResultsPage.ClickOfflineRequest();
                return await _interviewHelper.SubmitOfflineRequestAsync(popup);
            }, "Expected result: Offline Request popup opens; after Submit Request it shows \"Your request was submitted\" with a Confirm button");

            await _interviewHelper.VerifyOfflineRequestSubmittedAsync(offlineRequestPopup,
                "Expected result: Redirect back to CL_MarketsResults, button shows 'Request Submitted' (grayed out), case created");
        }

        [Test]
        [RunIn(BoltEnvironment.Staging)]
        [Tenant(UNIFY)]
        [Category("UNIFY")]
        [Category("CL")]
        [Category("Quoting")]
        [Category("Interview")]
        [Author(Author.Andrii)]
        [TestCaseId(254672)]
        [Description("Unify Marketslib CL new commercial auto flow — existing account, commercial auto quote submitted to rates")]
        public async Task UNIFY_MarketsLib_CLAuto_E2E_SubmitToRates()
        {
            ScopeContext.Set(ctx => ctx.FrontEnd, FrontEndType.ADBX);

            var user = TestContextAccessor.CurrentUserCollection.MarketLibAgent
                ?? throw new TestSetupException("UNIFY Staging MarketLibAgent user not configured");

            var address = AddressData.MN;

            await _logger.ExecuteStepAsync("Start a Commercial CL quote", async () =>
            {
                await _adbxHelper.StartCommercialQuoteFromExistingAccountAsync(user);
            });

            // Reusable CL Auto defaults from TestDataProvider; only per-test fields overridden below.
            var formData = CLAutoFormData.Defaults;
            formData[FieldNames.InterviewAddress] = $"{address.AddressLine1}, {address.City}, {address.State}, {address.ZipCode}";
            formData[FieldNames.OrganizationName] = "Test Automation";
            formData[FieldNames.EposNaicDescription] = "Auto Hauling, Long-Distance";
            formData[FieldNames.CLOperatorLicenseState] = "Alabama";
            formData[FieldNames.TruckSubCategory] = "Auto Hauler";
            // CLAutoFormData.Defaults' split-limit values target Product_CLPolicyPage's option text and
            // don't match ProductCommercialAutoCoveragesPageCL's actual dropdown options for this tenant.
            formData[FieldNames.CL_BIPD] = "$100,000 CSL";
            // Progressive declines the risk if UM/UIM Bodily Injury is left unanswered while BI/PD is set.
            formData[FieldNames.CombinedUninsuredUnderinsuredMotorist] = "$50,000/$100,000";
            formData[FieldNames.CL_PIP] = "$20,000 Medical Expense/$20,000 Economic Loss";
            formData[FieldNames.CL_CollisionDeductible] = "100";

            var resultsPage = await Executor.ExecuteToPage<Product_ResultsPage>(
                FlowType.InterviewCLAutoFlowNew,
                PageFactory.CreatePage<ProductBusinessProfilePageCL>(),
                formData,
                fillForms: true,
                pageCallbacks: PageCallbackManager.For<ProductSelectionPageCL>(async page =>
                {
                    await page.SelectLob(LobType.CommercialAuto.ToToken());
                }, PageCallbackManager.CallbackTiming.AfterFillForm),
                // Progressive is one of the eligible markets for this risk; its extra questions page
                // renders after Market Selections and isn't part of the base flow's page list.
                pagesToAdd: [new(typeof(ProductAdditionalQuestionsProgressivePageCL), After: typeof(ProductMarketSelectionsPageCL))]
                );

            await _logger.ExecuteStepAsync("Verify rates returned", async () =>
            {
                var rates = await _interviewHelper.LogRatesBusinessRule(resultsPage);
                Assert.That(rates.Admitted, Is.Not.Empty, "No rates returned on the result page");
            });
        }
        #region UNIFY Staging - Sales Environment, new CL flow

        [Test]
        [Tenant(UNIFY)]
        [Category("Interview")][Category("Quoting")][Category("CL")][Category("Sanity")]
        [Author(Author.Helen)]
        [RunIn(BoltEnvironment.Staging)]
        [TestCaseId(253181)]
        [Description("Unify sales-environment agent takes a BOP quote for a new commercial account through the new CL flow to rates, then checks the ACORD tab lists forms 125, 126 and 140 and that downloading them serves a non-empty PDF")]
        public async Task UNIFY_SalesEnvironment_CL_BOP_NewAccount_To_Rates_E2E()
        {
            var businessProfilePage = await _salesEnvHelper.StartCommercialQuoteAsync();

            var resultsPage = await _logger.ExecuteStepAsync(
                "Confirm the business profile, select the BOP product and answer every CL page through to the results page",
                async () => await Executor.ExecuteToPage<Product_ResultsPage>(
                    FlowType.InterviewBOPFlow,
                    currentPage: businessProfilePage,
                    new Dictionary<string, string>
                    {
                        [FieldNames.InterviewAddress] = SalesEnvironmentCLTestHelper.Address,
                        [FieldNames.EposNaicDescription] = SalesEnvironmentCLTestHelper.Industry,
                        [FieldNames.OrganizationName] = NameSelector.GetCompanyName(),
                    },
                    fillForms: true,
                    // Travelers is the only carrier this profile draws, and it asks.
                    pagesToAdd: [new(typeof(ProductAdditionalQuestionsTravelersPageCL), After: typeof(ProductMarketSelectionsPageCL))],
                    pageCallbacks: PageCallbackManager.For<ProductSelectionPageCL>(async page =>
                    {
                        await page.SelectLob(LobType.BusinessOwners.ToToken());
                    }, PageCallbackManager.CallbackTiming.AfterFillForm)),
                "Expected result: Every CL page is answered and the quote reaches the results page");

            await _logger.ExecuteStepAsync("Wait out rating and check at least one carrier returned a rate", async () =>
            {
                // Carriers land one at a time; reading before rating finishes sees only the fast failures.
                await resultsPage.WaitForRatingToComplete();
                var rates = await _interviewHelper.LogRatesBusinessRule(resultsPage);

                Assert.That(rates.Admitted, Is.Not.Empty, "Failed. No carrier returned a rate on the results page");
                Assert.That(rates.Admitted, Has.Some.Contains("Travelers").IgnoreCase,
                    $"Failed. TC 253181 expects a Travelers rate; admitted carriers were [{string.Join(", ", rates.Admitted)}]");
            }, "Expected result: At least one carrier rates, and Travelers is among them");

            await _logger.ExecuteStepAsync("Open the ACORD tab and check forms 125, 126 and 140 are listed", async () =>
            {
                await resultsPage.ClickMarketCategoryTab("ACORD");
                var acordForms = await resultsPage.GetAcordFormsList();

                Assert.Multiple(() =>
                {
                    Assert.That(acordForms, Has.Some.Contains("125"), $"ACORD Form 125 not found; listed forms were [{string.Join(", ", acordForms)}]");
                    Assert.That(acordForms, Has.Some.Contains("126"), $"ACORD Form 126 not found; listed forms were [{string.Join(", ", acordForms)}]");
                    Assert.That(acordForms, Has.Some.Contains("140"), $"ACORD Form 140 not found; listed forms were [{string.Join(", ", acordForms)}]");
                });
            }, "Expected result: The ACORD tab lists forms 125, 126 and 140");

            await _logger.ExecuteStepAsync("Download all ACORD forms and check a non-empty PDF was served", async () =>
            {
                var downloadHelper = new FileDownloadValidatorHelper(BrowserManager.GetCurrentTab()!, _logger);

                var result = await downloadHelper.ValidateFileDownload(
                    downloadTriggerAction: async () => await _pageHelper!.InteractWithField(FieldNames.DownloadAllFormsButton),
                    expectedFileExtension: ".pdf");

                Assert.That(result.FileSizeBytes, Is.GreaterThan(0), "Downloaded ACORD forms PDF is empty");
            }, "Expected result: A non-empty PDF of the ACORD forms is downloaded");
        }

        [Test]
        [Tenant(UNIFY)]
        [Category("Interview")][Category("Quoting")][Category("CL")][Category("Sanity")]
        [Author(Author.Helen)]
        [RunIn(BoltEnvironment.Staging)]
        [TestCaseId(253224)]
        [Description("Unify sales-environment agent takes a commercial-auto quote for a new commercial account through the new CL flow to rates, checks Progressive rated, and that every carrier card offers the same finalize action")]
        public async Task UNIFY_SalesEnvironment_CL_Auto_NewAccount_To_Rates_E2E()
        {
            var businessProfilePage = await _salesEnvHelper.StartCommercialQuoteAsync();

            var resultsPage = await _logger.ExecuteStepAsync(
                "Confirm the business profile, select the commercial-auto product and answer every CL page through to the results page",
                async () => await Executor.ExecuteToPage<Product_ResultsPage>(
                    FlowType.InterviewBoltAccessCLAutoFlow,
                    currentPage: businessProfilePage,
                    new Dictionary<string, string>
                    {
                        [FieldNames.InterviewAddress] = SalesEnvironmentCLTestHelper.Address,
                        [FieldNames.EposNaicDescription] = SalesEnvironmentCLTestHelper.Industry,
                        [FieldNames.OrganizationName] = NameSelector.GetCompanyName(),
                        // The account arrives with a vehicle prefilled, so Year/Make/Model are not set here.
                        [FieldNames.CL_LengthVehicleOwnership] = "Less than 1 month",
                        [FieldNames.PrimaryUseOfVehicle] = "Business Only",
                        [FieldNames.VehicleAverageDailyTrips] = "12",
                        [FieldNames.CL_CurrentVehicleValue] = "45000",
                        // The registry defaults are not options for this tenant; these are KLX-proven.
                        [FieldNames.CL_BIPD] = "$100,000 CSL",
                        [FieldNames.CombinedUninsuredUnderinsuredMotorist] = "$100,000",
                        [FieldNames.UninsuredMotoristPropertyDamage] = "100,000",
                    },
                    fillForms: true,
                    // No pagesToAdd: carrier-questions pages follow the carrier, and neither of this
                    // profile's asks. Recheck if the returned carrier set changes.
                    pageCallbacks: PageCallbackManager.For<ProductSelectionPageCL>(async page =>
                    {
                        await page.SelectLob(LobType.CommercialAuto.ToToken());
                    }, PageCallbackManager.CallbackTiming.AfterFillForm)),
                "Expected result: Every CL page is answered and the quote reaches the results page");

            await _logger.ExecuteStepAsync("Wait out rating and check Progressive returned a rate", async () =>
            {
                await resultsPage.WaitForRatingToComplete();
                var rates = await _interviewHelper.LogRatesBusinessRule(resultsPage);

                Assert.That(rates.Admitted, Is.Not.Empty, "Failed. No carrier returned a rate on the results page");
                Assert.That(rates.Admitted, Has.Some.Contains("Progressive").IgnoreCase,
                    $"Failed. TC 253224 expects a Progressive rate; admitted carriers were [{string.Join(", ", rates.Admitted)}]");
            }, "Expected result: At least one carrier rates, and Progressive is among them");

            await _logger.ExecuteStepAsync("Check every carrier card offers the same finalize action", async () =>
            {
                var buttonTexts = await resultsPage.GetCarrierActionButtonTexts();

                Assert.That(buttonTexts, Is.Not.Empty, "No carrier cards were found on the results page");
                // TC 253224 words this as "complete application"; the button the product renders reads
                // "Finalize with carrier", which is the wording confirmed as correct.
                Assert.That(buttonTexts, Is.All.Contains("Finalize with carrier"),
                    $"Every carrier card should offer 'Finalize with carrier'; the buttons read [{string.Join(" | ", buttonTexts)}]");
            }, "Expected result: Every carrier's action button reads 'Finalize with carrier'");
        }

        [Test]
        [Tenant(UNIFY)]
        [Category("Interview")][Category("Quoting")][Category("CL")][Category("ADBX")][Category("Sanity")]
        [Author(Author.Helen)]
        [RunIn(BoltEnvironment.Staging)]
        [TestCaseId(253208)]
        [Description("Unify sales-environment agent takes a BOP quote for a new commercial account to rates, hands off to the quote timeline in ADBX, and records a Sold note that creates a policy binder for Travelers")]
        public async Task UNIFY_SalesEnvironment_CL_BOP_PolicyBinder_E2E()
        {
            var businessProfilePage = await _salesEnvHelper.StartCommercialQuoteAsync();
            var resultsPage = await _logger.ExecuteStepAsync(
                "Confirm the business profile, select the BOP product and answer every CL page through to the results page",
                async () => await Executor.ExecuteToPage<Product_ResultsPage>(
                    FlowType.InterviewBOPFlow,
                    currentPage: businessProfilePage,
                    new Dictionary<string, string>
                    {
                        [FieldNames.InterviewAddress] = SalesEnvironmentCLTestHelper.Address,
                        [FieldNames.EposNaicDescription] = SalesEnvironmentCLTestHelper.Industry,
                        [FieldNames.OrganizationName] = NameSelector.GetCompanyName(),
                    },
                    fillForms: true,
                    // Travelers is the only carrier this profile draws, and it asks.
                    pagesToAdd: [new(typeof(ProductAdditionalQuestionsTravelersPageCL), After: typeof(ProductMarketSelectionsPageCL))],
                    pageCallbacks: PageCallbackManager.For<ProductSelectionPageCL>(async page =>
                    {
                        await page.SelectLob(LobType.BusinessOwners.ToToken());
                    }, PageCallbackManager.CallbackTiming.AfterFillForm)),
                "Expected result: Every CL page is answered and the quote reaches the results page");

            await _logger.ExecuteStepAsync("Wait out rating and check Travelers returned a rate", async () =>
            {
                await resultsPage.WaitForRatingToComplete();
                var rates = await _interviewHelper.LogRatesBusinessRule(resultsPage);

                Assert.That(rates.Admitted, Has.Some.Contains("Travelers").IgnoreCase,
                    $"Failed. TC 253208 binds a Travelers policy; admitted carriers were [{string.Join(", ", rates.Admitted)}]");
            }, "Expected result: Travelers returns a rate");

            var quoteSummaryPage = await _logger.ExecuteStepAsync("Click Notes/Cases and land on this quote's timeline in ADBX",
                async () => await resultsPage.ClickNotesAndCases(),
                "Expected result: The quote's timeline is displayed in ADBX");

            // Owned here: the registry default is a per-process static, so a run would reuse one number.
            var policyNumber = "AutoBinder" + RandomManager.GetRandomString(4) + RandomManager.GetRandomDigits(3);

            await _adbxHelper.RecordSoldNoteAsync(quoteSummaryPage, new Dictionary<string, string>
            {
                [ADBX_FieldNames.PolicyProduct] = LobType.BusinessOwners.ToToken(),
                [ADBX_FieldNames.PolicyCarrier] = "Travelers",
                [ADBX_FieldNames.PolicyTerm] = "6 months",
                [ADBX_FieldNames.PolicyNumber] = policyNumber,
                [ADBX_FieldNames.PolicyPremium] = "2000",
            });

            await _logger.ExecuteStepAsync("Check the policy binder exists", async () =>
            {
                var binderId = await _mainQueries!.Policy.GetPolicyBinderIdByExPolicyNumberAsync(policyNumber);

                // The query returns a non-nullable Guid, so "no binder" is Guid.Empty, not null.
                Assert.That(binderId, Is.Not.EqualTo(Guid.Empty),
                    $"Failed. No policy binder was created for policy number {policyNumber}");
            }, "Expected result: A policy binder exists for the saved policy number");
        }

        [Test]
        [RunIn(BoltEnvironment.Staging)]
        [Tenant(UNIFY)]
        [Category("CL")]
        [Category("ADBX")]
        [Category("Quoting")]
        [Category("Sanity")]
        [Author(Author.Andrii)]
        [TestCaseId(253185)]
        [Description("Unify SalesEnvironment CL new WC flow — existing account, WC quote submitted to rates, add via Notes/Cases > New Note (Sold) > Add Policy")]
        public async Task UNIFY_SalesEnvironment_CL_WC_ExistingAccount_ToPolicyBinder_E2E()
        {
            var policyFormData = new Dictionary<string, string>
            {
                [PolicyProduct] = LobType.WorkersCompensation.ToToken(),
                [PolicyCarrier] = "Travelers",
                [PolicyTerm] = "6 months",
                [PolicyNumber] = "WCBinder" + RandomManager.GetRandomString(4) + RandomManager.GetRandomDigits(4),
                [PolicyPremium] = "100",
            };

            var user = TestContextAccessor.CurrentUserCollection.SalesEnvironmentAdmin
                ?? throw new TestSetupException("Unify Staging SalesEnvironmentAdmin user not configured");
            var userFullName = $"{user.FirstName} {user.LastName}";

            var businessProfilePage = await _salesEnvHelper.StartCommercialQuoteFromExistingAccountAsync();

            var resultsPage = await _logger.ExecuteStepAsync("Select WC LOB and execute the interview flow to the result page", async () =>
            {
                return await Executor.ExecuteToPage<Product_ResultsPage>(
                    FlowType.InterviewWCFlowNew,
                    businessProfilePage,
                    CLWCFormData.Defaults,
                    fillForms: true,
                    pagesToAdd:
                    [
                        new(typeof(ProductAdditionalQuestionsTravelersPageCL), After: typeof(ProductMarketSelectionsPageCL))
                    ],
                    pageCallbacks: PageCallbackManager.For<ProductSelectionPageCL>(async page =>
                    {
                        await page.SelectLob(LobType.WorkersCompensation.ToToken());
                    }, PageCallbackManager.CallbackTiming.AfterFillForm)
                    .And<ProductMarketSelectionsPageCL>(async page =>
                    {
                        // Travelers only: quoting all eligible adds a questions page per carrier.
                        await _pageHelper!.InteractWithField(FieldNames.QuoteAllEligible, "false");
                        await page.SelectCarrierAsync("Travelers");
                    }, PageCallbackManager.CallbackTiming.AfterFillForm));
            }, "Expected result: Product Selection page shows WC lob selected; flow continues through Locations/Employee pages to the Result page with at least one rate");

            var quoteSummaryPage = await _logger.ExecuteStepAsync("Click Notes/Cases and land on this quote's timeline in ADBX",
                async () => await resultsPage.ClickNotesAndCases(),
                "Expected result: The quote's timeline is displayed in ADBX");

            await _adbxHelper.RecordSoldNoteAsync(quoteSummaryPage, policyFormData);

            await _logger.ExecuteStepAsync("Verify the policy binder was created with the submitted policy details", async () =>
            {
                var expectedEffectiveDate = DateTime.Today.ToString("MM/dd/yyyy");
                var expectedExpirationDate = DateTime.Today.AddMonths(6).ToString("MM/dd/yyyy");
                var expectedPremium = $"${policyFormData[PolicyPremium]}.00";

                var policyPage = PageFactory.CreatePage<ADBX_PolicySummaryPage>();
                var policyDetails = await policyPage.GetSummaryData();
                var pageTitle = await _pageHelper.GetFieldValue(FieldRegistryADBX.Fields[PageTitle]);
                var actualPolicyNumber = pageTitle.Replace("Policy ", "");

                Assert.Multiple(() =>
                {
                    Assert.That(actualPolicyNumber, Is.EqualTo(policyFormData[PolicyNumber]),
                        $"Displayed policy number should match the submitted policy number. Actual: {actualPolicyNumber}, expected :{policyFormData[PolicyNumber]}");
                    Assert.That(policyDetails["Product"], Is.EqualTo(policyFormData[PolicyProduct]),
                        $"Policy product should match the submitted product. Actual: {policyDetails["Product"]}, expected :{policyFormData[PolicyProduct]}");
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

        [Test]
        [RunIn(BoltEnvironment.Staging)]
        [Tenant(UNIFY)]
        [Category("CL")]
        [Category("ADBX")]
        [Category("Quoting")]
        [Category("Sanity")]
        [Author(Author.Andrii)]
        [TestCaseId(253222)]
        [Description("Unify Sales Environment CL new WC flow — new account, WC quote submitted to rates")]
        public async Task UNIFY_SalesEnvironment_CL_WC_NewAccount_To_Rates_E2E()
        {
            var wcQuoteData = CLWCFormData.Defaults;

            var startPage = await _salesEnvHelper.StartCommercialQuoteAsync();

            var insuranceHistoryPage = await _logger.ExecuteStepAsync("Select WC lob and confirm the market appetite panel is absent on Market Availability", async () =>
            {
                return await Executor.ExecuteToPage<ProductInsuranceHistoryPageCL>(
                    FlowType.InterviewWCFlowNew,
                    startPage,
                    wcQuoteData,
                    fillForms: true,
                    pageCallbacks: PageCallbackManager.For<ProductSelectionPageCL>(async page =>
                    {
                        await page.SelectLob(LobType.WorkersCompensation.ToToken());
                    }, PageCallbackManager.CallbackTiming.AfterFillForm)
                    .And<ProductMarketResultsPageCL>(async _ =>
                    {
                        var cnaVisible = await _interviewHelper.IsCarrierVisible("CNA");
                        Assert.That(cnaVisible, Is.True, "Expected at least one carrier (CNA) on the Market Availability page");

                        // Short timeout: ElementExists burns the full budget when the element is absent.
                        var panelVisible = await _pageHelper!.ElementExists(FieldNames.MarketAppetitePanel, timeout: 500);
                        Assert.That(panelVisible, Is.False, "Market appetite panel should not appear on the Market Availability page");
                    }, PageCallbackManager.CallbackTiming.AfterFillForm));
            }, "Expected result: Product Selection page shows WC lob selected; Market Availability shows carriers with no market appetite panel");

            var resultsPage = await _logger.ExecuteStepAsync("Confirm the market appetite panel on Insurance History, confirm ACORD generation, and drive through ACORD 130 to the result page", async () =>
            {
                return await Executor.ExecuteToPage<Product_ResultsPage>(
                    FlowType.InterviewWCFlowNew,
                    insuranceHistoryPage,
                    wcQuoteData,
                    fillForms: true,
                    pagesToAdd:
                    [
                        new(typeof(ProductAdditionalQuestionsCNAPageCL), After: typeof(ProductMarketSelectionsPageCL)),
                        new(typeof(ProductAdditionalQuestionsEmployersPageCL), After: typeof(ProductAdditionalQuestionsCNAPageCL)),
                        new(typeof(ProductAdditionalQuestionsLibertyMutualPageCL), After: typeof(ProductAdditionalQuestionsEmployersPageCL)),
                        new(typeof(ProductAdditionalQuestionsTravelersPageCL), After: typeof(ProductAdditionalQuestionsLibertyMutualPageCL)),
                        new(typeof(ProductACORD130PageCL), After: typeof(ProductAdditionalQuestionsTravelersPageCL))
                    ],
                    pageCallbacks: PageCallbackManager.For<ProductInsuranceHistoryPageCL>(async _ =>
                    {
                        var panelVisible = await _pageHelper!.ElementExists(FieldNames.MarketAppetitePanel, timeout: 15000);
                        Assert.That(panelVisible, Is.True, "Market appetite panel should appear on the Insurance History page");
                    }, PageCallbackManager.CallbackTiming.AfterFillForm)
                    .And<ProductMarketSelectionsPageCL>(async page =>
                    {
                        await page.EnsureQuoteAllEligibleSelectedAsync();
                        await _pageHelper!.InteractWithField(FieldNames.AcordAppetiteWC, "Yes, include and generate ACORD");
                    }, PageCallbackManager.CallbackTiming.AfterFillForm));
            }, "Expected result: Market appetite panel appears on Insurance History; flow continues through the ACORD 130 page to the Result page with at least one rate");

            await _logger.ExecuteStepAsync("Verify at least one carrier returned a rate", async () =>
            {
                var rates = await _interviewHelper.LogRatesBusinessRule(resultsPage);
                Assert.That(rates.Admitted, Is.Not.Empty, "No rates returned on the result page");
            });
        }

        #endregion
    }
}
