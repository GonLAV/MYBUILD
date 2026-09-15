using Bolt.Automation.ApiClients.GetQuoteApi.Interfaces;
using Bolt.Automation.ApiClients.Infrastructure;
using Bolt.Automation.ApiClients.PlatformApi;
using Bolt.Automation.ApiClients.SSO;
using Bolt.Automation.Common;
using Bolt.Automation.Common.Enums;
using Bolt.Automation.Common.Logging.Extentions;
using Bolt.Automation.FrontEnds.Projects.HQXAgent.Enums;
using Bolt.Automation.FrontEnds.Projects.HQXAgent.Pages;
using Bolt.Automation.FrontEnds.Projects.HQXConsumer.Flows;
using Bolt.Automation.FrontEnds.Projects.HQXConsumer.Pages;
using Bolt.Automation.InternalServices.Database.Queries.Main;
using Bolt.Automation.TestDataProvider.Context;
using Bolt.Automation.TestDataProvider.Providers.GetQuoteApiDataProvider;
using Bolt.Automation.TestDataProvider.Providers.PlatformAPIDataProvider;
using Bolt.Automation.TestDataProvider.TestData.CommonTestData;
using Bolt.Automation.TestDataProvider.TestData.PlatformAPITestData;
using Bolt.Automation.Tests.TestExtension.Attributes;
using Bolt.Automation.Tests.TestExtension.Base;
using Bolt.Automation.Tests.TestHelpers.Progressive;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using static Bolt.Automation.FrontEnds.FormData.Common.FieldNames;
using static Bolt.Automation.FrontEnds.Projects.HQXAgent.FormData.FieldNamesHQXAgent;
using AuthorAttribute = Bolt.Automation.Tests.TestExtension.Attributes.AuthorAttribute;
using PlaywrightValueType = Bolt.Automation.FrontEnds.PlaywrightBase.Helpers.ValueType;
using AgentFlowType = Bolt.Automation.FrontEnds.Projects.HQXAgent.Flows.FlowType;

namespace Bolt.Automation.Tests.Tests.Progressive
{
    public class PaaQuestionDisplayLogicTests : UITestBase
    {
        private IGetQuoteApi _getQuoteApi = null!;
        private ISsoApiFactory _ssoApiFactory = null!;
        private IPlatformApiClientFactory _platformApiClientFactory = null!;

        private PaaTestHelper _paaHelper = null!;
        private ProgressiveTestHelper _progressiveHelper = null!;

        public PaaQuestionDisplayLogicTests() : base()
        {
            ScopeContext.Set(ctx => ctx.FrontEnd, FrontEndType.HQXAgent);
        }

        protected override void ResolveServices()
        {
            var refitApiLocator = _uiTestScope.ServiceProvider.GetRequiredService<RefitApiServiceLocator>();
            _getQuoteApi = refitApiLocator.GetRequiredService<IGetQuoteApi>();
            _ssoApiFactory = _testScope.ServiceProvider.GetRequiredService<ISsoApiFactory>();
            _platformApiClientFactory = _uiTestScope.ServiceProvider.GetRequiredService<IPlatformApiClientFactory>();
        }

        protected override void InitializeComponents()
        {
            _paaHelper = new PaaTestHelper(
                _getQuoteApi, _ssoApiFactory, Executor, PageFactory, BrowserManager, ScopeContext, TestContextAccessor);
            _progressiveHelper = new ProgressiveTestHelper(
                _logger, ScopeContext, Executor, PageFactory, _platformApiClientFactory, BrowserManager);
        }

