using Bolt.Automation.ApiClients.GetQuoteApi.Interfaces;
using Bolt.Automation.ApiClients.Infrastructure;
using Bolt.Automation.ApiClients.SSO;
using Bolt.Automation.Common;
using Bolt.Automation.Common.Enums;
using Bolt.Automation.Common.Logging.Extentions;
using Bolt.Automation.Common.PollyRetry;
using Bolt.Automation.ExternalServices.LaunchDarkly;
using Bolt.Automation.FrontEnds.FormData.Common;
using Bolt.Automation.FrontEnds.Projects.HQXAgent.Flows;
using Bolt.Automation.FrontEnds.Projects.HQXAgent.FormData;
using Bolt.Automation.FrontEnds.Projects.HQXAgent.FormData.FieldsRegistry;
using Bolt.Automation.FrontEnds.Projects.HQXAgent.Pages;
using Bolt.Automation.TestDataProvider.Context;
using Bolt.Automation.TestDataProvider.Providers.GetQuoteApiDataProvider;
using Bolt.Automation.TestDataProvider.TestData.CommonTestData;
using Bolt.Automation.TestDataProvider.TestData.CoverageModificationTestData;
using Bolt.Automation.Tests.TestData.Progressive;
using Bolt.Automation.Tests.TestExtension.Attributes;
using Bolt.Automation.Tests.TestExtension.Base;
using Bolt.Automation.Tests.TestHelpers;
using Bolt.Automation.Tests.TestHelpers.Progressive;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using AuthorAttribute = Bolt.Automation.Tests.TestExtension.Attributes.AuthorAttribute;
using static Bolt.Automation.Common.Tenant;
using static Bolt.Automation.TestDataProvider.TestData.ApplicationTestData.ApplicationTestData;

namespace Bolt.Automation.Tests.Tests.Progressive
{
    public class PaaTests : UITestBase
    {
        private IGetQuoteApi _getQuoteApi = null!;
        private ISsoApiFactory _ssoApiFactory = null!;
        private IFeatureFlagService _featureFlag = null!;
        private IPollyRetryService _pollyRetryService = null!;

        private PaaTestHelper _paaHelper = null!;

        // Carrier Questions and Carrier Bridge assertions used to flap because Progressive's ranking
        // returns a non-deterministic carrier set. US 250713 fixed that: each case forces its own carrier
        // through PolicyData.CustomFields.RankingCarrierOverrides (a COMMA-SEPARATED string - Submission
        // splits it in RankingCarrierOverride.ParseRequestedCarriers, an array is silently ignored), which
        // requires LaunchDarkly's 'ranking-mock-mode' to be 'carrier-override' for the environment.
        // Carriers are matched against the quote's APPETITE naming, not the CarrierEnums name - e.g. FL
        // appetite carries TowerHillExchangeInsurance, so a TowerHill override finds nothing and the case
        // falls back to normal ranking.

        public PaaTests() : base()
        {
            ScopeContext.Set(ctx => ctx.FrontEnd, FrontEndType.HQXAgent);
        }

        protected override void ResolveServices()
        {
            var refitApiLocator = _uiTestScope.ServiceProvider.GetRequiredService<RefitApiServiceLocator>();
            _getQuoteApi = refitApiLocator.GetRequiredService<IGetQuoteApi>();
            _ssoApiFactory = _testScope.ServiceProvider.GetRequiredService<ISsoApiFactory>();
            _featureFlag = _testScope.ServiceProvider.GetRequiredService<IFeatureFlagService>();
            _pollyRetryService = _testScope.ServiceProvider.GetRequiredService<IPollyRetryService>();
        }

        protected override void InitializeComponents()
        {
            _paaHelper = new PaaTestHelper(
                _getQuoteApi, _ssoApiFactory, Executor, PageFactory, BrowserManager, ScopeContext, TestContextAccessor);
        }


