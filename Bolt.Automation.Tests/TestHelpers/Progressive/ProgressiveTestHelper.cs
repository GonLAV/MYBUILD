using Bolt.Automation.ApiClients.Infrastructure;
using Bolt.Automation.ApiClients.PlatformApi;
using Bolt.Automation.ApiClients.PlatformApi.Entities.Common;
using Bolt.Automation.ApiClients.PlatformApi.Entities.Quote.QuoteRetrieve;
using Bolt.Automation.ApiClients.PlatformApi.Entities.Quote.QuoteStart;
using Bolt.Automation.ApiClients.PlatformApi.Extensions;
using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Enums;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.Common.Logging.Extentions;
using Bolt.Automation.FrontEnds.Executor;
using Bolt.Automation.FrontEnds.Executor.Helpers;
using Bolt.Automation.FrontEnds.Interfaces;
using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;
using static Bolt.Automation.FrontEnds.Executor.Helpers.PageCallbackManager;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.HQXConsumer.Flows;
using Bolt.Automation.FrontEnds.Projects.HQXConsumer.FormData;
using Bolt.Automation.FrontEnds.Projects.HQXConsumer.Pages;
using Bolt.Automation.TestDataProvider.Providers.PlatformAPIDataProvider;
using Microsoft.Playwright;

namespace Bolt.Automation.Tests.TestHelpers.Progressive;