        [Test]
        [Tenant(Tenant.PROGRESSIVEPL)]
        [Category("PAA")]
        [Category("Regression")]
        [TestCaseId(243900)]
        [Author(Author.Helen)]
        [Description("Verifies that the 'Is the exterior of your home insured by someone else?' question is hidden on the Overview page and defaults to 'Yes' when the Home Style is Condo (new quote)")]
        public async Task PGR_PAA_Condo_NewQuote_ExteriorInsured_Question_Hidden_DefaultYes()
        {
            var address = AddressData.GetAddress(AddressKey.NY_Averill_Park);

            var data = await _logger.ExecuteStepAsync("Setup test data and agent user context", async () =>
            {
                _paaHelper.SetAdminUserContext();
                var data = PersonalLineDataProvider.GetPersonalDataProgressive(address);
                return data;
            });

            await _logger.ExecuteStepAsync("Create application, navigate through SSO, and select Condo on Customer Intro", async () =>
            {
                await _paaHelper.CreateApplicationAndSetRelayStateAsync(data);
                await _paaHelper.NavigateThroughSsoToOverviewAsync(
                    customerIntroFormData: new Dictionary<string, string> { [PLTypeOfDwelling] = "Condominium" });
            });

            await _logger.ExecuteStepAsync("Assert RoofResponsible question is not displayed on Overview page for Condo", async () =>
            {
                var roofFieldExists = await _pageHelper!.ElementExists(RoofResponsible);
                Assert.That(roofFieldExists, Is.False,
                    "RoofResponsible question should be hidden on the Overview page for a Condo (HO6) new quote");
            });

            await _logger.ExecuteStepAsync("Assert RoofResponsible defaults to 'true' in policy data (DB)", async () =>
            {
                var mainQueries = _testScope.ServiceProvider.GetRequiredService<IMainQueries>();
                var friendlyId = ScopeContext.Data.FriendlyId;

                var roofResponsible = await mainQueries.PolicyDataLogic.WaitForNodeValueAsync(
                    friendlyId, "RoofResponsible",
                    v => string.Equals(v, "true", StringComparison.OrdinalIgnoreCase));

                Assert.That(roofResponsible, Is.EqualTo("true").IgnoreCase,
                    $"RoofResponsible should default to 'true' in policy data for a Condo quote, actual: {roofResponsible}");
            });
        }

        [Test]
        [RunIn(includeProduction: true)]
        [Tenant(Tenant.PROGRESSIVEPL)]
        [Category("PAA")]
        [Category("Regression")]
        [TestCaseId(243901)]
        [Author(Author.Helen)]
        [Description("Verifies that 'Is the exterior of your home insured by someone else?' question appears and is editable when Home Style is changed from Condo to Homeowners (new quote)")]
        public async Task PGR_PAA_CondoToHomeowners_ExteriorInsured_Question_Shown_Required()
        {
            var address = AddressData.GetAddress(AddressKey.NY_Averill_Park);

            var data = await _logger.ExecuteStepAsync("Setup test data and agent user context", async () =>
            {
                _paaHelper.SetAdminUserContext();
                var data = PersonalLineDataProvider.GetPersonalDataProgressive(address);
                return data;
            });

            await _logger.ExecuteStepAsync("Create application, navigate through SSO, and select Condo on Customer Intro", async () =>
            {
                await _paaHelper.CreateApplicationAndSetRelayStateAsync(data);
                await _paaHelper.NavigateThroughSsoToOverviewAsync(
                    customerIntroFormData: new Dictionary<string, string> { [PLTypeOfDwelling] = "Condominium" });
            });

            await _logger.ExecuteStepAsync("Navigate to Customer Intro via progress bar and change Home Style to Single Family House (Homeowners)", async () =>
            {
                var customerIntroPage = await _paaHelper.NavigateViaProgressBarAsync<HQXAgent_CustomerIntroPage>(HQXAgentPage.CustomerIntro);
                await customerIntroPage.FillForm(new Dictionary<string, string> { [PLTypeOfDwelling] = "Single Family House" });
                await customerIntroPage.ClickContinue();
                var overviewPage = PageFactory.CreatePage<HQXAgent_OverviewPage>();

            });

            await _logger.ExecuteStepAsync("Assert RoofResponsible question is visible and editable on Overview page for Homeowners", async () =>
            {
                var roofFieldExists = await _pageHelper!.ElementExists(RoofResponsible);
                Assert.That(roofFieldExists, Is.True,
                    "RoofResponsible question should be displayed after switching from Condo to Homeowners");
            });
        }