        [Test]
            [RunIn(includeProduction: true)]
            [Tenant(PROGRESSIVEPL)]
            [Author(Author.Helen)]
            [TestCaseSource(typeof(PaaTestCases), nameof(PaaTestCases.CarrierQuestionsHomeOwnersCases))]
            [Category("PAA")]
            [Category("CarrierQuestions")]
            [Category("Carriers")]
            [Description("Validates that the correct carrier specific questions are displayed for Homeowners flow based on carrier and address")]
            public async Task PGR_PAA_Carrier_Specific_Questions_Validation_HO3(
                CarrierEnums carrier,
                AddressKey addressKey)
            {

                var address = AddressData.GetAddress(addressKey);
                var expectedQuestions = CarrierQuestionsHelper.GetHomeOwnersExpectedQuestions(carrier, addressKey);

                var data = await _logger.ExecuteStepAsync("Setup test data and user context", async () =>
                {
                    _paaHelper.SetAdminUserContext();
                    var data = PersonalLineDataProvider.GetPersonalDataProgressive(
                        address, rankingCarrierOverrides: [carrier]);
                    data.YearsAtAddress = 0;
                    data.MonthsAtAddress = 0;
                    if (carrier == CarrierEnums.Bamboo)
                    {
                        data.PLYearBuilt = 1990;
                        data.PL_RoofUpdateYearRange = "TwentyToTwentyNine";
                        data.PlumbingUpdatedYear = 2000;
                    }
                    return data;
                });

                await _logger.ExecuteStepAsync("Create application and navigate through SSO", async () =>
                {
                    await _paaHelper.CreateApplicationAndSetRelayStateAsync(data);
                    await _paaHelper.NavigateThroughSsoToOverviewAsync();
                });

                await _logger.ExecuteStepAsync($"Navigate to Selected Carrier page and select {carrier}", async () =>
                {
                    var selectedCarrier = await Executor.ExecuteToPage<HQXAgent_SelectedCarrierPage>(
                        FlowType.PgrHomeFlow,
                        PageFactory.CreatePage<HQXAgent_OverviewPage>(),
                        fillForms: false);
                    await _paaHelper.SelectCarrierAndContinueAsync(selectedCarrier, carrier);
                });

                await _logger.ExecuteStepAsync("Complete Prefill Verification", async () =>
                {
                    var prefillVerification = PageFactory.CreatePage<HQXAgent_PrefillVerificationPage>();
                    await prefillVerification.ClickContinue();
                });

                var actualQuestions = await _logger.ExecuteStepAsync("Extract carrier questions", async () =>
                {
                    var carrierQuestions = PageFactory.CreatePage<HQXAgent_CarrierQuestionsPage>();
                    var bankruptcy = CarrierQuestionsFields.Fields[FieldNamesHQXAgent.PL_Bankruptcy];
                    if (await _pageHelper.ElementExists(bankruptcy))
                    {
                        await _pageHelper.InteractWithElement(bankruptcy);
                    }
                    var actualQuestions = await carrierQuestions.ExtractCarrierQuestionsAsync();
                    return actualQuestions;
                });

                await _logger.ExecuteStepAsync("Validate carrier questions", async () =>
                {
                    var (missing, extra) = CarrierQuestionsHelper.GetCarrierQuestionsDiff(expectedQuestions, actualQuestions, _logger);
                    Assert.That(missing.Count == 0 && extra.Count == 0, Is.True,
                        $"Carrier questions mismatch. Missing: {string.Join(", ", missing)} " +
                        $"| Extra: {string.Join(", ", extra)}");
                });
            }


