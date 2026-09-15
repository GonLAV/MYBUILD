using Bolt.Automation.ApiClients.GetQuoteApi.Interfaces;
using Bolt.Automation.ApiClients.GetQuoteApi.Models.Note;
using Bolt.Automation.ApiClients.Infrastructure;
using Bolt.Automation.ApiClients.PlatformApi.Entities.Common;
using Bolt.Automation.ApiClients.PlatformApi.Entities.CRMNote;
using Bolt.Automation.ApiClients.SSO;
using Microsoft.Playwright;
using Bolt.Automation.FrontEnds.Executor;
using Bolt.Automation.FrontEnds.Executor.Helpers;
using Bolt.Automation.FrontEnds.Projects.HQXAgent.Flows;
using Bolt.Automation.FrontEnds.Projects.HQXAgent.Pages;
using Bolt.Automation.FrontEnds.Projects.HQXAgent.Popups;
using Bolt.Automation.Common;
using Bolt.Automation.Common.Enums;
using Bolt.Automation.Common.Logging.Extentions;
using Bolt.Automation.Common.PollyRetry;
using Bolt.Automation.FrontEnds.Projects.HQXConsumer.Pages;
using Bolt.Automation.TestDataProvider.Providers.PlatformAPIDataProvider;
using Bolt.Automation.TestDataProvider.TestData.PlatformAPITestData;
using Bolt.Automation.Tests.TestData.Progressive;
using Bolt.Automation.Tests.TestExtension.Attributes;
using Bolt.Automation.Tests.TestExtension.Base;
using Bolt.Automation.Tests.TestHelpers.Progressive;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using static Bolt.Automation.FrontEnds.FormData.Common.FieldNames;
using static Bolt.Automation.FrontEnds.Executor.Helpers.PageCallbackManager.CallbackTiming;
using static Bolt.Automation.TestDataProvider.Providers.PlatformAPIDataProvider.QuoteStartPrefillDataProvider;
using AuthorAttribute = Bolt.Automation.Tests.TestExtension.Attributes.AuthorAttribute;

namespace Bolt.Automation.Tests.Tests.Progressive.DRFlows
{
    [TestFixture]
    public class MPQ3QuoteStatusTests : ProgressiveUITestBase
    {
        private const string SourceName = "MPQ3";

        // The Sold Note modal's Parent Company option for the carrier a GA_Athens quote rates with.
        private const string HomesiteParentCompany = "Homesite Insurance";

        private IGetQuoteApi _getQuoteApi = null!;
        private IPollyRetryService _pollyRetryService = null!;
        private ISsoApiFactory _ssoApiFactory = null!;
        private PaaTestHelper _paaHelper = null!;
        private Mpq3QuoteStatusHelper _mpq3 = null!;

        // Only the pages an agent must answer are filled; the rest keep the consumer's own data.
        // Replacement cost has no registry default — an agent types it per quote.
        private static readonly Dictionary<Type, PageCallbackManager.PageCallbackConfig> AgentTakeoverAnswers =
            PageCallbackManager
                .For<HQXAgent_TriagePage>(p => p.FillForm(new Dictionary<string, string>
                {
                    [ShortTermRental] = "false",
                    [PersonalLineReplacementCost] = "300000",
                    [YearsAtAddress] = "5"
                }), InsteadOfFillForm)
                // FinalDetails overrides FillForm without a default, so the argument is required here.
                .And<HQXAgent_FinalDetailsPage>(p => p.FillForm(null!), InsteadOfFillForm)
                .And<HQXAgent_SelectedCarrierPage>(p => p.FillForm(), InsteadOfFillForm);

        protected override void ResolveServices()
        {
            base.ResolveServices();
            _getQuoteApi = _uiTestScope.ServiceProvider
                .GetRequiredService<RefitApiServiceLocator>()
                .GetRequiredService<IGetQuoteApi>();
            _pollyRetryService = _testScope.ServiceProvider.GetRequiredService<IPollyRetryService>();
            _ssoApiFactory = _testScope.ServiceProvider.GetRequiredService<ISsoApiFactory>();
        }

        protected override void InitializeComponents()
        {
            base.InitializeComponents();
            // The agent cases hand a consumer-created MPQ3 quote to the PAA front end mid-test.
            _paaHelper = new PaaTestHelper(
                _getQuoteApi, _ssoApiFactory, Executor, PageFactory, BrowserManager, ScopeContext, TestContextAccessor);

            _mpq3 = new Mpq3QuoteStatusHelper(
                _logger, ScopeContext, TestContextAccessor, _platformApiFactory, _getQuoteApi,
                _progressiveHelper, _pageHelper!, BrowserManager);
        }

        [Test]
        [RunIn(includeProduction: true)]
        [Tenant(Tenant.PROGRESSIVEPL)]
        [TestCaseId(248527)]
        [Category("MPQ3")]
        [Category("Sanity")]
        [Description("Selecting Manufactured/Mobile Home on the MPQ3 Overview kicks the consumer out to progressive.com; QuoteStatus reports Incomplete / ManufacturedHome and GetQuote writes the 'Kick out' note.")]
        [Author(Author.Helen)]
        public async Task PGR_MPQ3_MFH_Kickout_Reports_Incomplete_ManufacturedHome()
        {
            const string kickOutDescription = "Manufactured home kick out.";

            var quote = await _mpq3.StartConsumerQuoteAsync(AddressKey.OH, PrefillScenario.Standard);
            var overviewPage = await _progressiveHelper.HandleThreePQIfPresentAsync();

            await overviewPage.SetHomeStyleAsync(HQXConsumer_OverviewPage.HomeStyles.ManufacturedMobileHome);
            await overviewPage.ClickContinue();

            var landingUrl = await _mpq3.ReadLandingUrlAsync();
            var quoteStatus = await _mpq3.ReadStatusUntilPairAsync(
                quote, QuoteStatus.Incomplete, QuoteSecondaryStatus.ManufacturedHome);
            var kickOut = await _mpq3.ReadNoteAsync(quote, QuoteNoteAction.KickOut);

            Assert.Multiple(() =>
            {
                Assert.That(landingUrl, Does.Contain(Mpq3QuoteStatusHelper.ProgressiveDomain),
                    $"The manufactured-home kickout did not hand the consumer back to Progressive — an MPQ3 quote must redirect, never render a BOLT kickout page. Left on: {landingUrl}");
                Assert.That(quoteStatus.QuoteStatus, Is.EqualTo(nameof(QuoteStatus.Incomplete)),
                    $"QuoteStatus was not Incomplete. Actual: {quoteStatus.QuoteStatus}");
                Assert.That(quoteStatus.SecondaryStatus, Is.EqualTo(nameof(QuoteSecondaryStatus.ManufacturedHome)),
                    $"SecondaryStatus was not ManufacturedHome. Actual: {quoteStatus.SecondaryStatus}");
                Assert.That(kickOut.Note, Is.Not.Null,
                    $"No '{QuoteNoteAction.KickOut}' note was written. Notes present: {kickOut.ActionsWritten}.");
                Assert.That(kickOut.Description, Does.Contain(kickOutDescription),
                    $"Note description should contain '{kickOutDescription}'. Actual: {kickOut.Description}");
            });
        }

        [Test]
        [RunIn(includeProduction: true)]
        [Tenant(Tenant.PROGRESSIVEPL)]
        [TestCaseId(248548)]
        [Category("MPQ3")]
        [Category("Sanity")]
        [Description("Starting an MPQ3 residency quote with a Hawaii (out-of-appetite) address kicks the consumer out to progressive.com; QuoteStatus reports NoAppetite / NoAppetite and GetQuote writes the 'No available carriers.' kick-out note.")]
        [Author(Author.Helen)]
        public async Task PGR_MPQ3_HI_Kickout_Reports_NoAppetite_NoAppetite()
        {
            const string kickOutDescription = "No available carriers.";

            // Never settles on Overview: an out-of-appetite state kicks out at QuoteStart.
            var quote = await _mpq3.StartConsumerQuoteAsync(AddressKey.HI, PrefillScenario.Standard);

            var landingUrl = await _mpq3.ReadLandingUrlAsync();
            var quoteStatus = await _mpq3.ReadStatusUntilPairAsync(
                quote, QuoteStatus.NoAppetite, QuoteSecondaryStatus.NoAppetite);
            var kickOut = await _mpq3.ReadNoteAsync(quote, QuoteNoteAction.KickOut);

            Assert.Multiple(() =>
            {
                Assert.That(landingUrl, Does.Contain(Mpq3QuoteStatusHelper.ProgressiveDomain),
                    $"The Hawaii out-of-appetite kickout did not hand the consumer back to Progressive — an MPQ3 quote must redirect, never render a BOLT kickout page. Left on: {landingUrl}");
                Assert.That(quoteStatus.QuoteStatus, Is.EqualTo(nameof(QuoteStatus.NoAppetite)),
                    $"QuoteStatus was not NoAppetite. Actual: {quoteStatus.QuoteStatus}");
                Assert.That(quoteStatus.SecondaryStatus, Is.EqualTo(nameof(QuoteSecondaryStatus.NoAppetite)),
                    $"SecondaryStatus was not NoAppetite. Actual: {quoteStatus.SecondaryStatus}");
                Assert.That(kickOut.Note, Is.Not.Null,
                    $"No '{QuoteNoteAction.KickOut}' note was written. Notes present: {kickOut.ActionsWritten}.");
                Assert.That(kickOut.Description, Does.Contain(kickOutDescription),
                    $"Note description should contain '{kickOutDescription}'. Actual: {kickOut.Description}");
            });
        }