/// <summary>
/// Reusable helper for common Progressive HQX Consumer test steps:
/// QuoteStart API calls, page navigation, and carrier selection.
/// </summary>
public class ProgressiveTestHelper(
    IAutomationLogger logger,
    IScopeContext scopeContext,
    PlaywrightExecutor executor,
    IPageFactory pageFactory,
    IPlatformApiClientFactory platformApiClientFactory,
    IBrowserManager browserManager)
{
    /// <summary>
    /// Calls the QuoteStart API and sets the start URL in scope context.
    /// Returns the external ID and start URL from the response.
    /// </summary>
    /// <param name="address">Address for the quote</param>
    /// <param name="navigate">If true, navigates to the start URL automatically</param>
    public Task<(string? ExternalId, string? StartUrl)> CallQuoteStartAsync(AddressModel address, bool navigate = false) =>
        CallQuoteStartAsync(QuoteStartPrefillDataProvider.GetFullPrefillData(address), navigate);

    /// <param name="request">Quote start request model</param>
    /// <param name="navigate">If true, navigates to the start URL automatically</param>
    public async Task<(string? ExternalId, string? StartUrl)> CallQuoteStartAsync(QuoteStartRequestModel request, bool navigate = false)
    {
        return await logger.ExecuteStepAsync("QuoteStart API Call", async () =>
        {
            var api = await platformApiClientFactory.CreateApiClientAsync();
            var response = await api.QuoteStartWithRetryAsync(request).EnsureSuccessContentAsync();

            response?.MapExternalId(scopeContext);
            var externalId = response?.BoltExternalId;
            var startUrl = response?.WebsiteURL;

            scopeContext.Set(ctx => ctx.CurrentUrl, startUrl);
            logger.Info($"QuoteStart API response received. WebsiteURL: {startUrl}. ExternalId {externalId}");

            if (navigate && !string.IsNullOrEmpty(startUrl))
            {
                logger.Info($"Navigating to start URL: {startUrl}");
                await browserManager.NavigateAsync(startUrl);
            }

            return (externalId, startUrl);
        });
    }

    /// <summary>
    /// Calls the QuoteRetrieval API and sets the URL in scope context.
    /// Returns the status and URL from the response.
    /// </summary>
    /// <param name="friendlyId">Quote friendly id</param>
    /// <param name="lastName">Applicant last name</param>
    /// <param name="zipCode">Applicant zip code</param>
    /// <param name="navigate">If true, navigates to the URL automatically</param>
    public Task<(string? Status, string? Url)> CallQuoteRetrievalAsync(
        string friendlyId,
        string lastName,
        string zipCode,
        bool navigate = false) =>
        CallQuoteRetrievalAsync(
            RetrieveQuoteDataProvider.CreateQuoteRetrievalRequest(friendlyId, lastName, zipCode),
            navigate);

    /// <param name="request">Quote retrieval request model</param>
    /// <param name="navigate">If true, navigates to the URL automatically</param>
    public async Task<(string? Status, string? Url)> CallQuoteRetrievalAsync(
        RetrievalRequestWrapperModel request,
        bool navigate = false)
    {
        return await logger.ExecuteStepAsync("QuoteRetrieval API Call", async () =>
        {
            var api = await platformApiClientFactory.CreateApiClientAsync();
            var response = await api.QuoteRetrievalAsync(request).EnsureSuccessContentAsync();

            var status = response?.Status;
            var url = response?.URL;

            scopeContext.Set(ctx => ctx.CurrentUrl, url);
            logger.Info($"QuoteRetrieval API response received. Status: {status}. URL: {url}");

            if (navigate && !string.IsNullOrEmpty(url))
            {
                logger.Info($"Navigating to retrieval URL: {url}");
                await browserManager.NavigateAsync(url);
            }

            return (status, url);
        });
    }

    /// <summary>
    /// Reopens a quote via QuoteRetrieval, navigates to the returned URL, and returns the resulting
    /// page (<typeparamref name="TPage"/>) — validated for readiness on creation. Use this when the
    /// retrieval is expected to land on a specific page (e.g. a completed consumer quote reopening
    /// on Rates); callers that only need the retrieval URL should use the tuple-returning overload.
    /// </summary>
    public async Task<TPage> CallQuoteRetrievalAsync<TPage>(
        string friendlyId,
        string lastName,
        string zipCode) where TPage : IBase
    {
        var (_, url) = await CallQuoteRetrievalAsync(friendlyId, lastName, zipCode, navigate: true);
        if (string.IsNullOrEmpty(url))
        {
            throw new InvalidOperationException("QuoteRetrieval did not return a URL to reopen the quote");
        }
        return pageFactory.CreatePage<TPage>();
    }

    /// <summary>
    /// Fills and submits the 3PQ (three prefill questions) page if the browser is currently on it.
    /// This is a no-op when prefill data is already available and the 3PQ page is skipped by the app.
    /// Call this after <see cref="CallQuoteStartAsync"/> and navigation, before any flow execution.
    /// Returns the OverviewPage instance after handling 3PQ or directly if already on Overview.
    /// </summary>
    public async Task<HQXConsumer_OverviewPage> HandleThreePQIfPresentAsync()
    {
        return await logger.ExecuteStepAsync("Handle 3PQ page if present", async () =>
        {
            var page = await browserManager.GetPageAsync();

            static bool Is3PQ(string url) => url.Contains("threeprefillquestions", StringComparison.OrdinalIgnoreCase);
            static bool IsOverview(string url) => url.Contains("overview", StringComparison.OrdinalIgnoreCase);

            // Step 1: Wait for the SPA to finish client-side routing after the deeplink redirect.
            // GotoAsync returns on the initial load event, but Angular routing happens after.
            if (!Is3PQ(page.Url) && !IsOverview(page.Url))
            {
                try
                {
                    await page.WaitForURLAsync(
                        new System.Text.RegularExpressions.Regex("(threeprefillquestions|overview)", System.Text.RegularExpressions.RegexOptions.IgnoreCase),
                        new PageWaitForURLOptions { Timeout = 30_000 });
                }
                catch
                {
                    logger.Debug($"URL did not settle to 3PQ or overview within timeout. Current: '{page.Url}'");
                }
            }

            // Step 2: Wait for the Angular component to finish mounting.
            // The URL changes before the component renders — button.next-button appearing
            // in the DOM is the reliable signal that the page is fully interactive.
            try
            {
                await page.Locator("button.next-button").WaitForAsync(new LocatorWaitForOptions
                {
                    State = WaitForSelectorState.Visible,
                    Timeout = 30_000
                });
            }
            catch
            {
                logger.Debug($"button.next-button not visible within timeout. Current URL: '{page.Url}'");
            }

            logger.Debug($"URL after settle: '{page.Url}'");

            if (Is3PQ(page.Url))
            {
                logger.Info($"3PQ page detected (URL: {page.Url}). Filling and continuing.");
                var threePQPage = pageFactory.CreatePage<HQXConusmer_3PQ>();
                await threePQPage.FillForm();
                await threePQPage.ClickContinue();

                // Wait for the SPA to route to overview after 3PQ submit.
                var waitHelper = new WaitHelper(page, logger);
                try { await waitHelper.WaitForNavigationOrUrlContainsAsync("overview", timeout: 30_000); }
                catch { logger.Debug($"URL did not reach overview after 3PQ continue. Current: '{page.Url}'"); }

                // Wait for overview component to finish mounting before returning.
                try
                {
                    await page.Locator("button.next-button").WaitForAsync(new LocatorWaitForOptions
                    {
                        State = WaitForSelectorState.Visible,
                        Timeout = 30_000
                    });
                }
                catch
                {
                    logger.Debug($"Overview button.next-button not visible after 3PQ submit. Current URL: '{page.Url}'");
                }
            }
            else
            {
                logger.Info($"3PQ page not present (URL: {page.Url}). Skipping.");
            }

            return pageFactory.CreatePage<HQXConsumer_OverviewPage>();
        });
    }

    /// <summary>
    /// Identifiers captured when a consumer quote is created and settled on Overview, plus the
    /// settled Overview page instance (pass it to <see cref="SubmitToRatesPageAsync"/>).
    /// </summary>
    public sealed record ConsumerQuote(
        string ExternalId, string FriendlyId, string LastName, string ZipCode, HQXConsumer_OverviewPage OverviewPage);

    /// <summary>
    /// Creates a consumer quote (QuoteStart) with full prefill for the given address, navigates to
    /// the deeplink, settles the SPA on Overview (handling the 3PQ page), and returns the identifiers
    /// the header/search calls need plus the settled Overview page. Full prefill lets the HQX short
    /// flow auto-advance to Rates without form-filling; settling first avoids the KickOutTimeOut race
    /// when an out-of-band API call runs immediately after create.
    /// </summary>
    public async Task<ConsumerQuote> CreateConsumerQuoteAndSettleAsync(AddressModel address)
    {
        var request = QuoteStartPrefillDataProvider.GetFullPrefillData(address);
        var (externalId, _) = await CallQuoteStartAsync(request, navigate: true);
        if (string.IsNullOrEmpty(externalId))
        {
            throw new InvalidOperationException("QuoteStart did not return an external application id");
        }

        var overview = await HandleThreePQIfPresentAsync();
        var friendlyId = await overview.GetFriendlyId();

        // FriendlyId is the lookup key for SEARCH APPLICATIONS and QuoteRetrieval, so the
        // page object's "NOT Found" placeholder must never reach a caller: it produces a
        // request that looks well-formed and quietly searches for nothing.
        if (string.IsNullOrWhiteSpace(friendlyId) || friendlyId == "NOT Found")
        {
            throw new InvalidOperationException(
                $"Could not read the quote number from the Overview page for application '{externalId}'.");
        }

        return new ConsumerQuote(
            externalId!,
            friendlyId,
            request.ApplicantDetails!.Surname!,
            request.PropertyAddr!.PostalCode!,
            overview);
    }

    /// <summary>
    /// Navigates from the Overview page to the Rates page using the short flow.
    /// Returns the Rates page instance for further interactions.
    /// </summary>
    public async Task<HQXConsumer_RatesPage> NavigateToRatesPageAsync()
    {
        return await logger.ExecuteStepAsync("Navigate to Rates Page", () =>
            executor.Execute<HQXConsumer_OverviewPage, HQXConsumer_RatesPage>(
                FlowType.HQXShortFlow, null, false));
    }

    /// <summary>
    /// Submits the already-settled Overview page through the short flow to the Rates page.
    /// Prefer this over <see cref="NavigateToRatesPageAsync"/> when you already hold the settled
    /// Overview page (it walks from that instance rather than re-resolving a possibly-stale one).
    /// </summary>
    public Task<HQXConsumer_RatesPage> SubmitToRatesPageAsync(HQXConsumer_OverviewPage overviewPage) =>
        // No ExecuteStepAsync here — callers already wrap this in their own orchestration step
        // (e.g. "Consumer submits through to the Rates page"); an inner step would nest redundantly.
        executor.ExecuteToPage<HQXConsumer_RatesPage>(FlowType.HQXShortFlow, overviewPage, fillForms: false);

    /// <summary>Submits with declining <paramref name="detailsOverrides"/>; returns the DNQ page served instead of Rates.</summary>
    /// <remarks>DNQ is off the flow's declared path. Created here, not inserted as a flow terminal — that
    /// would make Owner a walked page and start filling it, unlike other <see cref="SubmitInterviewAsync"/> callers.</remarks>
    public async Task<HQXConsumer_KoDNQPage> SubmitToDnqPageAsync(
        HQXConsumer_OverviewPage overviewPage, Dictionary<string, string> detailsOverrides)
    {
        await SubmitInterviewAsync(overviewPage, detailsOverrides);
        return pageFactory.CreatePage<HQXConsumer_KoDNQPage>();
    }

    /// <summary>
    /// Walks the short flow to Owner and submits, creating no landing page. MPQ3 needs this for both
    /// outcomes: a kickout redirects instead of rendering a page (so <see cref="SubmitToDnqPageAsync"/>
    /// would fail), and a successful submission shows its rates on Progressive's site, not BOLT's.
    /// </summary>
    public async Task SubmitInterviewAsync(
        HQXConsumer_OverviewPage overviewPage, Dictionary<string, string>? detailsOverrides = null)
    {
        var ownerPage = await executor.ExecuteToPage<HQXConsumer_OwnerPage>(
            FlowType.HQXShortFlow,
            overviewPage,
            fillForms: true,
            pageCallbacks: detailsOverrides is null
                ? null
                : PageCallbackManager.For<HQXConsumer_DetailsPage>(
                    detailsPage => detailsPage.FillForm(detailsOverrides),
                    CallbackTiming.InsteadOfFillForm));

        logger.Info("Submitting the completed interview; carrier evaluation runs next.");
        await ownerPage.ClickContinue();
    }

    /// <summary>
    /// Walks to Details, applies <paramref name="detailsOverrides"/> and continues — for kickouts that
    /// fire on Details rather than at submission. Creates no landing page.
    /// </summary>
    public async Task SubmitDetailsForKickoutAsync(
        HQXConsumer_OverviewPage overviewPage, Dictionary<string, string> detailsOverrides)
    {
        var detailsPage = await executor.ExecuteToPage<HQXConsumer_DetailsPage>(
            FlowType.HQXShortFlow, overviewPage, fillForms: true);

        await detailsPage.FillForm(detailsOverrides);

        logger.Info("Continuing from Details; the entered answers are expected to kick the quote out.");
        await detailsPage.ClickContinue();
    }

    /// <summary>
    /// Ensures the expected carrier is selected on the Rates page.
    /// Returns the Rates page instance for further interactions.
    /// </summary>
    public async Task<HQXConsumer_RatesPage> EnsureCarrierSelectedAsync(CarrierEnums carrier)
    {
        var ratesPage = pageFactory.CreatePage<HQXConsumer_RatesPage>();
        var expectedCarrierName = carrier.ToString();

        await logger.ExecuteStepAsync("Ensure Expected Carrier Selected", async () =>
        {
            await ratesPage.EnsureCarrierSelectedAsync(expectedCarrierName);
        });

        return ratesPage;
    }

    public static string GetFieldLabel(string fieldName)
    {
        return FieldRegistryHQXConsumer.Fields.TryGetValue(fieldName, out var field)
            ? field.Label ?? fieldName
            : fieldName;
    }

    public static IReadOnlyDictionary<string, string> GetFieldLabels(params string[] fieldNames)
    {
        var labels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var fieldName in fieldNames)
        {
            labels[fieldName] = GetFieldLabel(fieldName);
        }
        return labels;
    }

    /// <summary>
    /// Logs the actual/excluded/included question labels and computes which excluded labels
    /// are unexpectedly displayed and which included labels are missing from the actual questions.
    /// Returns (unexpectedlyDisplayed, missing) lists for assertion.
    /// </summary>
    public (IReadOnlyList<string> UnexpectedlyDisplayed, IReadOnlyList<string> Missing) GetQuestionLabelDiff(
        IEnumerable<string> actualQuestions,
        IEnumerable<string> excludedLabels,
        IEnumerable<string>? includedLabels = null,
        string? sectionName = null)
    {
        var actual = actualQuestions as string[] ?? [.. actualQuestions];
        var excluded = excludedLabels as string[] ?? [.. excludedLabels];
        var included = includedLabels?.ToArray();

        var prefix = string.IsNullOrWhiteSpace(sectionName) ? "Questions" : sectionName;
        logger.Info($"{prefix} actual questions: {string.Join(" | ", actual)}");
        logger.Info($"{prefix} excluded questions: {string.Join(" | ", excluded)}");
        if (included is { Length: > 0 })
        {
            logger.Info($"{prefix} included questions: {string.Join(" | ", included)}");
        }

        var unexpectedlyDisplayed = excluded.Where(actual.Contains).ToList();
        var missing = included?.Where(label => !actual.Contains(label)).ToList() ?? [];
        return (unexpectedlyDisplayed, missing);
    }

    /// <summary>
    /// Computes missing and extra question IDs between expected and actual sets.
    /// Returns (missing, extra) lists for assertion.
    /// </summary>
    public static (IReadOnlyList<string> Missing, IReadOnlyList<string> Extra) GetSectionQuestionDiff(
        IEnumerable<string> expectedIds,
        IEnumerable<string> actualIds)
    {
        var expected = new HashSet<string>(expectedIds, StringComparer.OrdinalIgnoreCase);
        var actual = new HashSet<string>(actualIds, StringComparer.OrdinalIgnoreCase);
        var missing = expected.Except(actual).OrderBy(x => x).ToList();
        var extra = actual.Except(expected).OrderBy(x => x).ToList();
        return (missing, extra);
    }
}