        [Test]
            [RunIn(includeProduction: true)]
            [Tenant(PROGRESSIVEPL)]
            [Author(Author.Helen)]
            [TestCaseSource(typeof(PaaTestCases), nameof(PaaTestCases.CarrierQuestionsDwellingFireCases))]
            [Category("PAA")]
            [Category("CarrierQuestions")]
            [Category("Carriers")]
            [Description("Validates that the correct carrier specific questions are displayed for Dwelling Fire flow based on carrier and address")]
            public async Task PGR_PAA_Carrier_Specific_Questions_Validation_DF(
                CarrierEnums carrier,
                AddressKey addressKey)
            {

                var address = AddressData.GetAddress(addressKey);
                var expectedQuestions = CarrierQuestionsHelper.DwellingFireExpectedQuestions[carrier];

                var data = await _logger.ExecuteStepAsync("Setup test data and user context", async () =>
                {
                    _paaHelper.SetAdminUserContext();
                    var data = PersonalLineDataProvider.GetPersonalDataProgressive(
                        address, null, HomeDetailsTestData.PGRDFDetails, rankingCarrierOverrides: [carrier]);
                    return data;
                });

                await _logger.ExecuteStepAsync("Create application and navigate through SSO", async () =>
                {
                    await _paaHelper.CreateApplicationAndSetRelayStateAsync(data);
                    await _paaHelper.NavigateThroughSsoToOverviewAsync();
                });

                await _logger.ExecuteStepAsync($"Navigate to Selected Carrier page and select {carrier}", async () =>
                {
                    var selectedCarrier = await Executor.ExecuteToPage<HQXAgent_SelectedCarrierPage>(
                        FlowType.PgrHomeFlow,
                        PageFactory.CreatePage<HQXAgent_OverviewPage>(),
                        fillForms: false);
                    await selectedCarrier.ClickOnSpecificCarrier(carrier.ToString());
                    await selectedCarrier.ClickContinue();
                });

                await _logger.ExecuteStepAsync("Complete Prefill Verification", async () =>
                {
                    var prefillVerification = PageFactory.CreatePage<HQXAgent_PrefillVerificationPage>();
                    await prefillVerification.ClickContinue();
                });

                var actualQuestions = await _logger.ExecuteStepAsync("Extract carrier questions", async () =>
                {
                    var carrierQuestions = PageFactory.CreatePage<HQXAgent_CarrierQuestionsPage>();
                    var actualQuestions = await carrierQuestions.ExtractCarrierQuestionsAsync();
                    return actualQuestions;
                });

                await _logger.ExecuteStepAsync("Validate carrier questions", async () =>
                {
                    var (missing, extra) = CarrierQuestionsHelper.GetCarrierQuestionsDiff(expectedQuestions, actualQuestions, _logger);
                    Assert.That(missing.Count == 0 && extra.Count == 0, Is.True,
                        $"Carrier questions mismatch. Missing: {string.Join(", ", missing)} " +
                        $"| Extra: {string.Join(", ", extra)}");
                });
            }


        [Test]
            [RunIn(includeProduction: true)]
            [Tenant(PROGRESSIVEPL)]
            [Author(Author.Helen)]
            [TestCaseSource(typeof(PaaTestCases), nameof(PaaTestCases.CarrierQuestionsCondominiumCases))]
            [Category("PAA")]
            [Category("CarrierQuestions")]
            [Category("Carriers")]
            [Description("Validates that the correct carrier specific questions are displayed for Condominium (HO6) flow based on carrier and address")]
            public async Task PGR_PAA_Carrier_Specific_Questions_Validation_HO6(
                CarrierEnums carrier,
                AddressKey addressKey)
            {

                var address = AddressData.GetAddress(addressKey);
                var expectedQuestions = CarrierQuestionsHelper.CondominiumExpectedQuestions[carrier];

                var data = await _logger.ExecuteStepAsync("Setup test data and user context", async () =>
                {
                    _paaHelper.SetAdminUserContext();
                    var data = PersonalLineDataProvider.GetPersonalDataProgressive(
                        address, null, HomeDetailsTestData.PGRCondoDetails, rankingCarrierOverrides: [carrier]);
                    data.YearsAtAddress = 0;
                    return data;
                });

                await _logger.ExecuteStepAsync("Create application and navigate through SSO", async () =>
                {
                    await _paaHelper.CreateApplicationAndSetRelayStateAsync(data);
                    await _paaHelper.NavigateThroughSsoToOverviewAsync();
                });

                await _logger.ExecuteStepAsync("Complete Overview page", async () =>
                {
                    var overviewPage = PageFactory.CreatePage<HQXAgent_OverviewPage>();
                    var roofResponsibleField = FieldNames.RoofResponsible;
                    if (await _pageHelper!.ElementExists(roofResponsibleField))
                    {
                        await _pageHelper.InteractWithField(roofResponsibleField);
                    }
                    await overviewPage.ClickContinue();
                });

                await _logger.ExecuteStepAsync($"Navigate to Selected Carrier page and select {carrier}", async () =>
                {
                    var selectedCarrier = await Executor.ExecuteToPage<HQXAgent_SelectedCarrierPage>(
                        FlowType.PgrHomeFlow,
                        PageFactory.CreatePage<HQXAgent_TriagePage>(),
                        fillForms: false);
                    await selectedCarrier.ClickOnSpecificCarrier(carrier.ToString());
                    await selectedCarrier.ClickContinue();
                });

                await _logger.ExecuteStepAsync("Complete Prefill Verification", async () =>
                {
                    var prefillVerification = PageFactory.CreatePage<HQXAgent_PrefillVerificationPage>();
                    await prefillVerification.ClickContinue();
                });

                var actualQuestions = await _logger.ExecuteStepAsync("Extract carrier questions", async () =>
                {
                    var carrierQuestions = PageFactory.CreatePage<HQXAgent_CarrierQuestionsPage>();
                    var bankruptcy = CarrierQuestionsFields.Fields[FieldNamesHQXAgent.PL_Bankruptcy];
                    if (await _pageHelper.ElementExists(bankruptcy))
                    {
                        await _pageHelper.InteractWithElement(bankruptcy);
                    }
                    var actualQuestions = await carrierQuestions.ExtractCarrierQuestionsAsync();
                    return actualQuestions;
                });

                await _logger.ExecuteStepAsync("Validate carrier questions", async () =>
                {
                    var (missing, extra) = CarrierQuestionsHelper.GetCarrierQuestionsDiff(expectedQuestions, actualQuestions, _logger);
                    Assert.That(missing.Count == 0 && extra.Count == 0, Is.True,
                        $"Carrier questions mismatch. Missing: {string.Join(", ", missing)} " +
                        $"| Extra: {string.Join(", ", extra)}");
                });
            }