        [Test]
        [Tenant(Tenant.PROGRESSIVEPL)]
        [Category("MPQ3")]
        [Category("Regression")]
        [TestCaseId(251318)]
        [Description("Exotic pets on the premises is an underwriting kickout, so the UUD pass leaves no carrier to submit to: the consumer is redirected to the Progressive RedirectURL instead of the BOLT DNQ page and GetQuote records the 'No available carriers.' kick out note. Lands RED by design on the secondary status: the quote reports DNQ where the spec calls for UUD, the misassignment family of bug 249733.")]
        [Author(Author.Helen)]
        public async Task PGR_MPQ3_ExoticPets_Kickout_Reports_DNQ_UUD()
        {
            const string kickOutDescription = "No available carriers.";

            var quote = await _mpq3.StartConsumerQuoteAsync(AddressKey.GA_Athens);
            var overviewPage = await _progressiveHelper.HandleThreePQIfPresentAsync();

            var underwritingKickout = new Dictionary<string, string>
            {
                [AnimalsOnThePremises_None] = "true",
                [AnimalsOnThePremises_Exotic] = "true",
            };

            await _progressiveHelper.SubmitInterviewAsync(overviewPage, underwritingKickout);

            var landingUrl = await _mpq3.ReadLandingUrlAsync();

            // Settled, not the expected pair: UUD never arrives, and the assertion needs to name what did.
            var quoteStatus = await _mpq3.ReadStatusUntilSettledAsync(quote);
            var kickOut = await _mpq3.ReadNoteAsync(quote, QuoteNoteAction.KickOut);

            Assert.Multiple(() =>
            {
                Assert.That(landingUrl, Does.Contain(Mpq3QuoteStatusHelper.ProgressiveDomain),
                    $"The UUD kickout did not redirect to the Progressive domain — an MPQ3 quote must redirect, never render a BOLT kickout page. Left on: {landingUrl}");
                Assert.That(quoteStatus.QuoteStatus, Is.EqualTo(nameof(QuoteStatus.DNQ)),
                    $"QuoteStatus was not DNQ. Actual: {quoteStatus.QuoteStatus}");
                Assert.That(quoteStatus.SecondaryStatus, Is.EqualTo(nameof(QuoteSecondaryStatus.UUD)),
                    $"SecondaryStatus was not UUD. An underwriting kickout declines before any submission, so DNQ here is a misassignment. Actual: {quoteStatus.SecondaryStatus}");
                Assert.That(kickOut.Note, Is.Not.Null,
                    $"No '{QuoteNoteAction.KickOut}' note was written. Notes present: {kickOut.ActionsWritten}.");
                Assert.That(kickOut.Description, Does.Contain(kickOutDescription),
                    $"Note description should contain '{kickOutDescription}'. Actual: {kickOut.Description}");
            });
        }

        [Test]
        [Tenant(Tenant.PROGRESSIVEPL)]
        [Category("MPQ3")]
        [Category("Regression")]
        [TestCaseId(251319)]
        [Description("Answering 'not my primary residence' on the MPQ3 Details page and choosing the Rental home type kicks the quote out; the consumer is redirected to the Progressive RedirectURL rather than being blocked on a BOLT error page (bug 235011). Lands RED by design on the note assertion: unlike every other MPQ3 kickout, this one writes no 'Kick out' note.")]
        [Author(Author.Helen)]
        public async Task PGR_MPQ3_Rental_Kickout_Reports_Incomplete_ReferToAgent()
        {
            var quote = await _mpq3.StartConsumerQuoteAsync(AddressKey.OH);
            var overviewPage = await _progressiveHelper.HandleThreePQIfPresentAsync();

            // Each answer reveals the next; the registry's DependsOn chain fills them in order.
            var rentalAnswers = new Dictionary<string, string>
            {
                [IsPrimaryResidence] = "false",
                [DwellingUsage] = "Rental",
                [ShortTermRental] = "true",
            };

            await _progressiveHelper.SubmitDetailsForKickoutAsync(overviewPage, rentalAnswers);

            var landingUrl = await _mpq3.ReadLandingUrlAsync();
            var quoteStatus = await _mpq3.ReadStatusUntilPairAsync(
                quote, QuoteStatus.Incomplete, QuoteSecondaryStatus.ReferToAgent);
            var kickOut = await _mpq3.ReadNoteAsync(quote, QuoteNoteAction.KickOut);

            Assert.Multiple(() =>
            {
                Assert.That(landingUrl, Does.Contain(Mpq3QuoteStatusHelper.ProgressiveDomain),
                    $"The rental home type did not redirect to the Progressive domain — bug 235011 showed a BOLT error page instead. Left on: {landingUrl}");
                Assert.That(quoteStatus.QuoteStatus, Is.EqualTo(nameof(QuoteStatus.Incomplete)),
                    $"QuoteStatus was not Incomplete. Actual: {quoteStatus.QuoteStatus}");
                Assert.That(quoteStatus.SecondaryStatus, Is.EqualTo(nameof(QuoteSecondaryStatus.ReferToAgent)),
                    $"SecondaryStatus was not ReferToAgent. Actual: {quoteStatus.SecondaryStatus}");
                Assert.That(kickOut.Note, Is.Not.Null,
                    $"No '{QuoteNoteAction.KickOut}' note was written. Every other MPQ3 kickout records one. Notes present: {kickOut.ActionsWritten}.");
            });
        }

        [Test]
        [Tenant(Tenant.PROGRESSIVEPL)]
        [Category("MPQ3")]
        [Category("Regression")]
        [TestCaseId(251499)]
        [Description("DC, FL and HI are the three states where the Manufactured/Mobile Home kickout is replaced by a stronger one, so the same trigger as TC 248527 on a Florida address must not report ManufacturedHome. It reports NoAppetite / NoAppetite, with the redirect and the kick out note both correct — the same pair as the Hawaii case in TC 248548, which is the contrast that matters there: the status is right in both states and only Hawaii fails to redirect.")]
        [Author(Author.Helen)]
        public async Task PGR_MPQ3_FL_MFH_Kickout_Reports_NoAppetite_NoAppetite()
        {
            var quote = await _mpq3.StartConsumerQuoteAsync(AddressKey.FL, PrefillScenario.Standard);
            var overviewPage = await _progressiveHelper.HandleThreePQIfPresentAsync();

            await overviewPage.SetHomeStyleAsync(HQXConsumer_OverviewPage.HomeStyles.ManufacturedMobileHome);
            await overviewPage.ClickContinue();

            var landingUrl = await _mpq3.ReadLandingUrlAsync();

            // Settled, not the expected pair — a changed status names itself. That is how this pair was found.
            var quoteStatus = await _mpq3.ReadStatusUntilSettledAsync(quote);
            var kickOut = await _mpq3.ReadNoteAsync(quote, QuoteNoteAction.KickOut);

            Assert.Multiple(() =>
            {
                Assert.That(landingUrl, Does.Contain(Mpq3QuoteStatusHelper.ProgressiveDomain),
                    $"The Florida manufactured-home kickout did not redirect to the Progressive domain — an MPQ3 quote must redirect, never render a BOLT kickout page. Left on: {landingUrl}");
                Assert.That(quoteStatus.QuoteStatus, Is.EqualTo(nameof(QuoteStatus.NoAppetite)),
                    $"QuoteStatus was not NoAppetite. Actual: {quoteStatus.QuoteStatus}");
                Assert.That(quoteStatus.SecondaryStatus, Is.EqualTo(nameof(QuoteSecondaryStatus.NoAppetite)),
                    $"SecondaryStatus was not NoAppetite. Florida is one of the three states where the manufactured-home kickout is replaced by a stronger one, so ManufacturedHome here would mean the state rule did not fire. Actual: {quoteStatus.SecondaryStatus} (primary {quoteStatus.QuoteStatus}).");
                Assert.That(kickOut.Note, Is.Not.Null,
                    $"No '{QuoteNoteAction.KickOut}' note was written. Every MPQ3 kickout records one. Notes present: {kickOut.ActionsWritten}.");
            });
        }