        [Test]
        [Tenant(Tenant.PROGRESSIVEPL)]
        [Category("PAA")]
        [Category("Regression")]
        [TestCaseId(243902)]
        [Author(Author.Helen)]
        [Description("AC3: Verifies that 'Is the exterior of your home insured by someone else?' is hidden and defaults to Yes when Home Style is changed from Homeowners back to Condo (new quote)")]
        public async Task PGR_PAA_HomeownersToCondo_ExteriorInsured_Question_Hidden_DefaultYes()
        {
            var address = AddressData.GetAddress(AddressKey.NY_Averill_Park);

            var data = await _logger.ExecuteStepAsync("Setup test data and agent user context", async () =>
            {
                _paaHelper.SetAdminUserContext();
                var data = PersonalLineDataProvider.GetPersonalDataProgressive(address);
                return data;
            });

            await _logger.ExecuteStepAsync("Create application, navigate through SSO, and select Single Family House (Homeowners) on Customer Intro", async () =>
            {
                await _paaHelper.CreateApplicationAndSetRelayStateAsync(data);
                await _paaHelper.NavigateThroughSsoToOverviewAsync(
                    customerIntroFormData: new Dictionary<string, string> { [PLTypeOfDwelling] = "Single Family House" });
            });

            await _logger.ExecuteStepAsync("Assert RoofResponsible is initially visible and editable for Homeowners", async () =>
            {
                var overviewPage = PageFactory.CreatePage<HQXAgent_OverviewPage>();
                var roofFieldExists = await _pageHelper!.ElementExists(RoofResponsible);
                Assert.That(roofFieldExists, Is.True,
                    "RoofResponsible question should be visible for a Homeowners (HO3) quote before switching to Condo");
            });

            await _logger.ExecuteStepAsync("Change Home Style from Homeowners to Condo on Overview page", async () =>
            {
                var customerIntroPage = await _paaHelper.NavigateViaProgressBarAsync<HQXAgent_CustomerIntroPage>(HQXAgentPage.CustomerIntro);
                await customerIntroPage.FillForm(new Dictionary<string, string> { [PLTypeOfDwelling] = "Condominium" });
                await customerIntroPage.ClickContinue();
                var overviewPage = PageFactory.CreatePage<HQXAgent_OverviewPage>();

            });

            await _logger.ExecuteStepAsync("Assert RoofResponsible question is not displayed on Overview page after switching to Condo", async () =>
            {
                var roofFieldExists = await _pageHelper!.ElementExists(RoofResponsible);
                Assert.That(roofFieldExists, Is.False,
                    "RoofResponsible question should be hidden after switching from Homeowners to Condo");
            });

            await _logger.ExecuteStepAsync("Assert RoofResponsible defaults to 'true' in policy data (DB)", async () =>
            {
                var mainQueries = _testScope.ServiceProvider.GetRequiredService<IMainQueries>();
                var friendlyId = ScopeContext.Data.FriendlyId;

                var roofResponsible = await mainQueries.PolicyDataLogic.WaitForNodeValueAsync(
                    friendlyId, "RoofResponsible",
                    v => string.Equals(v, "true", StringComparison.OrdinalIgnoreCase));

                Assert.That(roofResponsible, Is.EqualTo("true").IgnoreCase,
                    $"RoofResponsible should default to 'true' in policy data after switching to Condo, actual: {roofResponsible}");
            });
        }