        [Test]
            [RunIn(includeProduction: true)]
            [Tenant(PROGRESSIVEPL)]
            [Author(Author.Helen)]
            [TestCaseSource(typeof(PaaTestCases), nameof(PaaTestCases.CarrierBridgeDwellingFireCases))]
            [Category("PAA")]
            [Category("CarrierBridge")]
            [Category("Carriers")]
            [Description("Validates that carrier bridge URL is correct for Dwelling Fire flow when navigating to external carrier site")]
            public async Task PGR_PAA_Carrier_Bridge_Validation_DF(
                CarrierEnums carrier,
                AddressKey addressKey)
            {

                var address = AddressData.GetAddress(addressKey);

                var data = await _logger.ExecuteStepAsync("Setup test data and user context", async () =>
                {
                    _paaHelper.SetAdminUserContext();
                    var data = PersonalLineDataProvider.GetPersonalDataProgressive(
                        address, null, HomeDetailsTestData.PGRDFDetails, rankingCarrierOverrides: [carrier]);
                    return data;
                });

                await _logger.ExecuteStepAsync("Create application and navigate through SSO", async () =>
                {
                    await _paaHelper.CreateApplicationAndSetRelayStateAsync(data);
                    await _paaHelper.NavigateThroughSsoToOverviewAsync();
                });

                await _logger.ExecuteStepAsync($"Navigate to Selected Carrier page and select {carrier}", async () =>
                {
                    var selectedCarrier = await Executor.ExecuteToPage<HQXAgent_SelectedCarrierPage>(
                        FlowType.PgrHomeFlow,
                        PageFactory.CreatePage<HQXAgent_OverviewPage>(),
                        fillForms: false);
                    await selectedCarrier.ClickOnSpecificCarrier(carrier.ToString());
                    await selectedCarrier.ClickContinue();
                });

                await _logger.ExecuteStepAsync("Complete Prefill Verification and Carrier Questions", async () =>
                {
                    var carrierQuestions = await Executor.ExecuteToPage<HQXAgent_CarrierQuestionsPage>(
                        FlowType.PgrHomeFlow,
                        PageFactory.CreatePage<HQXAgent_PrefillVerificationPage>(),
                        fillForms: false);
                    await _paaHelper.AnswerBridgeCarrierQuestionsAndContinueAsync(carrierQuestions, carrier);
                });

                await _logger.ExecuteStepAsync("Validate carrier bridge URL", async () =>
                {
                    await BrowserManager.SwitchToLastTabAsync();
                    var currentUrl = BrowserManager.GetCurrentTab()?.Url;
                    Assert.That(currentUrl, Does.Contain(TestContextAccessor.CarrierBridgeUrls.Urls[carrier]));
                });
            }

