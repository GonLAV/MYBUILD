using Bolt.Automation.ApiClients.GetQuoteApi.Interfaces;
using Bolt.Automation.ApiClients.GetQuoteApi.Models.Note;
using Bolt.Automation.ApiClients.Infrastructure;
using Bolt.Automation.ApiClients.PlatformApi;
using Bolt.Automation.ApiClients.PlatformApi.Entities.Common;
using Bolt.Automation.ApiClients.PlatformApi.Entities.Quote.QuoteStatus;
using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Enums;
using Bolt.Automation.Common.Exceptions;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.Common.Logging.Extentions;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.TestDataProvider.Context;
using Bolt.Automation.TestDataProvider.Providers.PlatformAPIDataProvider;
using Bolt.Automation.TestDataProvider.TestData.PlatformAPITestData;
using static Bolt.Automation.TestDataProvider.Providers.PlatformAPIDataProvider.QuoteStartPrefillDataProvider;

namespace Bolt.Automation.Tests.TestHelpers.Progressive;

/// <summary>
/// Creates MPQ3 quotes and reads back what the platform recorded for them, so the tests hold only
/// preconditions and assertions.
/// </summary>
/// <remarks>
/// Every read returns what it found and never asserts — a wrong or absent value is the caller's finding to
/// report. Identity is scoped per call, so no read leaks the Admin key into the rest of the test.
/// </remarks>
public sealed class Mpq3QuoteStatusHelper(
    IAutomationLogger logger,
    IScopeContext scopeContext,
    TestContextAccessor testContextAccessor,
    IPlatformApiClientFactory platformApiFactory,
    IGetQuoteApi getQuoteApi,
    ProgressiveTestHelper progressiveHelper,
    IPageHelper pageHelper,
    IBrowserManager browserManager)
{
    /// <summary>The domain an MPQ3 quote must be handed back to on any kickout or exit.</summary>
    public const string ProgressiveDomain = "progressive.com";

    /// <summary>The SourceName every MPQ3 quote is created and read under.</summary>
    private static readonly string SourceName = nameof(SourceNames.MPQ3);

    private readonly TestIdentityScope _identity = new(scopeContext, testContextAccessor);

    /// <summary>An MPQ3 quote under test, with the CheckQuoteStatus request every status read reuses.</summary>
    /// <remarks>The request is built once so all polls for one quote carry the same RqUID and ClientDt —
    /// <see cref="QuoteStatusRequestTestData"/> regenerates both on each property read.</remarks>
    public sealed record Mpq3Quote(string ExternalId)
    {
        public QuoteStatusRequestModel StatusRequest { get; } =
            QuoteStatusDataProvider.CreateQuoteStatusData(ExternalId, SourceName);
    }

    /// <summary>The result of waiting for a GetQuote note: the match, plus every action that <i>was</i> written.</summary>
    /// <remarks><see cref="ActionsWritten"/> carries the whole set so an assertion on an absent note can name
    /// what the platform recorded instead.</remarks>
    public sealed record NoteLookup(NoteResponseModel? Note, string ActionsWritten)
    {
        public string? Description => Note?.Description;
    }

    /// <summary>
    /// Starts an MPQ3 consumer quote for <paramref name="address"/> and navigates to the deeplink.
    /// </summary>
    /// <remarks>Does not settle on an interview page — kickouts that fire at QuoteStart never reach one.
    /// Callers that drive the interview settle with <c>HandleThreePQIfPresentAsync</c>.</remarks>
    public async Task<Mpq3Quote> StartConsumerQuoteAsync(
        AddressKey address, PrefillScenario prefill = PrefillScenario.Full)
    {
        scopeContext.Set(ctx => ctx.CurrentUser, _identity.Consumer);

        var mappedAddress = Addresses.GetAddress(address);
        var request = prefill switch
        {
            PrefillScenario.Standard => GetStandardPrefillData(mappedAddress),
            PrefillScenario.Full => GetFullPrefillData(mappedAddress),
            _ => throw new ArgumentOutOfRangeException(
                nameof(prefill), prefill, "MPQ3 quotes are started with Standard or Full prefill")
        };
        request.SourceName = SourceName;

        var (externalId, _) = await progressiveHelper.CallQuoteStartAsync(request, navigate: true);
        if (string.IsNullOrEmpty(externalId))
        {
            throw new InvalidOperationException(
                "QuoteStart returned no BOLT External Id, so there is no quote to read a status for");
        }

        return new Mpq3Quote(externalId);
    }

    /// <summary>Waits for the hand-back to Progressive and returns the URL the browser settled on.</summary>
    /// <remarks>The wait's bool is deliberately discarded — it throws rather than returning false, so the URL
    /// is the only assertable value. See kb framework:navigation-waits.</remarks>
    public Task<string> ReadLandingUrlAsync(int timeoutMs = 15000) =>
        logger.ExecuteStepAsync($"Wait for the hand-back to the '{ProgressiveDomain}' domain", async () =>
        {
            try
            {
                await pageHelper.WaitForNavigationOrUrlContainsAsync(ProgressiveDomain, timeout: timeoutMs);
            }
            catch (NavigationException ex)
            {
                logger.Info($"No hand-back to '{ProgressiveDomain}' within {timeoutMs}ms — {ex.Message}");
            }

            var landingUrl = browserManager.GetCurrentTab()?.Url ?? "no active page";
            logger.Info($"The consumer was left on: {landingUrl}");
            return landingUrl;
        });

    /// <summary>
    /// Reads until the quote reports a secondary status other than None, returning what it settled on.
    /// </summary>
    /// <remarks>Use when the expected pair is unknown or disputed — waiting for a specific one that never
    /// arrives spends the whole budget and reports a timeout in place of the finding.</remarks>
    public Task<QuoteStatusResponseModel> ReadStatusUntilSettledAsync(
        Mpq3Quote quote, int maxAttempts = 15, TimeSpan? delay = null) =>
        ReadStatusUntilAsync(
            quote,
            status => !Matches(status.SecondaryStatus, QuoteSecondaryStatus.None),
            "the quote settles on a secondary status",
            maxAttempts,
            delay ?? TimeSpan.FromSeconds(5));

    /// <summary>Reads until the quote reports the expected pair, returning what it settled on.</summary>
    public Task<QuoteStatusResponseModel> ReadStatusUntilPairAsync(
        Mpq3Quote quote,
        QuoteStatus expectedPrimary,
        QuoteSecondaryStatus expectedSecondary,
        int maxAttempts = 20,
        TimeSpan? delay = null) =>
        ReadStatusUntilAsync(
            quote,
            status => Matches(status.QuoteStatus, expectedPrimary)
                && Matches(status.SecondaryStatus, expectedSecondary),
            $"{expectedPrimary} / {expectedSecondary}",
            maxAttempts,
            delay ?? TimeSpan.FromSeconds(5));

    private Task<QuoteStatusResponseModel> ReadStatusUntilAsync(
        Mpq3Quote quote,
        Func<QuoteStatusResponseModel, bool> until,
        string waitingFor,
        int maxAttempts,
        TimeSpan delay) =>
        logger.ExecuteStepAsync($"Read QuoteStatus from Platform API until {waitingFor}", () =>
            // CheckQuoteStatus is a Progressive-facing Platform call, so it runs on the consumer key. The
            // client binds the identity at creation, so it is created inside the scope, not outside it.
            _identity.AsConsumerAsync(async () =>
            {
                var platformApi = await platformApiFactory.CreateApiClientAsync();
                var status = await platformApi.QuoteStatusUntilAsync(quote.StatusRequest, until, maxAttempts, delay);

                logger.Info($"QuoteStatus reports {status.QuoteStatus} / {status.SecondaryStatus} " +
                            $"(waited for {waitingFor}).");
                return status;
            }));

    /// <summary>
    /// Waits for a note with <paramref name="action"/> and returns it alongside every action written.
    /// </summary>
    /// <remarks>Returns a null <see cref="NoteLookup.Note"/> rather than throwing when the note never
    /// arrives, so the caller reports it as a finding instead of dying on a poll timeout.</remarks>
    public Task<NoteLookup> ReadNoteAsync(
        Mpq3Quote quote, string action, int maxAttempts = 12, TimeSpan? delay = null) =>
        logger.ExecuteStepAsync($"Read the GetQuote notes until the '{action}' note appears", async () =>
        {
            var notes = await RetryHelper.RetryAsync(
                () => ReadNotesOnceAsync(quote),
                written => written.Any(note => Matches(note.Action, action)),
                maxAttempts: maxAttempts,
                delay: delay ?? TimeSpan.FromSeconds(5));

            var match = notes.FirstOrDefault(note => Matches(note.Action, action));
            var actionsWritten = DescribeActions(notes);

            logger.Info(match is null
                ? $"No '{action}' note was written. Notes present: {actionsWritten}"
                : $"Found the '{action}' note: {match.Description}");

            return new NoteLookup(match, actionsWritten);
        });

    // GetQuote /notes is an agent/admin-authenticated read. EnsureSuccessContent rather than `Content ?? []`
    // so a 401 from the wrong identity reads as an auth failure, not as "the platform wrote no note".
    private Task<IReadOnlyList<NoteResponseModel>> ReadNotesOnceAsync(Mpq3Quote quote) =>
        _identity.AsAdminAsync(async () =>
        {
            var notes = await getQuoteApi.GetNotesByApplicationIdAsync(quote.ExternalId)
                .EnsureSuccessContentAsync($"GET NOTES failed for application '{quote.ExternalId}'");
            return (IReadOnlyList<NoteResponseModel>)notes;
        });

    private static string DescribeActions(IEnumerable<NoteResponseModel> notes)
    {
        var actions = notes.Select(note => $"'{note.Action}'").ToList();
        return actions.Count > 0 ? string.Join(", ", actions) : "none";
    }

    private static bool Matches(string? actual, QuoteStatus expected) =>
        string.Equals(actual, expected.ToString(), StringComparison.OrdinalIgnoreCase);

    // The platform spells the secondary "LockedbyAgent", so every comparison is case-insensitive.
    private static bool Matches(string? actual, QuoteSecondaryStatus expected) =>
        string.Equals(actual, expected.ToString(), StringComparison.OrdinalIgnoreCase);

    private static bool Matches(string? actual, string expected) =>
        string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);
}