        [Test]
        [RunIn(includeProduction: true)]
        [Tenant(Tenant.PROGRESSIVEPL)]
        [Category("PAA")]
        [Category("Regression")]
        [TestCaseId(243903)]
        [Author(Author.Helen)]
        [Description("Verifies that 'Is the exterior of your home insured by someone else?' is displayed on the Overview page with the consumer's previous answer when an agent opens a revised Condo quote")]
        public async Task PGR_PAA_Condo_RevisedQuote_ExteriorInsured_Shown_With_PreviousAnswer()
        {
            string? externalId = null;

                ScopeContext.Set(ctx => ctx.FrontEnd, FrontEndType.HQXConsumer);
                ScopeContext.Set(ctx => ctx.CurrentUser, TestContextAccessor.CurrentUserCollection.Consumer);
                var request = QuoteStartPrefillDataProvider.GetHO6PrefillData(Addresses.GetAddress(AddressKey.NY_Averill_Park));
                var (id, _) = await _progressiveHelper.CallQuoteStartAsync(request, navigate: false);
                externalId = id;


            await _logger.ExecuteStepAsync("Navigate to Details Page", async () =>
            {
                await Executor.Execute<HQXConsumer_OverviewPage, HQXConsumer_DetailsPage>(FlowType.HQXShortFlow, null, false);
            });

            await _logger.ExecuteStepAsync("Fill Details form, check RoofResponsible = true and assert HO6 info box appears", async () =>
            {
                var detailsPage = PageFactory.CreatePage<HQXConsumer_DetailsPage>();
                await detailsPage.FillForm();
                await _pageHelper.InteractWithField(RoofResponsible, "true");
            });

            await _logger.ExecuteStepAsync("Switch to Agent context and open the same Condo quote (revised quote)", async () =>
            {
                _paaHelper.SetAdminUserContext();
                _paaHelper.SetRelayState(externalId!);
                ScopeContext.Set(ctx => ctx.FrontEnd, FrontEndType.HQXAgent);
                await _paaHelper.NavigateThroughSsoToOverviewAsync(newTab: true);
            });

            await _logger.ExecuteStepAsync("Assert RoofResponsible is visible and reflects the consumer's answer (true) on the Agent Overview page", async () =>
            {
                var roofFieldExists = await _pageHelper!.ElementExists(RoofResponsible);
                Assert.That(roofFieldExists, Is.True,
                    "RoofResponsible question should be visible to the agent on a revised Condo quote");

                var yesInputChecked = await _pageHelper!.GetFieldValue(RoofResponsible, PlaywrightValueType.SelectedValue);
                Assert.That(yesInputChecked, Is.EqualTo("true").IgnoreCase,
                    "RoofResponsible 'Yes' option should be selected, reflecting the consumer's previous answer (true)");
            });
        }

        // The home must be 20-49 years old for the "utilities replaced" question to be presented.
        private const int UtilitiesHomeAgeInYears = 20;