        [Test]
            [RunIn(includeProduction: true)]
            [Tenant(PROGRESSIVEPL)]
            [Author(Author.Helen)]
            [TestCaseSource(typeof(PaaTestCases), nameof(PaaTestCases.CarrierBridgeHomeOwnersCases))]
            [Category("PAA")]
            [Category("CarrierBridge")]
            [Category("Carriers")]
            [Description("Validates that carrier bridge URL is correct for Homeowners (HO3) flow when navigating to external carrier site")]
            public async Task PGR_PAA_Carrier_Bridge_Validation_HO3(
                CarrierEnums carrier,
                AddressKey addressKey)
            {

                var address = AddressData.GetAddress(addressKey);

                var data = await _logger.ExecuteStepAsync("Setup test data and user context", async () =>
                {
                    _paaHelper.SetAdminUserContext();
                    var data = PersonalLineDataProvider.GetPersonalDataProgressive(
                        address, rankingCarrierOverrides: [carrier]);
                    return data;
                });

                await _logger.ExecuteStepAsync("Create application and navigate through SSO", async () =>
                {
                    await _paaHelper.CreateApplicationAndSetRelayStateAsync(data);
                    await _paaHelper.NavigateThroughSsoToOverviewAsync();
                });

                await _logger.ExecuteStepAsync($"Navigate to Selected Carrier page and select {carrier}", async () =>
                {
                    var selectedCarrier = await Executor.ExecuteToPage<HQXAgent_SelectedCarrierPage>(
                        FlowType.PgrHomeFlow,
                        PageFactory.CreatePage<HQXAgent_OverviewPage>(),
                        fillForms: false);
                    await _paaHelper.SelectCarrierAndContinueAsync(selectedCarrier, carrier);
                });

                await _logger.ExecuteStepAsync("Complete Prefill Verification and Carrier Questions, then bridge to carrier", async () =>
                {
                    var carrierQuestions = await Executor.ExecuteToPage<HQXAgent_CarrierQuestionsPage>(
                        FlowType.PgrHomeFlow,
                        PageFactory.CreatePage<HQXAgent_PrefillVerificationPage>(),
                        fillForms: false);
                    await _paaHelper.AnswerBridgeCarrierQuestionsAndContinueAsync(carrierQuestions, carrier);
                });

                await _logger.ExecuteStepAsync("Validate carrier bridge URL", async () =>
                {
                    await BrowserManager.SwitchToLastTabAsync();
                    var currentUrl = BrowserManager.GetCurrentTab()?.Url;
                    Assert.That(currentUrl, Does.Contain(TestContextAccessor.CarrierBridgeUrls.Urls[carrier]));
                });
            }