        [Test]
        [Tenant(Tenant.PROGRESSIVEPL)]
        [Category("MPQ3")]
        [Category("Regression")]
        [TestCaseId(251500)]
        [Description("CheckQuoteStatus for a BOLT External Id that resolves to no quote answers NotFound / None rather than failing the transport. Pure API — no interview, no quote of its own.")]
        [Author(Author.Helen)]
        public async Task PGR_MPQ3_UnknownExternalId_Reports_NotFound_None()
        {
            ScopeContext.Set(ctx => ctx.CurrentUser, TestContextAccessor.CurrentUserCollection.Consumer);

            var platformApi = await _platformApiFactory.CreateApiClientAsync();
            var unresolvableExternalId = Guid.NewGuid().ToString();

            // No polling: NotFound is immediate and terminal, so a retry loop could only mask a real answer.
            var quoteStatus = await _logger.ExecuteStepAsync("Request QuoteStatus for an External Id that resolves to no quote", async () =>
            {
                var quoteStatusRequest = QuoteStatusDataProvider.CreateQuoteStatusData(unresolvableExternalId, SourceName);
                return await platformApi.QuoteStatusAsync(quoteStatusRequest).EnsureSuccessContentAsync();
            });

            Assert.Multiple(() =>
            {
                Assert.That(quoteStatus.QuoteStatus, Is.EqualTo(nameof(QuoteStatus.NotFound)),
                    $"QuoteStatus was not NotFound for an unresolvable External Id. Actual: {quoteStatus.QuoteStatus}");
                Assert.That(quoteStatus.SecondaryStatus, Is.EqualTo(nameof(QuoteSecondaryStatus.None)),
                    $"SecondaryStatus was not None. Actual: {quoteStatus.SecondaryStatus}");
            });
        }

        [Test]
        [Tenant(Tenant.PROGRESSIVEPL)]
        [Category("MPQ3")]
        [Category("Regression")]
        [TestCaseId(251501)]
        [Description("Progressive reports the consumer's online-buy bridge to BOLT as a Bridge CRMNote — the click itself happens on Progressive's page and is not reachable from here, so the note is posted directly. A completed MPQ3 quote moves to ConsumerBridged / None on that note, and GetQuote records a Bridge note naming the carrier the consumer bridged to.")]
        [Author(Author.Helen)]
        public async Task PGR_MPQ3_ConsumerBridge_Reports_ConsumerBridged_None()
        {
            ScopeContext.Set(ctx => ctx.CurrentUser, TestContextAccessor.CurrentUserCollection.Consumer);
            var request = QuoteStartPrefillDataProvider.GetFullPrefillData(Addresses.GetAddress(AddressKey.GA_Athens));
            request.SourceName = SourceName;

            var (externalId, _) = await _progressiveHelper.CallQuoteStartAsync(request, navigate: true);
            var overviewPage = await _progressiveHelper.HandleThreePQIfPresentAsync();

            // No overrides — this is the happy path. MPQ3 shows the rates on Progressive's site, so the
            // submission lands on no BOLT page and the outcome is only observable through the API.
            await _progressiveHelper.SubmitInterviewAsync(overviewPage);

            var platformApi = await _platformApiFactory.CreateApiClientAsync();
            var quoteStatusRequest = QuoteStatusDataProvider.CreateQuoteStatusData(externalId!, SourceName);

            // Precondition, not an assertion — TC 89383 owns Complete/None. The wait is what matters:
            // the Bridge note is rejected until the quote carries a rated result. Read rather than poll
            // for it, so a quote that never rates names its status instead of throwing a cancellation.
            var ratedStatus = await _logger.ExecuteStepAsync("Wait for the quote to finish rating", async () =>
            {
                return await RetryHelper.RetryAsync(
                    async () => (await platformApi.QuoteStatusAsync(quoteStatusRequest)).Content!,
                    status => string.Equals(status.QuoteStatus, nameof(QuoteStatus.Complete), StringComparison.OrdinalIgnoreCase),
                    maxAttempts: 20,
                    delay: TimeSpan.FromSeconds(6));
            });

            Assert.That(ratedStatus.QuoteStatus, Is.EqualTo(nameof(QuoteStatus.Complete)),
                $"Precondition failed — the quote did not rate, so there is no rated result to bridge from. Actual: {ratedStatus.QuoteStatus} / {ratedStatus.SecondaryStatus}");

            // GetQuote is an agent/admin-authenticated read; switch off the consumer key that drove the quote.
            ScopeContext.Set(ctx => ctx.CurrentUser, TestContextAccessor.CurrentUserCollection.Admin);

            // The note is matched against an active successful result, so both the carrier and the LOB
            // have to come off that result rather than from the LOB the quote was started with.
            var ratedResult = await _logger.ExecuteStepAsync("Read the rated result the consumer would have bridged to", async () =>
            {
                var quotes = (await _getQuoteApi.GetQuotesByApplicationIdAsync(externalId!)).Content ?? [];
                return quotes.FirstOrDefault(q => string.Equals(q.Status, "Success", StringComparison.OrdinalIgnoreCase))
                    ?? throw new InvalidOperationException(
                        $"The completed quote carries no successful carrier result, so there is nothing to bridge to. Results returned: {quotes.Count}");
            });

            // CRMNote is a Progressive-facing Platform call, so it goes back on the consumer key.
            ScopeContext.Set(ctx => ctx.CurrentUser, TestContextAccessor.CurrentUserCollection.Consumer);
            platformApi = await _platformApiFactory.CreateApiClientAsync();

            // Whether the note was accepted is asserted by the status transition below, not by the
            // response envelope — a rejected note simply never moves the quote to ConsumerBridged.
            await _logger.ExecuteStepAsync("Post the consumer Bridge CRMNote for the rated carrier", async () =>
            {
                var crmNoteRequest = CRMNoteDataProvider.CreateCRMNoteData(
                    externalId!, SourceName, ratedResult.Carrier!, ratedResult.Lob!, NoteType.Bridge);
                return await platformApi.CRMNoteAsync(crmNoteRequest).EnsureSuccessContentAsync();
            });

            var bridgedStatus = await _logger.ExecuteStepAsync("Poll QuoteStatus from Platform API until ConsumerBridged / None", async () =>
            {
                var response = (await platformApi.QuoteStatusWithPollingAsync(
                    quoteStatusRequest,
                    QuoteStatus.ConsumerBridged,
                    QuoteSecondaryStatus.None)).Content!;
                return response;
            });

            Assert.Multiple(() =>
            {
                Assert.That(bridgedStatus.QuoteStatus, Is.EqualTo(nameof(QuoteStatus.ConsumerBridged)),
                    $"QuoteStatus was not ConsumerBridged after the Bridge note. Actual: {bridgedStatus.QuoteStatus}");
                Assert.That(bridgedStatus.SecondaryStatus, Is.EqualTo(nameof(QuoteSecondaryStatus.None)),
                    $"SecondaryStatus was not None after the Bridge note. Actual: {bridgedStatus.SecondaryStatus}");
            });

            const string bridgeAction = "Bridge";

            // GetQuote /notes is an agent/admin-authenticated read; switch off the consumer key that drove the quote.
            ScopeContext.Set(ctx => ctx.CurrentUser, TestContextAccessor.CurrentUserCollection.Admin);

            var bridgeNote = await _logger.ExecuteStepAsync("Poll GetQuote /notes until the Bridge note appears", async () =>
            {
                return await _pollyRetryService.ExecuteWithRetryAsync(async () =>
                {
                    var notes = (await _getQuoteApi.GetNotesByApplicationIdAsync(externalId!)).Content ?? [];
                    var note = notes.FirstOrDefault(n => string.Equals(n.Action, bridgeAction, StringComparison.OrdinalIgnoreCase));
                    return note ?? throw new InvalidOperationException($"'{bridgeAction}' note not yet available in GetQuote API — retrying.");
                });
            });

            Assert.Multiple(() =>
            {
                Assert.That(bridgeNote.Action, Is.EqualTo(bridgeAction),
                    $"Note action was not '{bridgeAction}'. Actual: {bridgeNote.Action}");
                Assert.That(bridgeNote.Description, Does.Contain(ratedResult.Carrier!),
                    $"The Bridge note names a carrier other than the one bridged to. Expected to contain '{ratedResult.Carrier}'. Actual: {bridgeNote.Description}");
                Assert.That(bridgeNote.Description, Does.Contain("bridged by Consumer"),
                    $"The Bridge note does not record the bridge as the consumer's. Actual: {bridgeNote.Description}");
            });
        }