        [Test]
        [RunIn(includeProduction: true)]
        [Tenant(Tenant.PROGRESSIVEPL)]
        [TestCaseId(248328)]
        [Category("PAA")]
        [Category("Sanity")]
        [Author(Author.Helen)]
        [Description("FL HO3 Agent (PAA) quote (home 20 years old): the 'Has the heating, plumbing or electrical been replaced?' parent question appears on the Interior page with the Assumed/Verified prefill component. Selecting Yes reveals the plumbing/heating/electrical child questions; No hides them. The flow then continues through to Rates.")]
        public async Task PGR_PAA_Utilities_Replaced_Question_Reveals_Children_Then_Reaches_Rates()
        {
            var address = AddressData.GetAddress(AddressKey.FL_Bradenton);

            var data = await _logger.ExecuteStepAsync("Setup test data and agent user context (FL HO3, home 20 years old)", async () =>
            {
                _paaHelper.SetAdminUserContext();
                var data = PersonalLineDataProvider.GetPersonalDataProgressive(address);
                data.PLYearBuilt = DateTime.Now.Year - UtilitiesHomeAgeInYears;
                await Task.CompletedTask;
                return data;
            });

            await _logger.ExecuteStepAsync("Create application and navigate through SSO to Overview", async () =>
            {
                await _paaHelper.CreateApplicationAndSetRelayStateAsync(data);
                await _paaHelper.NavigateThroughSsoToOverviewAsync();
            });

            HQXAgent_InteriorPage interiorPage = null!;

            await _logger.ExecuteStepAsync("Navigate to the Interior page and assert the utilities-replaced question is presented", async () =>
            {
                var overviewPage = PageFactory.CreatePage<HQXAgent_OverviewPage>();
                // fillForms: false — the interview is already prefilled from the API application data
                // (the proven PAA navigation idiom); the flow just clicks Continue through each page.
                interiorPage = await Executor.ExecuteToPage<HQXAgent_InteriorPage>(AgentFlowType.PgrHomeFlow, overviewPage, fillForms: false);

                var parentPresented = await _pageHelper!.ElementExists(UtilitiesUpdated);
                Assert.That(parentPresented, Is.True,
                    "The 'Has the heating, plumbing or electrical been replaced?' parent question should be presented on the Interior page");
            });

            await _logger.ExecuteStepAsync("Assert the Assumed/Verified prefill component is displayed next to the parent question", async () =>
            {
                var componentDisplayed = await interiorPage.IsAssumedVerifiedComponentDisplayedAsync(UtilitiesUpdated);
                Assert.That(componentDisplayed, Is.True,
                    "The Assumed/Verified prefill component should be displayed next to the utilities-replaced parent question");
            });

            await _logger.ExecuteStepAsync("Select Yes for the parent question and assert child questions are revealed", async () =>
            {
                await _pageHelper!.InteractWithField(UtilitiesUpdated, "true");

                // Children are added to the DOM (Angular *ngIf) asynchronously after the parent flips,
                // so wait for the reveal rather than checking presence immediately (which races it).
                var plumbingShown = await interiorPage.WaitForQuestionVisibleAsync(UtilitiesPlumbingUpdated);
                var heatingShown = await interiorPage.WaitForQuestionVisibleAsync(PLHeatingUpdate);
                var electricalShown = await interiorPage.WaitForQuestionVisibleAsync(PLElectricalUpdated);
                Assert.Multiple(() =>
                {
                    Assert.That(plumbingShown, Is.True, "Plumbing update child question should be presented when the parent is Yes");
                    Assert.That(heatingShown, Is.True, "Heating update child question should be presented when the parent is Yes");
                    Assert.That(electricalShown, Is.True, "Electrical update child question should be presented when the parent is Yes");
                });
            });

            await _logger.ExecuteStepAsync("Change the parent question to No and assert child questions are hidden", async () =>
            {
                await _pageHelper!.InteractWithField(UtilitiesUpdated, "false");

                // The *ngIf teardown is asynchronous, so wait for the children to be removed from the
                // DOM rather than checking presence immediately (which sees the still-attached elements).
                var plumbingHidden = await interiorPage.WaitForQuestionHiddenAsync(UtilitiesPlumbingUpdated);
                var heatingHidden = await interiorPage.WaitForQuestionHiddenAsync(PLHeatingUpdate);
                var electricalHidden = await interiorPage.WaitForQuestionHiddenAsync(PLElectricalUpdated);
                Assert.Multiple(() =>
                {
                    Assert.That(plumbingHidden, Is.True, "Plumbing update child question should be hidden when the parent is No");
                    Assert.That(heatingHidden, Is.True, "Heating update child question should be hidden when the parent is No");
                    Assert.That(electricalHidden, Is.True, "Electrical update child question should be hidden when the parent is No");
                });
            });

            await _logger.ExecuteStepAsync("Continue through to the Rates (Selected Carrier) page", async () =>
            {
                var selectedCarrierPage = await Executor.ExecuteToPage<HQXAgent_SelectedCarrierPage>(
                    AgentFlowType.PgrHomeFlow, interiorPage, fillForms: false);
                Assert.That(selectedCarrierPage, Is.Not.Null,
                    "Rates (Selected Carrier) page should be presented after completing the quote");
            });
        }
    }
}