        [Test]
            [RunIn(includeProduction: true)]
            [Tenant(PROGRESSIVEPL)]
            [Author(Author.Helen)]
            [TestCaseSource(typeof(PaaTestCases), nameof(PaaTestCases.CarrierQuestionsManufacturedHomeCases))]
            [Category("PAA")]
            [Category("CarrierQuestions")]
            [Category("Carriers")]
            [Description("Validates that the correct carrier specific questions are displayed for Manufactured Home flow based on carrier and address")]
            public async Task PGR_CarrierQuestions_MH(
                CarrierEnums carrier,
                AddressKey addressKey)
            {

                var address = AddressData.GetAddress(addressKey);
                var expectedQuestions = CarrierQuestionsHelper.ManufacturedHomeExpectedQuestions[carrier];

                var data = await _logger.ExecuteStepAsync("Setup test data and user context", async () =>
                {
                    _paaHelper.SetAdminUserContext();
                    var data = PersonalLineDataProvider.GetPersonalDataProgressive(
                        address, null, HomeDetailsTestData.PGRMHDetails, rankingCarrierOverrides: [carrier]);
                    data.PersonalLineReplacementCost = 113000;
                    data.CustomFields.HasWaterHeaterBeenReplaced = "true";
                    data.CustomFields.MHElectircalUpdated = "true";
                    return data;
                });

                await _logger.ExecuteStepAsync("Create application and navigate through SSO", async () =>
                {
                    await _paaHelper.CreateApplicationAndSetRelayStateAsync(data);
                    await _paaHelper.NavigateThroughSsoToOverviewAsync();
                });

                await _logger.ExecuteStepAsync($"Navigate to Selected Carrier page and select {carrier}", async () =>
                {
                    var selectedCarrier = await Executor.ExecuteToPage<HQXAgent_SelectedCarrierPage>(
                        FlowType.PgrHomeFlow,
                        PageFactory.CreatePage<HQXAgent_OverviewPage>(),
                        fillForms: false);
                    await selectedCarrier.ClickOnSpecificCarrier(carrier.ToString());
                    await selectedCarrier.ClickContinue();
                });

                await _logger.ExecuteStepAsync("Complete Prefill Verification", async () =>
                {
                    var prefillVerification = PageFactory.CreatePage<HQXAgent_PrefillVerificationPage>();
                    await prefillVerification.ClickContinue();
                });

                var actualQuestions = await _logger.ExecuteStepAsync("Extract carrier questions", async () =>
                {
                    var carrierQuestions = PageFactory.CreatePage<HQXAgent_CarrierQuestionsPage>();
                    var dealership = CarrierQuestionsFields.Fields[FieldNamesHQXAgent.DealershipPurchase];
                    if (await _pageHelper!.ElementExists(dealership))
                    {
                        await _pageHelper.InteractWithElement(dealership);
                    }
                    var actualQuestions = await carrierQuestions.ExtractCarrierQuestionsAsync();
                    return actualQuestions;
                });

                await _logger.ExecuteStepAsync("Validate carrier questions", async () =>
                {
                    var (missing, extra) = CarrierQuestionsHelper.GetCarrierQuestionsDiff(expectedQuestions, actualQuestions, _logger);
                    Assert.That(missing.Count == 0 && extra.Count == 0, Is.True,
                        $"Carrier questions mismatch. Missing: {string.Join(", ", missing)} " +
                        $"| Extra: {string.Join(", ", extra)}");
                });
            }

        [Test]
            [RunIn(includeProduction: true)]
            [Tenant(PROGRESSIVEPL)]
            [Author(Author.Helen)]
            [TestCaseId(246475)]
            [Category("PAA")]
            [Category("Coverages")]
            [Category("Carriers")]
            [Description("Validates that the correct coverages are displayed under 'View rates' on the Selected Carrier page for the E&S (Bamboo Surplus) Homeowners flow in TX")]
            public async Task PGR_PAA_Coverage_Display_Validation_ES_HO3()
            {
                // Coverage display is an E&S-only, single-carrier scenario (Bamboo Surplus). "LTPLow"
                // in the TC title is a Loss-To-Property-Low QA label with no data override — reuses
                // the default TX_Crowley profile.
                const CarrierEnums carrier = CarrierEnums.BambooSurplus;
                var address = AddressData.GetAddress(AddressKey.TX_Crowley);
                var expectedCoverages = CoverageDisplayData.SelectedCarrierCoverages[LOBEnums.HO3];
                var expectedCarrierName = KeyPointsData.CarrierDisplayNames[carrier];
                var expectedKeyPoints = KeyPointsData.SelectedCarrierKeyPoints[carrier];

                var data = await _logger.ExecuteStepAsync("Setup test data and user context", async () =>
                {
                    _paaHelper.SetAdminUserContext();
                    var data = PersonalLineDataProvider.GetPersonalDataProgressive(address);
                    return data;
                });

                await _logger.ExecuteStepAsync("Create application and navigate through SSO", async () =>
                {
                    await _paaHelper.CreateApplicationAndSetRelayStateAsync(data);
                    await _paaHelper.NavigateThroughSsoToOverviewAsync();
                });

                var selectedCarrier = await _logger.ExecuteStepAsync(
                    "Navigate to Selected Carrier page and press Get E&S Rates", async () =>
                {
                    var selectedCarrier = await Executor.ExecuteToPage<HQXAgent_SelectedCarrierPage>(
                        FlowType.PgrHomeFlow,
                        PageFactory.CreatePage<HQXAgent_OverviewPage>(),
                        fillForms: false);
                    await selectedCarrier.ClickGetESRatesButton();
                    return selectedCarrier;
                });

                await _logger.ExecuteStepAsync("View rates to expand the coverages panel", async () =>
                {
                    await selectedCarrier.ClickViewRatesAsync();
                });

                await _logger.ExecuteStepAsync("Validate E&S carrier name and key points", async () =>
                {
                    var carrierName = await selectedCarrier.GetSelectedCarrierNameAsync();
                    Assert.That(carrierName, Does.Contain(expectedCarrierName),
                        $"Carrier name mismatch. Expected to contain '{expectedCarrierName}' but was '{carrierName}'");

                    var (missing, extra) = await selectedCarrier.GetKeyPointsDiffAsync(expectedKeyPoints);
                    Assert.That(missing.Count == 0 && extra.Count == 0, Is.True,
                        $"E&S key-points text mismatch. Missing: {string.Join(" | ", missing)} " +
                        $"| Extra: {string.Join(" | ", extra)}");
                });

                await _logger.ExecuteStepAsync("Validate displayed coverages", async () =>
                {
                    var (missing, extra) = await selectedCarrier.GetCoverageTitlesDiffAsync(expectedCoverages);
                    Assert.That(missing.Count == 0 && extra.Count == 0, Is.True,
                        $"Coverage display mismatch. Missing: {string.Join(", ", missing)} " +
                        $"| Extra: {string.Join(", ", extra)}");
                });
            }