        [Test]
        [Tenant(Tenant.PROGRESSIVEPL)]
        [Category("MPQ3")]
        [Category("Regression")]
        [TestCaseId(251502)]
        [Description("An agent retrieving a completed MPQ3 quote and entering edit mode takes ownership of it: the quote reports LockedByAgent and drops back to Incomplete because it is being changed. Merely viewing the quote does not lock it — only the Edit action does.")]
        [Author(Author.Helen)]
        public async Task PGR_MPQ3_AgentEdit_Reports_Incomplete_LockedByAgent()
        {
            ScopeContext.Set(ctx => ctx.CurrentUser, TestContextAccessor.CurrentUserCollection.Consumer);
            var request = QuoteStartPrefillDataProvider.GetFullPrefillData(Addresses.GetAddress(AddressKey.GA_Athens));
            request.SourceName = SourceName;

            var (externalId, _) = await _progressiveHelper.CallQuoteStartAsync(request, navigate: true);
            var overviewPage = await _progressiveHelper.HandleThreePQIfPresentAsync();
            await _progressiveHelper.SubmitInterviewAsync(overviewPage);

            var platformApi = await _platformApiFactory.CreateApiClientAsync();
            var quoteStatusRequest = QuoteStatusDataProvider.CreateQuoteStatusData(externalId!, SourceName);

            // Precondition: the agent has to have a settled quote to take over.
            // Read rather than poll for Complete: a quote that never rates would otherwise throw a bare
            // cancellation instead of naming the status it did reach.
            var ratedStatus = await _logger.ExecuteStepAsync("Wait for the consumer quote to finish rating", async () =>
            {
                return await RetryHelper.RetryAsync(
                    async () => (await platformApi.QuoteStatusAsync(quoteStatusRequest)).Content!,
                    status => string.Equals(status.QuoteStatus, nameof(QuoteStatus.Complete), StringComparison.OrdinalIgnoreCase),
                    maxAttempts: 20,
                    delay: TimeSpan.FromSeconds(6));
            });

            Assert.That(ratedStatus.QuoteStatus, Is.EqualTo(nameof(QuoteStatus.Complete)),
                $"Precondition failed — the consumer quote did not rate, so there is nothing rated for this scenario to act on. Actual: {ratedStatus.QuoteStatus} / {ratedStatus.SecondaryStatus}");

            await _logger.ExecuteStepAsync("Agent retrieves the consumer's quote and enters edit mode", async () =>
            {
                await _paaHelper.OpenQuoteAsAgentAndEnterEditModeAsync(externalId!);
            });

            // Polls the primary only: the platform spells the secondary "LockedbyAgent", which the
            // enum-driven overload would never match, so it is asserted case-insensitively below.
            ScopeContext.Set(ctx => ctx.CurrentUser, TestContextAccessor.CurrentUserCollection.Consumer);
            platformApi = await _platformApiFactory.CreateApiClientAsync();

            var lockedStatus = await _logger.ExecuteStepAsync("Poll QuoteStatus from Platform API until the agent's edit lands", async () =>
            {
                return (await platformApi.QuoteStatusWithPollingAsync(
                    quoteStatusRequest,
                    QuoteStatus.Incomplete)).Content!;
            });

            const string editAction = "Edit Quote";

            // GetQuote /notes is an agent/admin-authenticated read; switch off the consumer key that drove the quote.
            ScopeContext.Set(ctx => ctx.CurrentUser, TestContextAccessor.CurrentUserCollection.Admin);

            var editNote = await _logger.ExecuteStepAsync("Poll GetQuote /notes until the Edit Quote note appears", async () =>
            {
                return await _pollyRetryService.ExecuteWithRetryAsync(async () =>
                {
                    var notes = (await _getQuoteApi.GetNotesByApplicationIdAsync(externalId!)).Content ?? [];
                    var note = notes.FirstOrDefault(n => string.Equals(n.Action, editAction, StringComparison.OrdinalIgnoreCase));
                    return note ?? throw new InvalidOperationException($"'{editAction}' note not yet available in GetQuote API — retrying.");
                });
            });

            Assert.Multiple(() =>
            {
                Assert.That(lockedStatus.SecondaryStatus, Is.EqualTo(nameof(QuoteSecondaryStatus.LockedByAgent)).IgnoreCase,
                    $"SecondaryStatus was not LockedByAgent after the agent took the quote into edit mode. Actual: {lockedStatus.SecondaryStatus}");
                Assert.That(lockedStatus.QuoteStatus, Is.EqualTo(nameof(QuoteStatus.Incomplete)),
                    $"QuoteStatus was not Incomplete. An agent editing a rated quote invalidates its results. Actual: {lockedStatus.QuoteStatus}");
                Assert.That(editNote.Description, Does.Contain("edited by the agent"),
                    $"The Edit Quote note does not record the edit as the agent's. Actual: {editNote.Description}");
            });
        }

        [Test]
        [Tenant(Tenant.PROGRESSIVEPL)]
        [Category("MPQ3")]
        [Category("Regression")]
        [TestCaseId(252053)]
        [Description("A declined MPQ3 quote that an agent then takes into edit mode reports DNQ / LockedByAgent: the lock is a secondary status and must not displace the primary. The comparison is the agent-edit case on a rated quote, which does drop the primary to Incomplete — that is legitimate there because an edit invalidates results, whereas a decline is a verdict on the risk that editing does not undo.")]
        [Author(Author.Helen)]
        public async Task PGR_MPQ3_AgentEditsDnqQuote_Reports_DNQ_LockedByAgent()
        {
            ScopeContext.Set(ctx => ctx.CurrentUser, TestContextAccessor.CurrentUserCollection.Consumer);
            var request = QuoteStartPrefillDataProvider.GetFullPrefillData(Addresses.GetAddress(AddressKey.GA_Athens));
            request.SourceName = SourceName;

            var (externalId, _) = await _progressiveHelper.CallQuoteStartAsync(request, navigate: true);
            var overviewPage = await _progressiveHelper.HandleThreePQIfPresentAsync();

            var underwritingKickout = new Dictionary<string, string>
            {
                [AnimalsOnThePremises_None] = "true",
                [AnimalsOnThePremises_Exotic] = "true",
            };

            await _progressiveHelper.SubmitInterviewAsync(overviewPage, underwritingKickout);

            var platformApi = await _platformApiFactory.CreateApiClientAsync();
            var quoteStatusRequest = QuoteStatusDataProvider.CreateQuoteStatusData(externalId!, SourceName);

            var declinedStatus = await _logger.ExecuteStepAsync("Read QuoteStatus until the decline settles", async () =>
            {
                return await RetryHelper.RetryAsync(
                    async () => (await platformApi.QuoteStatusAsync(quoteStatusRequest)).Content!,
                    status => !string.Equals(status.SecondaryStatus, nameof(QuoteSecondaryStatus.None), StringComparison.OrdinalIgnoreCase),
                    maxAttempts: 15,
                    delay: TimeSpan.FromSeconds(5));
            });

            // Guarded rather than assumed: everything below reads as a lock defect if the quote never
            // declined in the first place, and the exotic-pets decline is a carrier-side verdict.
            Assert.That(declinedStatus.QuoteStatus, Is.EqualTo(nameof(QuoteStatus.DNQ)),
                $"Precondition failed — the quote did not decline, so there is no DNQ to lock. Actual: {declinedStatus.QuoteStatus} / {declinedStatus.SecondaryStatus}");

            await _logger.ExecuteStepAsync("Agent retrieves the declined quote and enters edit mode", async () =>
            {
                await _paaHelper.OpenQuoteAsAgentAndEnterEditModeAsync(externalId!);
            });

            ScopeContext.Set(ctx => ctx.CurrentUser, TestContextAccessor.CurrentUserCollection.Consumer);
            platformApi = await _platformApiFactory.CreateApiClientAsync();

            var lockedStatus = await _logger.ExecuteStepAsync("Read QuoteStatus until the agent's lock lands", async () =>
            {
                return await RetryHelper.RetryAsync(
                    async () => (await platformApi.QuoteStatusAsync(quoteStatusRequest)).Content!,
                    status => string.Equals(status.SecondaryStatus, nameof(QuoteSecondaryStatus.LockedByAgent), StringComparison.OrdinalIgnoreCase),
                    maxAttempts: 15,
                    delay: TimeSpan.FromSeconds(5));
            });

            Assert.Multiple(() =>
            {
                Assert.That(lockedStatus.SecondaryStatus, Is.EqualTo(nameof(QuoteSecondaryStatus.LockedByAgent)).IgnoreCase,
                    $"SecondaryStatus was not LockedByAgent after the agent took the declined quote into edit mode. Actual: {lockedStatus.SecondaryStatus}");
                Assert.That(lockedStatus.QuoteStatus, Is.EqualTo(nameof(QuoteStatus.DNQ)),
                    $"QuoteStatus was not DNQ. The agent's lock is a secondary status and must not displace a decline — the risk is still declined while the agent holds the quote. Actual: {lockedStatus.QuoteStatus}");
            });
        }

