using Bolt.Automation.ApiClients.GetQuoteApi.Extensions;
using Bolt.Automation.ApiClients.GetQuoteApi.Interfaces;
using Bolt.Automation.ApiClients.GetQuoteApi.Models.Application;
using Bolt.Automation.ApiClients.Infrastructure;
using Bolt.Automation.ApiClients.SSO;
using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Enums;
using Bolt.Automation.Common.Models.RelayStates;
using Bolt.Automation.Common.Models.TestData.DataModels.PersonalLine;
using Bolt.Automation.FrontEnds.Executor;
using Bolt.Automation.FrontEnds.Interfaces;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.HQXAgent.Enums;
using Bolt.Automation.FrontEnds.Projects.HQXAgent.Flows;
using Bolt.Automation.FrontEnds.Projects.HQXAgent.FormData.FieldsRegistry;
using Bolt.Automation.FrontEnds.Projects.HQXAgent.Pages;
using Bolt.Automation.TestDataProvider.Context;
using Microsoft.Playwright;

namespace Bolt.Automation.Tests.TestHelpers.Progressive;

public class PaaTestHelper(
    IGetQuoteApi getQuoteApi,
    ISsoApiFactory ssoApiFactory,
    PlaywrightExecutor executor,
    Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface.IPageFactory pageFactory,
    IBrowserManager browserManager,
    IScopeContext scopeContext,
    TestContextAccessor testContextAccessor)
{
    public void SetAdminUserContext()
    {
        var user = testContextAccessor.CurrentUserCollection.Admin;
        scopeContext.Set(ctx => ctx.CurrentUser, user);
        scopeContext.Set(ctx => ctx.SamlTemplate, SamlTemplateType.Saml2ResponseTemplate);
    }

    public void SetAgentUserContext()
    {
        var user = testContextAccessor.CurrentUserCollection.Admin;
        scopeContext.Set(ctx => ctx.CurrentUser, user);
    }

    public async Task CreateApplicationAndSetRelayStateAsync(PersonalLineData data)
    {
        var requestData = new ApplicationRequestModel<PersonalLineData>
        {
            Data = data
        };

        var response = await getQuoteApi.CreateApplicationAsync(requestData).EnsureSuccessContentAsync();
        response.MapIdentifiers(scopeContext);
        SetRelayState(response.Id);
    }

    public void SetRelayState(string applicationId)
    {
        var baseRelayState = testContextAccessor.CurrentRelayStateCollection.RetrieveQuoteRelayState.Value;
        var relayStateValue = $"{baseRelayState}{applicationId}";
        var relayStateTestData = new RelayStateTestData { Value = relayStateValue };
        scopeContext.Set(ctx => ctx.CurrentRelayStateType, relayStateTestData);
    }

    /// <summary>
    /// Calls GET APPLICATION HEADER for the application (as the Admin identity).
    /// </summary>
    public Task<ApplicationHeader> GetApplicationHeaderAsync(string applicationId) =>
        AsAdminAsync(() => getQuoteApi.GetApplicationHeadersByApplicationIdAsync(applicationId)
            .EnsureSuccessContentAsync($"GET APPLICATION HEADER failed for application '{applicationId}'"));

    /// <summary>
    /// Polls GET APPLICATION HEADER (as Admin) until <paramref name="until"/> holds or the timeout
    /// elapses, then returns the last header. Backend fields (preferredCarrierPremium, lockedByAgent)
    /// can lag the UI action that triggers them, so callers poll rather than reading once.
    /// </summary>
    public Task<ApplicationHeader> GetApplicationHeaderUntilAsync(
        string applicationId, Func<ApplicationHeader, bool> until) =>
        AsAdminAsync(async () =>
        {
            var response = await RetryHelper.RetryUntilAsync(
                () => getQuoteApi.GetApplicationHeadersByApplicationIdAsync(applicationId),
                content => until(content),
                timeout: TimeSpan.FromSeconds(60),
                delay: TimeSpan.FromSeconds(3),
                label: $"GET APPLICATION HEADER condition for application '{applicationId}'");
            return response.Content!;
        });

    /// <summary>
    /// Calls SEARCH APPLICATIONS (as Admin) and returns the matching records.
    /// </summary>
    public Task<List<ApplicationHeader>> SearchApplicationsAsync(SearchApplicationsRequestModel request) =>
        AsAdminAsync(() => getQuoteApi.SearchApplicationsAsync(request)
            .EnsureSuccessContentAsync("SEARCH APPLICATIONS call failed"));

    /// <summary>
    /// Builds a SEARCH APPLICATIONS request from the common last-name / friendly-id / zip criteria.
    /// </summary>
    public static SearchApplicationsRequestModel BuildSearchRequest(string lastName, string friendlyId, string zipCode) =>
        new()
        {
            SearchParameters = new SearchApplicationsParameters
            {
                LastName = lastName,
                FriendlyId = friendlyId,
                ZipCode = zipCode
            }
        };

    private readonly TestIdentityScope _identity = new(scopeContext, testContextAccessor);

    /// <summary>
    /// Runs a GetQuote API call under the Admin identity, then restores the prior user.
    /// </summary>
    /// <remarks>
    /// GET APPLICATION HEADER / SEARCH APPLICATIONS authenticate through the GetQuote OAuth pipeline,
    /// which only Admin carries credentials for — the Consumer drives the UI but has no GetQuote token.
    /// </remarks>
    private Task<T> AsAdminAsync<T>(Func<Task<T>> apiCall) => _identity.AsAdminAsync(apiCall);

    public async Task NavigateThroughSsoAsync(bool newTab = false)
    {
        var progressiveSsoClient = ssoApiFactory.CreateClient();
        var ssoResponse = await progressiveSsoClient.GetSsoResponse();
        var redirectUrl = ssoResponse.Content?.RedirectUrl;

        if (newTab)
            await browserManager.OpenNewTabAsync(redirectUrl);
        else
            await browserManager.NavigateAsync(redirectUrl);
    }

    /// <summary>Lands via SSO on Customer Intro and continues to Overview — the first two pages of PgrHomeFlow.</summary>
    public async Task<HQXAgent_OverviewPage> NavigateThroughSsoToOverviewAsync(
        bool newTab = false, Dictionary<string, string>? customerIntroFormData = null)
    {
        await NavigateThroughSsoAsync(newTab);

        return await executor.ExecuteToPage<HQXAgent_OverviewPage>(
            FlowType.PgrHomeFlow,
            pageFactory.CreatePage<HQXAgent_CustomerIntroPage>(),
            customerIntroFormData,
            fillForms: true);
    }

    /// <summary>
    /// Opens a consumer-created quote as the PAA/Admin (by application id via the retrieve-quote SSO
    /// relay state) and lands on Overview, leaving the quote untouched. Encapsulates the role switch
    /// — Admin context, HQXAgent front-end, new tab — and the SSO landing.
    /// </summary>
    public async Task<HQXAgent_OverviewPage> OpenQuoteAsAgentAsync(string applicationId)
    {
        SetAdminUserContext();
        SetRelayState(applicationId);
        scopeContext.Set(ctx => ctx.FrontEnd, FrontEndType.HQXAgent);

        return await NavigateThroughSsoToOverviewAsync(newTab: true);
    }

    /// <summary>
    /// Opens a consumer-created quote as the PAA/Admin and enters edit mode — the action that flips
    /// the quote's lockedByAgent to true. Retrieval on its own does not; only the Edit click does.
    /// </summary>
    /// <summary>Returns the Overview page the agent lands back on, now in edit mode.</summary>
    public async Task<HQXAgent_OverviewPage> OpenQuoteAsAgentAndEnterEditModeAsync(string applicationId)
    {
        var overviewAgentPage = await OpenQuoteAsAgentAsync(applicationId);
        await overviewAgentPage.ClickEditQuote();

        // Edit mode routes back through Customer Intro; continuing from it confirms edit mode is active.
        return await executor.ExecuteToPage<HQXAgent_OverviewPage>(
            FlowType.PgrHomeFlow,
            pageFactory.CreatePage<HQXAgent_CustomerIntroPage>(),
            fillForms: false);
    }

    /// <summary>
    /// Calls SEARCH APPLICATIONS with the given criteria and returns the single record matching
    /// <paramref name="externalId"/>, or null if not present in the results.
    /// </summary>
    public async Task<ApplicationHeader?> SearchAndFindByExternalIdAsync(
        SearchApplicationsRequestModel request, string externalId)
    {
        var results = await SearchApplicationsAsync(request);
        return results.FirstOrDefault(r =>
            string.Equals(r.QuoteExternalId, externalId, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Selects the carrier on the Selected Carrier page and continues. Admitted carriers are
    /// picked from the carrier table; the Excess &amp; Surplus carrier (Bamboo Surplus) is reached
    /// via the dedicated "Get E&amp;S HO rates" button instead. Keeps the carrier-selection branch
    /// out of the test body so the test stays a clean orchestration.
    /// </summary>
    public async Task SelectCarrierAndContinueAsync(HQXAgent_SelectedCarrierPage selectedCarrier, CarrierEnums carrier)
    {
        if (carrier == CarrierEnums.BambooSurplus)
            await selectedCarrier.ClickGetESRatesButton();
        else
            await selectedCarrier.ClickOnSpecificCarrier(carrier.ToString());

        await selectedCarrier.FillForm();
        await selectedCarrier.ClickContinue();
    }

    /// <summary>Answers the bridge-gating carrier questions, then continues to trigger the bridge.</summary>
    public async Task AnswerBridgeCarrierQuestionsAndContinueAsync(
        HQXAgent_CarrierQuestionsPage carrierQuestions, CarrierEnums carrier)
    {
        await carrierQuestions.AnswerBridgeQuestionsAsync(carrier);
        await carrierQuestions.ClickContinue();
    }

    public async Task<T> NavigateViaProgressBarAsync<T>(HQXAgentPage page) where T : class, IBase
    {
        var label = page switch
        {
            HQXAgentPage.CustomerIntro      => "Customer Intro",
            HQXAgentPage.Overview           => "Overview",
            HQXAgentPage.Triage             => "Triage",
            HQXAgentPage.Exterior           => "Exterior",
            HQXAgentPage.Interior           => "Interior",
            HQXAgentPage.Owner              => "Owner",
            HQXAgentPage.Discounts          => "Discounts",
            HQXAgentPage.FinalDetails       => "Final Details",
            HQXAgentPage.SelectedCarrier    => "Selected Carrier",
            HQXAgentPage.PrefillVerification => "Prefilled Verification",
            HQXAgentPage.CarrierQuestions   => "Carrier Questions",
            _ => throw new ArgumentOutOfRangeException(nameof(page), page, "Unknown HQXAgent progress bar page")
        };

        var currentPage = browserManager.GetCurrentTab()!;
        var button = currentPage.Locator($"app-progress-meter button", new() { HasText = label });

        // Capture the current URL, then wait for the Angular router's pushState to change
        // it before creating the page object. Without this, WaitForLoadersToDisappearAsync
        // in ValidatePageReady can pass before the loading indicator appears, leaving Angular
        // still rendering the form when FillForm subsequently runs.
        var previousUrl = currentPage.Url;
        await button.ClickAsync();
        await currentPage.WaitForURLAsync(
            url => url != previousUrl,
            new PageWaitForURLOptions { Timeout = 30000 });

        return pageFactory.CreatePage<T>();
    }
}