        [Test]
            [RunIn(includeProduction: true)]
            [Tenant(PROGRESSIVEPL)]
            [Author(Author.Helen)]
            [TestCaseId(246473)]
            [Category("PAA")]
            [Category("Coverages")]
            [Category("Carriers")]
            [Description("Validates the E&S (Bamboo Surplus) 'Not quoted' decline (UUDS) on the Selected Carrier page in TX when the estimated replacement cost pushes Coverage A above the carrier's allowable limits")]
            public async Task PGR_PAA_Not_Quoted_Validation_ES_HO3()
            {

                const CarrierEnums carrier = CarrierEnums.BambooSurplus;
                const int replacementCostAboveLimits = 1000060;
                var address = AddressData.GetAddress(AddressKey.TX_Crowley);
                var expectedReason = KeyPointsData.SelectedCarrierNotQuotedReason[carrier];

                var data = await _logger.ExecuteStepAsync("Setup test data and user context", async () =>
                {
                    _paaHelper.SetAdminUserContext();
                    var data = PersonalLineDataProvider.GetPersonalDataProgressive(address);
                    // Drive Coverage A above the E&S carrier's allowable limits so the carrier declines.
                    // Answered in Triage's "Estimated replacement cost" field as the flow walks the page.
                    data.PersonalLineReplacementCost = replacementCostAboveLimits;
                    return data;
                });

                await _logger.ExecuteStepAsync("Create application and navigate through SSO", async () =>
                {
                    await _paaHelper.CreateApplicationAndSetRelayStateAsync(data);
                    await _paaHelper.NavigateThroughSsoToOverviewAsync();
                });

                var selectedCarrier = await _logger.ExecuteStepAsync(
                    "Navigate to Selected Carrier page and press Get E&S Rates", async () =>
                {
                    var selectedCarrier = await Executor.ExecuteToPage<HQXAgent_SelectedCarrierPage>(
                        FlowType.PgrHomeFlow,
                        PageFactory.CreatePage<HQXAgent_OverviewPage>(),
                        fillForms: false);
                    await selectedCarrier.ClickGetESRatesExpectingDeclineAsync();
                    return selectedCarrier;
                });

                await _logger.ExecuteStepAsync("Validate the E&S 'Not quoted' decline and fallback link", async () =>
                {
                    Assert.That(await selectedCarrier.IsCarrierInNotQuotedBlockAsync(carrier.ToString()), Is.True,
                        $"Expected the E&S carrier ({carrier}) to appear in the 'Not quoted' block.");

                    var reasons = await selectedCarrier.GetNotQuotedReasonsAsync();
                    Assert.That(reasons, Has.Some.Contains(expectedReason),
                        $"Expected a 'Not quoted' reason containing '{expectedReason}'. Actual reasons: " +
                        $"{string.Join(" | ", reasons)}");

                    var dfField = SelectedCarrierFields.Fields[FieldNamesHQXAgent.GetDFRates];
                    Assert.That(await _pageHelper.ElementExists(dfField), Is.True,
                        "Expected the 'Get DF rates' fallback link to be shown in the decline block.");

                    var esField = SelectedCarrierFields.Fields[FieldNamesHQXAgent.GetESRates];
                    Assert.That(await _pageHelper.ElementExists(esField), Is.False,
                        "Expected the 'Get E&S rates' button to no longer be available after the E&S decline.");
                });
            }