        [Test]
        [Tenant(Tenant.PROGRESSIVEPL)]
        [Category("MPQ3")]
        [Category("Regression")]
        [TestCaseId(252052)]
        [Description("An MPQ3 home quote that runs to the end of the interview and rates reports Complete / None — the one path where nothing went wrong. Every other test in this fixture asserts a quote that was kicked out, declined, abandoned or taken over, so without this one a defect that stopped quotes completing would leave the whole suite green.")]
        [Author(Author.Helen)]
        public async Task PGR_MPQ3_CompletedHomeQuote_Reports_Complete_None()
        {
            ScopeContext.Set(ctx => ctx.CurrentUser, TestContextAccessor.CurrentUserCollection.Consumer);
            var request = QuoteStartPrefillDataProvider.GetFullPrefillData(Addresses.GetAddress(AddressKey.GA_Athens));
            request.SourceName = SourceName;

            var (externalId, _) = await _progressiveHelper.CallQuoteStartAsync(request, navigate: true);
            var overviewPage = await _progressiveHelper.HandleThreePQIfPresentAsync();
            await _progressiveHelper.SubmitInterviewAsync(overviewPage);

            var platformApi = await _platformApiFactory.CreateApiClientAsync();
            var quoteStatusRequest = QuoteStatusDataProvider.CreateQuoteStatusData(externalId!, SourceName);

            var completedStatus = await _logger.ExecuteStepAsync("Read QuoteStatus until the submission completes", async () =>
            {
                return await RetryHelper.RetryAsync(
                    async () => (await platformApi.QuoteStatusAsync(quoteStatusRequest)).Content!,
                    status => string.Equals(status.QuoteStatus, nameof(QuoteStatus.Complete), StringComparison.OrdinalIgnoreCase),
                    maxAttempts: 20,
                    delay: TimeSpan.FromSeconds(6));
            });

            Assert.Multiple(() =>
            {
                Assert.That(completedStatus.QuoteStatus, Is.EqualTo(nameof(QuoteStatus.Complete)),
                    $"QuoteStatus was not Complete. The interview was answered in full and submitted, so anything else means a quote that should have finished did not. Actual: {completedStatus.QuoteStatus}");
                Assert.That(completedStatus.SecondaryStatus, Is.EqualTo(nameof(QuoteSecondaryStatus.None)),
                    $"SecondaryStatus was not None. Nothing kicked this quote out, declined it or locked it. Actual: {completedStatus.SecondaryStatus}");
            });
        }

        [Test]
        [Tenant(Tenant.PROGRESSIVEPL)]
        [Category("MPQ3")]
        [Category("Regression")]
        [TestCaseId(252054)]
        [Description("A consumer who abandons an MPQ3 home quote through the exit-to-auto link is handed back to Progressive and the quote is left Incomplete / None — an unfinished interview, not a kickout or a decline. GetQuote records the return as the consumer's own action.")]
        [Author(Author.Helen)]
        public async Task PGR_MPQ3_ConsumerExitsToAuto_Reports_Incomplete_None()
        {
            ScopeContext.Set(ctx => ctx.CurrentUser, TestContextAccessor.CurrentUserCollection.Consumer);
            var request = QuoteStartPrefillDataProvider.GetFullPrefillData(Addresses.GetAddress(AddressKey.OH));
            request.SourceName = SourceName;

            var (externalId, _) = await _progressiveHelper.CallQuoteStartAsync(request, navigate: true);
            var overviewPage = await _progressiveHelper.HandleThreePQIfPresentAsync();

            var linkPresent = await overviewPage.IsExitToAutoQuoteLinkVisibleAsync();
            var handedBack = await overviewPage.ClickExitToAutoQuoteAsync();

            var platformApi = await _platformApiFactory.CreateApiClientAsync();
            var quoteStatusRequest = QuoteStatusDataProvider.CreateQuoteStatusData(externalId!, SourceName);

            // Incomplete / None is the quote's state from the moment the interview is left unfinished, so
            // there is nothing to settle on — read until the primary is Incomplete and report what is there.
            var exitedStatus = await _logger.ExecuteStepAsync("Read QuoteStatus after the consumer returns to auto", async () =>
            {
                return await RetryHelper.RetryAsync(
                    async () => (await platformApi.QuoteStatusAsync(quoteStatusRequest)).Content!,
                    status => string.Equals(status.QuoteStatus, nameof(QuoteStatus.Incomplete), StringComparison.OrdinalIgnoreCase),
                    maxAttempts: 12,
                    delay: TimeSpan.FromSeconds(5));
            });

            const string returnedAction = "INET returned to auto";
            const string returnedDescription = "Consumer clicked the return to auto quote link.";

            // GetQuote /notes is an agent/admin-authenticated read; switch off the consumer key that drove the quote.
            ScopeContext.Set(ctx => ctx.CurrentUser, TestContextAccessor.CurrentUserCollection.Admin);

            var notes = await _logger.ExecuteStepAsync("Read the GetQuote notes written for this quote", async () =>
            {
                return await RetryHelper.RetryAsync(
                    async () => (await _getQuoteApi.GetNotesByApplicationIdAsync(externalId!)).Content ?? [],
                    written => written.Any(n => string.Equals(n.Action, returnedAction, StringComparison.OrdinalIgnoreCase)),
                    maxAttempts: 12,
                    delay: TimeSpan.FromSeconds(5));
            });

            var returnedNote = notes.FirstOrDefault(n => string.Equals(n.Action, returnedAction, StringComparison.OrdinalIgnoreCase));

            Assert.Multiple(() =>
            {
                Assert.That(linkPresent, Is.True,
                    "The exit-to-auto link was not on the Overview stage. FSD 8.1.1 requires it on every stage so the consumer can always return to their auto quote.");
                Assert.That(handedBack, Is.True,
                    "Clicking the exit-to-auto link did not hand the consumer back to the Progressive domain — an MPQ3 quote must return to the Quote Start RedirectURL, never strand the consumer in BOLT.");
                Assert.That(exitedStatus.QuoteStatus, Is.EqualTo(nameof(QuoteStatus.Incomplete)),
                    $"QuoteStatus was not Incomplete. Leaving an interview unfinished is an incomplete quote, not a kickout or a decline. Actual: {exitedStatus.QuoteStatus}");
                Assert.That(exitedStatus.SecondaryStatus, Is.EqualTo(nameof(QuoteSecondaryStatus.None)),
                    $"SecondaryStatus was not None. Nothing kicked the consumer out and no agent touched the quote — the consumer chose to leave. Actual: {exitedStatus.SecondaryStatus}");
                Assert.That(returnedNote, Is.Not.Null,
                    $"No '{returnedAction}' note was written. Notes present: {string.Join(", ", notes.Select(n => $"'{n.Action}'"))}.");
                Assert.That(returnedNote?.Description, Does.Contain(returnedDescription),
                    $"The note does not record the return as the consumer's own action. Actual: {returnedNote?.Description}");
            });
        }

        [Test]
        [Tenant(Tenant.PROGRESSIVEPL)]
        [Category("MPQ3")]
        [Category("Regression")]
        [TestCaseId(251503)]
        [Description("An agent picks up a completed MPQ3 quote and bridges it to the ranked carrier on the consumer's behalf. The quote reports Bridged, and stays LockedByAgent because the agent still holds it. GetQuote records a Bridge note naming the agent rather than the consumer.")]
        [Author(Author.Helen)]
        public async Task PGR_MPQ3_AgentBridge_Reports_Bridged_LockedByAgent()
        {
            ScopeContext.Set(ctx => ctx.CurrentUser, TestContextAccessor.CurrentUserCollection.Consumer);
            var request = QuoteStartPrefillDataProvider.GetFullPrefillData(Addresses.GetAddress(AddressKey.GA_Athens));
            request.SourceName = SourceName;

            var (externalId, _) = await _progressiveHelper.CallQuoteStartAsync(request, navigate: true);
            var overviewPage = await _progressiveHelper.HandleThreePQIfPresentAsync();
            await _progressiveHelper.SubmitInterviewAsync(overviewPage);

            var platformApi = await _platformApiFactory.CreateApiClientAsync();
            var quoteStatusRequest = QuoteStatusDataProvider.CreateQuoteStatusData(externalId!, SourceName);

            // Precondition: the agent needs a rated quote to bridge.
            // Read rather than poll for Complete: a quote that never rates would otherwise throw a bare
            // cancellation instead of naming the status it did reach.
            var ratedStatus = await _logger.ExecuteStepAsync("Wait for the consumer quote to finish rating", async () =>
            {
                return await RetryHelper.RetryAsync(
                    async () => (await platformApi.QuoteStatusAsync(quoteStatusRequest)).Content!,
                    status => string.Equals(status.QuoteStatus, nameof(QuoteStatus.Complete), StringComparison.OrdinalIgnoreCase),
                    maxAttempts: 20,
                    delay: TimeSpan.FromSeconds(6));
            });

            Assert.That(ratedStatus.QuoteStatus, Is.EqualTo(nameof(QuoteStatus.Complete)),
                $"Precondition failed — the consumer quote did not rate, so there is nothing rated for this scenario to act on. Actual: {ratedStatus.QuoteStatus} / {ratedStatus.SecondaryStatus}");

            await _logger.ExecuteStepAsync("Agent takes the quote over and drives it to the carrier bridge", async () =>
            {
                var agentOverview = await _paaHelper.OpenQuoteAsAgentAndEnterEditModeAsync(externalId!);

                var carrierQuestions = await Executor.ExecuteToPage<HQXAgent_CarrierQuestionsPage>(
                    FlowType.PgrHomeFlow,
                    agentOverview,
                    fillForms: false,
                    pageCallbacks: AgentTakeoverAnswers);

                await _paaHelper.AnswerBridgeCarrierQuestionsAndContinueAsync(carrierQuestions, CarrierEnums.Homesite);
                await BrowserManager.SwitchToLastTabAsync();
            });

            ScopeContext.Set(ctx => ctx.CurrentUser, TestContextAccessor.CurrentUserCollection.Consumer);
            platformApi = await _platformApiFactory.CreateApiClientAsync();

            var bridgedStatus = await _logger.ExecuteStepAsync("Poll QuoteStatus from Platform API until the agent's bridge lands", async () =>
            {
                return (await platformApi.QuoteStatusWithPollingAsync(
                    quoteStatusRequest,
                    QuoteStatus.Bridged)).Content!;
            });

            const string bridgeAction = "Bridge";

            // GetQuote /notes is an agent/admin-authenticated read; switch off the consumer key that drove the quote.
            ScopeContext.Set(ctx => ctx.CurrentUser, TestContextAccessor.CurrentUserCollection.Admin);

            var bridgeNote = await _logger.ExecuteStepAsync("Poll GetQuote /notes until the Bridge note appears", async () =>
            {
                return await _pollyRetryService.ExecuteWithRetryAsync(async () =>
                {
                    var notes = (await _getQuoteApi.GetNotesByApplicationIdAsync(externalId!)).Content ?? [];
                    var note = notes.FirstOrDefault(n => string.Equals(n.Action, bridgeAction, StringComparison.OrdinalIgnoreCase));
                    return note ?? throw new InvalidOperationException($"'{bridgeAction}' note not yet available in GetQuote API — retrying.");
                });
            });

            Assert.Multiple(() =>
            {
                Assert.That(bridgedStatus.QuoteStatus, Is.EqualTo(nameof(QuoteStatus.Bridged)),
                    $"QuoteStatus was not Bridged after the agent bridged the quote. Actual: {bridgedStatus.QuoteStatus}");
                Assert.That(bridgedStatus.SecondaryStatus, Is.EqualTo(nameof(QuoteSecondaryStatus.LockedByAgent)).IgnoreCase,
                    $"SecondaryStatus was not LockedByAgent. The agent bridged from edit mode, so the lock must survive the bridge. Actual: {bridgedStatus.SecondaryStatus}");
                Assert.That(bridgeNote.Description, Does.Not.Contain("by Consumer"),
                    $"The Bridge note credits the consumer for a bridge the agent performed. Actual: {bridgeNote.Description}");
            });
        }

        [Test]
        [Tenant(Tenant.PROGRESSIVEPL)]
        [Category("MPQ3")]
        [Category("Regression")]
        [TestCaseId(251506)]
        [Description("Three or more farm animals on the property is not a kickout — the quote is submitted and every carrier answers, Homesite with a partner declination and ASI with a UUD — so the consumer reaches the end with nothing to show. That is Complete / NoVisibleResults, which the status pair has to distinguish from a quote that never qualified. Lands RED by design: the quote reports DNQ / DNQ instead, the same misassignment family as bug 249733.")]
        [Author(Author.Helen)]
        public async Task PGR_MPQ3_FarmAnimals_Reports_Complete_NoVisibleResults()
        {
            ScopeContext.Set(ctx => ctx.CurrentUser, TestContextAccessor.CurrentUserCollection.Consumer);
            var request = QuoteStartPrefillDataProvider.GetFullPrefillData(Addresses.GetAddress(AddressKey.LA_Gonzales));
            request.SourceName = SourceName;

            var (externalId, _) = await _progressiveHelper.CallQuoteStartAsync(request, navigate: true);
            var overviewPage = await _progressiveHelper.HandleThreePQIfPresentAsync();

            var farmAnimals = new Dictionary<string, string>
            {
                [AnimalsOnThePremises_None] = "true",
                [AnimalsOnThePremises_Farm3orMore] = "true",
            };

            await _progressiveHelper.SubmitInterviewAsync(overviewPage, farmAnimals);

            var platformApi = await _platformApiFactory.CreateApiClientAsync();

            // Polls the primary the quote actually settles at, not the expected one: Complete never
            // arrives, and waiting it out would throw a bare cancellation in place of the finding.
            var quoteStatus = await _logger.ExecuteStepAsync("Poll QuoteStatus from Platform API until the submission settles", async () =>
            {
                var quoteStatusRequest = QuoteStatusDataProvider.CreateQuoteStatusData(externalId!, SourceName);
                var response = (await platformApi.QuoteStatusWithPollingAsync(
                    quoteStatusRequest,
                    QuoteStatus.DNQ)).Content!;
                return response;
            });

            // GetQuote is an agent/admin-authenticated read; switch off the consumer key that drove the quote.
            ScopeContext.Set(ctx => ctx.CurrentUser, TestContextAccessor.CurrentUserCollection.Admin);

            var carrierResults = await _logger.ExecuteStepAsync("Read how each carrier responded to the submission", async () =>
            {
                return (await _getQuoteApi.GetQuotesByApplicationIdAsync(externalId!)).Content ?? [];
            });

            // A result Status of SubmissionReferral is how a carrier UUD surfaces in GetQuote.
            var outcomes = string.Join(", ", carrierResults.Select(q => $"{q.Carrier}={q.Status}"));

            Assert.Multiple(() =>
            {
                Assert.That(quoteStatus.SecondaryStatus, Is.EqualTo(nameof(QuoteSecondaryStatus.NoVisibleResults)),
                    $"SecondaryStatus was not NoVisibleResults. The submission ran and every carrier answered, so the consumer has no results to see — not a quote that failed to qualify. Actual: {quoteStatus.SecondaryStatus}. Carrier outcomes: {outcomes}.");
                Assert.That(quoteStatus.QuoteStatus, Is.EqualTo(nameof(QuoteStatus.Complete)),
                    $"QuoteStatus was not Complete. Every carrier answering is still a completed submission, not a quote that failed to qualify. Actual: {quoteStatus.QuoteStatus}.");
            });
        }