        [Test]
            [RunIn(includeProduction: true)]
            [Tenant(PROGRESSIVEPL)]
            [Author(Author.Helen)]
            [TestCaseSource(typeof(PaaTestCases), nameof(PaaTestCases.CarrierBridgeManufacturedHomeCases))]
            [Category("PAA")]
            [Category("CarrierBridge")]
            [Category("Carriers")]
            [Description("Validates that carrier bridge URL is correct for Manufactured Home flow when navigating to external carrier site")]
            public async Task PGR_CarrierBridge_MH(
                CarrierEnums carrier,
                AddressKey addressKey)
            {

                var address = AddressData.GetAddress(addressKey);

                var data = await _logger.ExecuteStepAsync("Setup test data and user context", async () =>
                {
                    _paaHelper.SetAdminUserContext();
                    var data = PersonalLineDataProvider.GetPersonalDataProgressive(
                        address, null, HomeDetailsTestData.PGRMHDetails, rankingCarrierOverrides: [carrier]);
                    data.PersonalLineReplacementCost = 113000;
                    data.CustomFields.HasWaterHeaterBeenReplaced = "true";
                    data.CustomFields.MHElectircalUpdated = "true";
                    return data;
                });

                await _logger.ExecuteStepAsync("Create application and navigate through SSO", async () =>
                {
                    await _paaHelper.CreateApplicationAndSetRelayStateAsync(data);
                    await _paaHelper.NavigateThroughSsoToOverviewAsync();
                });

                await _logger.ExecuteStepAsync($"Navigate to Selected Carrier page and select {carrier}", async () =>
                {
                    var selectedCarrier = await Executor.ExecuteToPage<HQXAgent_SelectedCarrierPage>(
                        FlowType.PgrHomeFlow,
                        PageFactory.CreatePage<HQXAgent_OverviewPage>(),
                        fillForms: false);
                    await selectedCarrier.ClickOnSpecificCarrier(carrier.ToString());
                    await selectedCarrier.ClickContinue();
                });

                await _logger.ExecuteStepAsync("Complete Prefill Verification and Carrier Questions", async () =>
                {
                    var carrierQuestions = await Executor.ExecuteToPage<HQXAgent_CarrierQuestionsPage>(
                        FlowType.PgrHomeFlow,
                        PageFactory.CreatePage<HQXAgent_PrefillVerificationPage>(),
                        fillForms: false);
                    await _paaHelper.AnswerBridgeCarrierQuestionsAndContinueAsync(carrierQuestions, carrier);
                    await BrowserManager.SwitchToLastTabAsync();
                });

                await _logger.ExecuteStepAsync("Validate carrier bridge URL", async () =>
                {
                    var status = _featureFlag.GetFeatureStatus("assurant-bridge-url-update");
                    var currentUrl = BrowserManager.GetCurrentTab()?.Url;

                    if (carrier == CarrierEnums.Assurant && status == FeatureStatus.On)
                    {
                        await _pollyRetryService.ExecuteWithRetryAsync(async () =>
                        {
                            currentUrl = BrowserManager.GetCurrentTab()?.Url;
                            return currentUrl!.Contains("login.microsoftonline");
                        });
                    }
                    else
                    {
                        Assert.That(currentUrl, Does.Contain(TestContextAccessor.CarrierBridgeUrls.Urls[carrier]));
                    }
                });
            }

    }
}