        [Test]
        [Tenant(Tenant.PROGRESSIVEPL)]
        [Category("MPQ3")]
        [Category("Regression")]
        [TestCaseId(251505)]
        [Description("An agent taking over a quote the consumer has already bridged adds the lock without disturbing the bridge: the quote stays ConsumerBridged and gains LockedByAgent. This is the pair FSD 6.1 says cannot exist — it has the primary reverting to Complete — while table 6.4 lists it as row 3. Table 6.4 is the one that matches the platform.")]
        [Author(Author.Helen)]
        public async Task PGR_MPQ3_AgentLocksBridgedQuote_Reports_ConsumerBridged_LockedByAgent()
        {
            ScopeContext.Set(ctx => ctx.CurrentUser, TestContextAccessor.CurrentUserCollection.Consumer);
            var request = QuoteStartPrefillDataProvider.GetFullPrefillData(Addresses.GetAddress(AddressKey.GA_Athens));
            request.SourceName = SourceName;

            var (externalId, _) = await _progressiveHelper.CallQuoteStartAsync(request, navigate: true);
            var overviewPage = await _progressiveHelper.HandleThreePQIfPresentAsync();
            await _progressiveHelper.SubmitInterviewAsync(overviewPage);

            var platformApi = await _platformApiFactory.CreateApiClientAsync();
            var quoteStatusRequest = QuoteStatusDataProvider.CreateQuoteStatusData(externalId!, SourceName);

            // Read rather than poll for Complete: a quote that never rates would otherwise throw a bare
            // cancellation instead of naming the status it did reach.
            var ratedStatus = await _logger.ExecuteStepAsync("Wait for the consumer quote to finish rating", async () =>
            {
                return await RetryHelper.RetryAsync(
                    async () => (await platformApi.QuoteStatusAsync(quoteStatusRequest)).Content!,
                    status => string.Equals(status.QuoteStatus, nameof(QuoteStatus.Complete), StringComparison.OrdinalIgnoreCase),
                    maxAttempts: 20,
                    delay: TimeSpan.FromSeconds(6));
            });

            Assert.That(ratedStatus.QuoteStatus, Is.EqualTo(nameof(QuoteStatus.Complete)),
                $"Precondition failed — the consumer quote did not rate, so there is nothing rated for this scenario to act on. Actual: {ratedStatus.QuoteStatus} / {ratedStatus.SecondaryStatus}");

            ScopeContext.Set(ctx => ctx.CurrentUser, TestContextAccessor.CurrentUserCollection.Admin);

            var ratedResult = await _logger.ExecuteStepAsync("Read the rated result the consumer bridged to", async () =>
            {
                var quotes = (await _getQuoteApi.GetQuotesByApplicationIdAsync(externalId!)).Content ?? [];
                return quotes.FirstOrDefault(q => string.Equals(q.Status, "Success", StringComparison.OrdinalIgnoreCase))
                    ?? throw new InvalidOperationException(
                        $"The completed quote carries no successful carrier result. Results returned: {quotes.Count}");
            });

            ScopeContext.Set(ctx => ctx.CurrentUser, TestContextAccessor.CurrentUserCollection.Consumer);
            platformApi = await _platformApiFactory.CreateApiClientAsync();

            await _logger.ExecuteStepAsync("Consumer bridges, reported to BOLT as a Bridge CRMNote", async () =>
            {
                var crmNoteRequest = CRMNoteDataProvider.CreateCRMNoteData(
                    externalId!, SourceName, ratedResult.Carrier!, ratedResult.Lob!, NoteType.Bridge);
                return await platformApi.CRMNoteAsync(crmNoteRequest).EnsureSuccessContentAsync();
            });

            await _logger.ExecuteStepAsync("Wait for the consumer bridge to land", async () =>
            {
                return (await platformApi.QuoteStatusWithPollingAsync(
                    quoteStatusRequest,
                    QuoteStatus.ConsumerBridged,
                    QuoteSecondaryStatus.None)).Content!;
            });

            await _logger.ExecuteStepAsync("Agent takes the bridged quote over", async () =>
            {
                await _paaHelper.OpenQuoteAsAgentAndEnterEditModeAsync(externalId!);
            });

            const string editAction = "Edit Quote";

            // The primary does not move when an already-bridged quote is locked, so the Edit Quote note
            // is the signal that the takeover registered — polling the status would return immediately.
            ScopeContext.Set(ctx => ctx.CurrentUser, TestContextAccessor.CurrentUserCollection.Admin);

            await _logger.ExecuteStepAsync("Poll GetQuote /notes until the agent's Edit Quote note appears", async () =>
            {
                return await _pollyRetryService.ExecuteWithRetryAsync(async () =>
                {
                    var notes = (await _getQuoteApi.GetNotesByApplicationIdAsync(externalId!)).Content ?? [];
                    var note = notes.FirstOrDefault(n => string.Equals(n.Action, editAction, StringComparison.OrdinalIgnoreCase));
                    return note ?? throw new InvalidOperationException($"'{editAction}' note not yet available in GetQuote API — retrying.");
                });
            });

            ScopeContext.Set(ctx => ctx.CurrentUser, TestContextAccessor.CurrentUserCollection.Consumer);
            platformApi = await _platformApiFactory.CreateApiClientAsync();

            var lockedStatus = await _logger.ExecuteStepAsync("Read QuoteStatus once the takeover has registered", async () =>
            {
                return await platformApi.QuoteStatusAsync(quoteStatusRequest).EnsureSuccessContentAsync();
            });

            Assert.Multiple(() =>
            {
                Assert.That(lockedStatus.QuoteStatus, Is.EqualTo(nameof(QuoteStatus.ConsumerBridged)),
                    $"QuoteStatus was not ConsumerBridged. An agent lock must not erase the consumer's bridge. Actual: {lockedStatus.QuoteStatus}");
                Assert.That(lockedStatus.SecondaryStatus, Is.EqualTo(nameof(QuoteSecondaryStatus.LockedByAgent)).IgnoreCase,
                    $"SecondaryStatus was not LockedByAgent. Actual: {lockedStatus.SecondaryStatus}");
            });
        }

        [Test]
        [Tenant(Tenant.PROGRESSIVEPL)]
        [Category("MPQ3")]
        [Category("Regression")]
        [TestCaseId(251504)]
        [Description("A consumer bridges to a carrier from Progressive's page, then an agent picks the same quote up and bridges it too. The later agent bridge wins the primary status, and both bridges are kept as separate notes rather than one overwriting the other.")]
        [Author(Author.Helen)]
        public async Task PGR_MPQ3_ConsumerThenAgentBridge_Reports_Bridged_LockedByAgent()
        {
            ScopeContext.Set(ctx => ctx.CurrentUser, TestContextAccessor.CurrentUserCollection.Consumer);
            var request = QuoteStartPrefillDataProvider.GetFullPrefillData(Addresses.GetAddress(AddressKey.GA_Athens));
            request.SourceName = SourceName;

            var (externalId, _) = await _progressiveHelper.CallQuoteStartAsync(request, navigate: true);
            var overviewPage = await _progressiveHelper.HandleThreePQIfPresentAsync();
            await _progressiveHelper.SubmitInterviewAsync(overviewPage);

            var platformApi = await _platformApiFactory.CreateApiClientAsync();
            var quoteStatusRequest = QuoteStatusDataProvider.CreateQuoteStatusData(externalId!, SourceName);

            // Read rather than poll for Complete: a quote that never rates would otherwise throw a bare
            // cancellation instead of naming the status it did reach.
            var ratedStatus = await _logger.ExecuteStepAsync("Wait for the consumer quote to finish rating", async () =>
            {
                return await RetryHelper.RetryAsync(
                    async () => (await platformApi.QuoteStatusAsync(quoteStatusRequest)).Content!,
                    status => string.Equals(status.QuoteStatus, nameof(QuoteStatus.Complete), StringComparison.OrdinalIgnoreCase),
                    maxAttempts: 20,
                    delay: TimeSpan.FromSeconds(6));
            });

            Assert.That(ratedStatus.QuoteStatus, Is.EqualTo(nameof(QuoteStatus.Complete)),
                $"Precondition failed — the consumer quote did not rate, so there is nothing rated for this scenario to act on. Actual: {ratedStatus.QuoteStatus} / {ratedStatus.SecondaryStatus}");

            ScopeContext.Set(ctx => ctx.CurrentUser, TestContextAccessor.CurrentUserCollection.Admin);

            var ratedResult = await _logger.ExecuteStepAsync("Read the rated result the consumer bridged to", async () =>
            {
                var quotes = (await _getQuoteApi.GetQuotesByApplicationIdAsync(externalId!)).Content ?? [];
                return quotes.FirstOrDefault(q => string.Equals(q.Status, "Success", StringComparison.OrdinalIgnoreCase))
                    ?? throw new InvalidOperationException(
                        $"The completed quote carries no successful carrier result. Results returned: {quotes.Count}");
            });

            ScopeContext.Set(ctx => ctx.CurrentUser, TestContextAccessor.CurrentUserCollection.Consumer);
            platformApi = await _platformApiFactory.CreateApiClientAsync();

            await _logger.ExecuteStepAsync("Consumer bridges first, reported to BOLT as a Bridge CRMNote", async () =>
            {
                var crmNoteRequest = CRMNoteDataProvider.CreateCRMNoteData(
                    externalId!, SourceName, ratedResult.Carrier!, ratedResult.Lob!, NoteType.Bridge);
                return await platformApi.CRMNoteAsync(crmNoteRequest).EnsureSuccessContentAsync();
            });

            await _logger.ExecuteStepAsync("Wait for the consumer bridge to land", async () =>
            {
                return (await platformApi.QuoteStatusWithPollingAsync(
                    quoteStatusRequest,
                    QuoteStatus.ConsumerBridged,
                    QuoteSecondaryStatus.None)).Content!;
            });

            await _logger.ExecuteStepAsync("Agent then takes the same quote over and bridges it as well", async () =>
            {
                var agentOverview = await _paaHelper.OpenQuoteAsAgentAndEnterEditModeAsync(externalId!);

                var carrierQuestions = await Executor.ExecuteToPage<HQXAgent_CarrierQuestionsPage>(
                    FlowType.PgrHomeFlow,
                    agentOverview,
                    fillForms: false,
                    pageCallbacks: AgentTakeoverAnswers);

                await _paaHelper.AnswerBridgeCarrierQuestionsAndContinueAsync(carrierQuestions, CarrierEnums.Homesite);
                await BrowserManager.SwitchToLastTabAsync();
            });

            ScopeContext.Set(ctx => ctx.CurrentUser, TestContextAccessor.CurrentUserCollection.Consumer);
            platformApi = await _platformApiFactory.CreateApiClientAsync();

            var finalStatus = await _logger.ExecuteStepAsync("Poll QuoteStatus from Platform API until the agent's bridge lands", async () =>
            {
                return (await platformApi.QuoteStatusWithPollingAsync(
                    quoteStatusRequest,
                    QuoteStatus.Bridged)).Content!;
            });

            const string bridgeAction = "Bridge";

            // GetQuote /notes is an agent/admin-authenticated read; switch off the consumer key that drove the quote.
            ScopeContext.Set(ctx => ctx.CurrentUser, TestContextAccessor.CurrentUserCollection.Admin);

            var bridgeNotes = await _logger.ExecuteStepAsync("Poll GetQuote /notes until both Bridge notes appear", async () =>
            {
                return await _pollyRetryService.ExecuteWithRetryAsync(async () =>
                {
                    var notes = (await _getQuoteApi.GetNotesByApplicationIdAsync(externalId!)).Content ?? [];
                    var bridges = notes
                        .Where(n => string.Equals(n.Action, bridgeAction, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                    return bridges.Count >= 2
                        ? bridges
                        : throw new InvalidOperationException($"Only {bridges.Count} '{bridgeAction}' note(s) so far — retrying.");
                });
            });

            Assert.Multiple(() =>
            {
                Assert.That(finalStatus.QuoteStatus, Is.EqualTo(nameof(QuoteStatus.Bridged)),
                    $"QuoteStatus was not Bridged. The agent bridged after the consumer, so the agent's bridge is the one that counts. Actual: {finalStatus.QuoteStatus}");
                Assert.That(finalStatus.SecondaryStatus, Is.EqualTo(nameof(QuoteSecondaryStatus.LockedByAgent)).IgnoreCase,
                    $"SecondaryStatus was not LockedByAgent. Actual: {finalStatus.SecondaryStatus}");
                Assert.That(bridgeNotes.Any(n => n.Description?.Contains("by Consumer", StringComparison.OrdinalIgnoreCase) == true), Is.True,
                    $"The consumer's Bridge note is missing — the agent's bridge overwrote it. Notes: {string.Join(" | ", bridgeNotes.Select(n => n.Description))}");
                Assert.That(bridgeNotes.Any(n => n.Description?.Contains("by Consumer", StringComparison.OrdinalIgnoreCase) == false), Is.True,
                    $"The agent's Bridge note is missing. Notes: {string.Join(" | ", bridgeNotes.Select(n => n.Description))}");
            });
        }

        [Test]
        [Tenant(Tenant.PROGRESSIVEPL)]
        [Category("MPQ3")]
        [Category("Regression")]
        [TestCaseId(251817)]
        [Description("An agent who retrieves an MPQ3 quote and records the sale takes it to Sold, the highest-priority status after NotFound — Sold asserts a policy header exists, not that the quote had results. The agent never edited it, so nothing locks and the secondary stays None.")]
        [Author(Author.Helen)]
        public async Task PGR_MPQ3_AgentSellsQuote_Reports_Sold_None()
        {
            ScopeContext.Set(ctx => ctx.CurrentUser, TestContextAccessor.CurrentUserCollection.Consumer);
            var request = QuoteStartPrefillDataProvider.GetFullPrefillData(Addresses.GetAddress(AddressKey.OH));
            request.SourceName = SourceName;

            // Opened but never submitted: Sold depends on the policy header, not on results, so staying
            // clear of submission keeps this test off the carrier-appetite path.
            var (externalId, _) = await _progressiveHelper.CallQuoteStartAsync(request, navigate: true);
            await _progressiveHelper.HandleThreePQIfPresentAsync();

            var platformApi = await _platformApiFactory.CreateApiClientAsync();
            var quoteStatusRequest = QuoteStatusDataProvider.CreateQuoteStatusData(externalId!, SourceName);

            await _logger.ExecuteStepAsync("Agent retrieves the quote without editing it and records the sale", async () =>
            {
                var agentOverview = await _paaHelper.OpenQuoteAsAgentAsync(externalId!);
                var notePopup = await agentOverview.ClickCreateNoteAsync();
                await notePopup.CreateSoldNoteAndConfirmAsync(new SoldNoteFormData
                {
                    PolicyNumber = SoldNotePolicyTestData.RandomDigits(8),
                    Product = SoldNotePolicyTestData.DefaultProduct,
                    EffectiveDate = DateTime.Today.AddDays(7).ToString("MM/dd/yyyy"),
                    ParentCompany = HomesiteParentCompany,
                    Premium = SoldNotePolicyTestData.DefaultPremium
                });
            });

            ScopeContext.Set(ctx => ctx.CurrentUser, TestContextAccessor.CurrentUserCollection.Consumer);
            platformApi = await _platformApiFactory.CreateApiClientAsync();

            // Read until Sold appears rather than polling for it: RetryAsync returns the last status it
            // saw instead of throwing, so if the sale never registers the assertion names what it found.
            var soldStatus = await _logger.ExecuteStepAsync("Read QuoteStatus from Platform API until the sale lands", async () =>
            {
                return await RetryHelper.RetryAsync(
                    async () => (await platformApi.QuoteStatusAsync(quoteStatusRequest)).Content!,
                    status => string.Equals(status.QuoteStatus, nameof(QuoteStatus.Sold), StringComparison.OrdinalIgnoreCase),
                    maxAttempts: 20,
                    delay: TimeSpan.FromSeconds(6));
            });

            Assert.Multiple(() =>
            {
                Assert.That(soldStatus.QuoteStatus, Is.EqualTo(nameof(QuoteStatus.Sold)),
                    $"QuoteStatus was not Sold. A policy header exists for the quote, and Sold outranks every status but NotFound. Actual: {soldStatus.QuoteStatus}");
                Assert.That(soldStatus.SecondaryStatus, Is.EqualTo(nameof(QuoteSecondaryStatus.None)),
                    $"SecondaryStatus was not None. Recording a sale is not an edit, so nothing should have locked the quote. Actual: {soldStatus.SecondaryStatus}");
            });
        }

        [Test]
        [Tenant(Tenant.PROGRESSIVEPL)]
        [Category("MPQ3")]
        [Category("Regression")]
        [TestCaseId(251818)]
        [Description("An agent who edits a quote and then sells it produces the pair FSD 6.3.2 and issue 17.17 call for: Sold outranks locked-by-PAA, so the sale takes the primary and the lock survives as the secondary.")]
        [Author(Author.Helen)]
        public async Task PGR_MPQ3_AgentSellsEditedQuote_Reports_Sold_LockedByAgent()
        {
            ScopeContext.Set(ctx => ctx.CurrentUser, TestContextAccessor.CurrentUserCollection.Consumer);
            var request = QuoteStartPrefillDataProvider.GetFullPrefillData(Addresses.GetAddress(AddressKey.OH));
            request.SourceName = SourceName;

            // Opened but never submitted: Sold depends on the policy header, not on results, so staying
            // clear of submission keeps this test off the carrier-appetite path.
            var (externalId, _) = await _progressiveHelper.CallQuoteStartAsync(request, navigate: true);
            await _progressiveHelper.HandleThreePQIfPresentAsync();

            var platformApi = await _platformApiFactory.CreateApiClientAsync();
            var quoteStatusRequest = QuoteStatusDataProvider.CreateQuoteStatusData(externalId!, SourceName);

            await _logger.ExecuteStepAsync("Agent takes the quote over in edit mode, then records the sale", async () =>
            {
                var agentOverview = await _paaHelper.OpenQuoteAsAgentAndEnterEditModeAsync(externalId!);

                var notePopup = await agentOverview.ClickCreateNoteAsync();
                await notePopup.CreateSoldNoteAndConfirmAsync(new SoldNoteFormData
                {
                    PolicyNumber = SoldNotePolicyTestData.RandomDigits(8),
                    Product = SoldNotePolicyTestData.DefaultProduct,
                    EffectiveDate = DateTime.Today.AddDays(7).ToString("MM/dd/yyyy"),
                    ParentCompany = HomesiteParentCompany,
                    Premium = SoldNotePolicyTestData.DefaultPremium
                });
            });

            ScopeContext.Set(ctx => ctx.CurrentUser, TestContextAccessor.CurrentUserCollection.Consumer);
            platformApi = await _platformApiFactory.CreateApiClientAsync();

            // Read until Sold appears rather than polling for it: RetryAsync returns the last status it
            // saw instead of throwing, so if the sale never registers the assertion names what it found.
            var soldStatus = await _logger.ExecuteStepAsync("Read QuoteStatus from Platform API until the sale lands", async () =>
            {
                return await RetryHelper.RetryAsync(
                    async () => (await platformApi.QuoteStatusAsync(quoteStatusRequest)).Content!,
                    status => string.Equals(status.QuoteStatus, nameof(QuoteStatus.Sold), StringComparison.OrdinalIgnoreCase),
                    maxAttempts: 20,
                    delay: TimeSpan.FromSeconds(6));
            });

            Assert.Multiple(() =>
            {
                Assert.That(soldStatus.QuoteStatus, Is.EqualTo(nameof(QuoteStatus.Sold)),
                    $"QuoteStatus was not Sold. The agent holds the quote in edit mode, and Sold has to outrank that. Actual: {soldStatus.QuoteStatus}");
                Assert.That(soldStatus.SecondaryStatus, Is.EqualTo(nameof(QuoteSecondaryStatus.LockedByAgent)).IgnoreCase,
                    $"SecondaryStatus was not LockedByAgent. Sold takes the primary but the agent still holds the quote, so the lock has to survive as the secondary. Actual: {soldStatus.SecondaryStatus}");
            });
        }
    }
}
