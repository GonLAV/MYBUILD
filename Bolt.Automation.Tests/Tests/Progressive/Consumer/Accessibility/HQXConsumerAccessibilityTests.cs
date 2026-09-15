using System.Text;
using System.Text.RegularExpressions;
using Bolt.Automation.ApiClients.PlatformApi.Entities.Quote.QuoteStart;
using Bolt.Automation.Common;
using Bolt.Automation.Common.Enums;
using Bolt.Automation.Common.Logging.Extentions;
using Bolt.Automation.Common.Logging.Mongo;
using Bolt.Automation.FrontEnds.Executor.Helpers;
using Bolt.Automation.FrontEnds.Interfaces;
using Bolt.Automation.FrontEnds.PlaywrightBase.Accessibility;
using Bolt.Automation.FrontEnds.Projects.HQXConsumer.Flows;
using Bolt.Automation.FrontEnds.Projects.HQXConsumer.Pages;
using Bolt.Automation.FrontEnds.Projects.HQXConsumer.Services.Accessibility;
using Bolt.Automation.TestDataProvider.Providers.PlatformAPIDataProvider;
using Bolt.Automation.TestDataProvider.TestData.PlatformAPITestData;
using Bolt.Automation.Tests.TestExtension.Attributes;
using Bolt.Automation.Tests.TestExtension.Base;
using Bolt.Automation.Tests.TestHelpers.Progressive;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using NUnit.Framework;
using AuthorAttribute = Bolt.Automation.Tests.TestExtension.Attributes.AuthorAttribute;

namespace Bolt.Automation.Tests.Tests.Progressive.Consumer.Accessibility;

[TestFixture]
public class HQXConsumerAccessibilityTests : ProgressiveUITestBase
{
    // Report-only phase: nothing reaches Critical today, so the suite lands green while the
    // Phase 0 findings are filed and fixed. Drop to Serious to turn the gate on.
    private const AccessibilityImpact GateThreshold = AccessibilityImpact.Critical;

    /// <summary>Multi-product quote source — the only source that renders the LOB progress bar.</summary>
    private const string Mpq3SourceName = "MPQ3";

    private static readonly string BaselinePath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "TestData", "Accessibility", "hqx-consumer-known-issues.json");

    private IAccessibilityScanner _scanner = null!;
    private KeyboardTraversalHelper _keyboard = null!;
    private AccessibilityTestHelper _a11yHelper = null!;

    protected override void ResolveServices()
    {
        base.ResolveServices();
        _scanner = _uiTestScope.ServiceProvider.GetRequiredService<IAccessibilityScanner>();
        _keyboard = _uiTestScope.ServiceProvider.GetRequiredService<KeyboardTraversalHelper>();
    }

    protected override void InitializeComponents()
    {
        base.InitializeComponents();
        _a11yHelper = new AccessibilityTestHelper(_scanner, _keyboard, _pageHelper!, _logger);
    }

    [Test]
    [Tenant(Tenant.PROGRESSIVEPL)]
    [Category("Accessibility")]
    [Category("HQX2")]
    [Author(Author.Helen)]
    [TestCaseId(252319)]
    [Description("Scans every non-mutating state of the HQXShortFlow consumer journey against WCAG 2.1 AA and uploads the findings, gating on impact above the known-issues baseline.")]
    public async Task PGR_HQX2_Accessibility_Axe_Scan_ShortFlow()
    {
        var findings = new AccessibilityFindings();
        var overviewPage = await StartConsumerQuoteAsync();

        await _logger.ExecuteStepAsync("Walk the short flow, scanning every state of each screen", async () =>
        {
            await Executor.ExecuteToPage<HQXConsumer_RatesPage>(
                FlowType.HQXShortFlow,
                overviewPage,
                formData: null,
                fillForms: true,
                pagesToSkip: null,
                pageCallbacks: null,
                perPageAction: page => _a11yHelper.SweepNonMutatingStatesAsync(_currentPage!, page, findings));

            // perPageAction stops before the end page, so Rates needs its own sweep.
            await _a11yHelper.SweepNonMutatingStatesAsync(
                _currentPage!, PageFactory.CreatePage<HQXConsumer_RatesPage>(), findings);
        });

        // A passing test never reaches the failure-only teardown capture, so upload explicitly.
        await UploadArtifactBytesAsync(
            Encoding.UTF8.GetBytes(findings.ToJson()),
            $"accessibility_{TestContext.CurrentContext.Test.Name}.json",
            "application/json",
            ArtifactType.AccessibilityReport);

        var baseline = AccessibilityBaselineStore.Load(BaselinePath, _logger);
        var blocking = findings.Blocking(GateThreshold, baseline);

        Assert.That(findings.StatesScanned, Is.GreaterThanOrEqualTo(9),
            $"Expected at least 9 screen states to be scanned across the short flow but only {findings.StatesScanned} were — the sweep is under-covering.");

        Assert.That(blocking, Is.Empty,
            $"Accessibility violations at or above {GateThreshold} that are not in the known-issues baseline:{System.Environment.NewLine}" +
            AccessibilityFindings.Describe(blocking));
    }

    [Test]
    [Tenant(Tenant.PROGRESSIVEPL)]
    [Category("Accessibility")]
    [Category("HQX2")]
    [Author(Author.Helen)]
    [TestCaseId(252320)]
    [Description("Each conditional child question on the Discounts page must announce its appearance non-visually, via aria-expanded, aria-controls, a live region, or a focus move.")]
    public async Task PGR_HQX2_Accessibility_Conditional_Reveals_Are_Announced()
    {
        // The utilities-replaced questions only render for an older home — see TC 248329.
        var overviewPage = await StartConsumerQuoteAsync(
            QuoteStartPrefillDataProvider.GetHomeBuiltAgePrefillData(
                Addresses.GetAddress(AddressKey.FL_Bradenton), ageInYears: 20));

        var discountsPage = await _logger.ExecuteStepAsync("Advance to the Discounts page", async () =>
            await Executor.ExecuteToPage<HQXConsumer_DiscountsPage>(
                FlowType.HQXShortFlow, overviewPage, fillForms: true));

        var reveals = HQXConsumerAccessibilityStates.RevealsFor(typeof(HQXConsumer_DiscountsPage));
        var announcements = new List<RevealAnnouncement>();

        await _logger.ExecuteStepAsync($"Drive {reveals.Count} conditional reveal(s) and inspect how each is exposed", async () =>
        {
            foreach (var reveal in reveals)
                announcements.Add(await _a11yHelper.DriveRevealAsync(discountsPage, reveal));
        });

        var drivable = announcements.Where(a => a.ParentPresent).ToList();
        var skipped = announcements.Where(a => !a.ParentPresent).Select(a => a.ParentField).ToList();

        Assert.That(reveals, Is.Not.Empty,
            "No conditional reveals were derived for the Discounts page — the registry-driven state enumeration has stopped working.");

        // Never let an un-drivable reveal read as a pass: say what this quote could not cover.
        Assert.That(drivable, Is.Not.Empty,
            $"None of the {reveals.Count} Discounts reveals could be driven on this quote, so nothing was actually verified. " +
            $"Parent controls absent: {string.Join(", ", skipped)}");

        Assert.Multiple(() =>
        {
            foreach (var announcement in drivable)
            {
                Assert.That(announcement.ChildBecameVisible, Is.True,
                    $"Setting '{announcement.ParentField}' did not reveal '{announcement.ChildField}', so its announcement could not be judged.");

                Assert.That(announcement.IsAnnounced, Is.True,
                    $"'{announcement.ChildField}' appeared with no non-visual announcement — a screen reader user gets no signal that a new question exists. {announcement}");
            }
        });

        if (skipped.Count > 0)
            TestContext.Out.WriteLine($"Not drivable on an HO3 quote (parent control absent): {string.Join(", ", skipped)}");
    }

    [Test]
    [Tenant(Tenant.PROGRESSIVEPL)]
    [Category("Accessibility")]
    [Category("HQX2")]
    [Author(Author.Helen)]
    [TestCaseId(91046)]
    [Description("Every interactive control on each short-flow screen is reachable by keyboard, with no focus trap and a visible focus indicator.")]
    public async Task PGR_HQX2_Accessibility_Keyboard_Traversal_ShortFlow()
    {
        var traversals = new List<(string Screen, KeyboardTraversalResult Result)>();
        var overviewPage = await StartConsumerQuoteAsync();

        await _logger.ExecuteStepAsync("Tab through every screen of the short flow", async () =>
        {
            await Executor.ExecuteToPage<HQXConsumer_RatesPage>(
                FlowType.HQXShortFlow,
                overviewPage,
                formData: null,
                fillForms: true,
                pagesToSkip: null,
                pageCallbacks: null,
                perPageAction: async page =>
                    traversals.Add((AccessibilityTestHelper.ScreenNameOf(page), await _a11yHelper.TraverseAsync(_currentPage!))));
        });

        Assert.Multiple(() =>
        {
            foreach (var (screen, result) in traversals)
            {
                Assert.That(result.Order, Is.Not.Empty,
                    $"Tabbing reached no interactive control at all on {screen} — the screen is unusable by keyboard.");

                Assert.That(result.TrapDetected, Is.False,
                    $"Keyboard focus stopped advancing on {screen} at '{result.TrapSignature}' — a keyboard user cannot get past it.");

                Assert.That(result.FocusableInsideAriaHidden, Is.Empty,
                    $"On {screen}, focus reached {result.FocusableInsideAriaHidden.Count()} control(s) inside an aria-hidden subtree — " +
                    $"a keyboard user can land on what a screen reader cannot see: {string.Join("; ", result.FocusableInsideAriaHidden)}");
            }
        });
    }

    [Test]
    [Tenant(Tenant.PROGRESSIVEPL)]
    [Category("Accessibility")]
    [Category("HQX2")]
    [Author(Author.Helen)]
    [TestCaseId(90930)]
    [Description("Advancing between interview screens moves focus into the new screen rather than dropping it on the document body.")]
    public async Task PGR_HQX2_Accessibility_Focus_Moves_On_Page_Transition()
    {
        var droppedOn = new List<string>();
        var overviewPage = await StartConsumerQuoteAsync();

        // BeforeFillForm, not perPageAction — see kb framework:accessibility. Overview is excluded:
        // it is the entry page, so focus on body there is ordinary page load, not a lost transition.
        async Task RecordFocus(IInterview page)
        {
            if (await _a11yHelper.IsFocusOnBodyAsync(_currentPage!))
                droppedOn.Add(AccessibilityTestHelper.ScreenNameOf(page));
        }

        await _logger.ExecuteStepAsync("Advance through the flow, checking where focus lands on each new screen", async () =>
        {
            await Executor.ExecuteToPage<HQXConsumer_RatesPage>(
                FlowType.HQXShortFlow,
                overviewPage,
                formData: null,
                fillForms: true,
                pagesToSkip: null,
                pageCallbacks: PageCallbackManager
                    .For<HQXConsumer_DetailsPage>(RecordFocus, PageCallbackManager.CallbackTiming.BeforeFillForm)
                    .And<HQXConsumer_DiscountsPage>(RecordFocus, PageCallbackManager.CallbackTiming.BeforeFillForm)
                    .And<HQXConsumer_OwnerPage>(RecordFocus, PageCallbackManager.CallbackTiming.BeforeFillForm));
        });

        Assert.That(droppedOn, Is.Empty,
            "Focus was left on the document body when these screens rendered, so a screen reader user is silently returned to the top of the page " +
            $"with no announcement of the new step: {string.Join(", ", droppedOn)}");
    }

    [Test]
    [Tenant(Tenant.PROGRESSIVEPL)]
    [Category("Accessibility")]
    [Category("HQX2")]
    [Author(Author.Helen)]
    [TestCaseId(252321)]
    [Description("WCAG 1.4.10 reflow: at a 320px viewport the interview screens must not scroll horizontally.")]
    public async Task PGR_HQX2_Accessibility_Reflow_At_320px()
    {
        var overflowing = new List<string>();
        var overviewPage = await StartConsumerQuoteAsync();

        await _logger.ExecuteStepAsync("Narrow the viewport to 320px and walk the flow checking for horizontal scroll", async () =>
        {
            await _currentPage!.SetViewportSizeAsync(320, 800);

            await Executor.ExecuteToPage<HQXConsumer_RatesPage>(
                FlowType.HQXShortFlow,
                overviewPage,
                formData: null,
                fillForms: true,
                pagesToSkip: null,
                pageCallbacks: null,
                perPageAction: async page =>
                {
                    if (await _a11yHelper.HasHorizontalOverflowAsync(_currentPage!))
                        overflowing.Add(AccessibilityTestHelper.ScreenNameOf(page));
                });
        });

        Assert.That(overflowing, Is.Empty,
            $"These screens scroll horizontally at a 320px viewport, so content is cut off for anyone zoomed in or on a small phone: {string.Join(", ", overflowing)}");
    }

    [Test]
    [Tenant(Tenant.PROGRESSIVEPL)]
    [Category("Accessibility")]
    [Category("HQX2")]
    [Author(Author.Helen)]
    [TestCaseId(238832)]
    [Description("WCAG 1.4.4 resize text: at 200% zoom the interview screens must not clip content or scroll horizontally.")]
    public async Task PGR_HQX2_Accessibility_Text_Zoom_200_Percent()
    {
        var overflowing = new List<string>();
        var overviewPage = await StartConsumerQuoteAsync();

        await _logger.ExecuteStepAsync("Zoom to 200% and walk the flow checking content still fits", async () =>
        {
            // CSS zoom approximates browser zoom; true browser zoom is not scriptable in Playwright.
            await _currentPage!.AddStyleTagAsync(new() { Content = ":root { zoom: 200%; }" });

            await Executor.ExecuteToPage<HQXConsumer_RatesPage>(
                FlowType.HQXShortFlow,
                overviewPage,
                formData: null,
                fillForms: true,
                pagesToSkip: null,
                pageCallbacks: null,
                perPageAction: async page =>
                {
                    if (await _a11yHelper.HasHorizontalOverflowAsync(_currentPage!))
                        overflowing.Add(AccessibilityTestHelper.ScreenNameOf(page));
                });
        });

        Assert.That(overflowing, Is.Empty,
            $"These screens scroll horizontally at 200% zoom, so text is cut off for anyone who needs to enlarge it: {string.Join(", ", overflowing)}");
    }

    [Test]
    [Tenant(Tenant.PROGRESSIVEPL)]
    [Category("Accessibility")]
    [Category("HQX2")]
    [Category("MPQ3")]
    [Author(Author.Helen)]
    [TestCaseId(252322)]
    [Description("WCAG 3.2.1 On Focus: tabbing through the MPQ3 progress bar must move focus to the next control, never navigate to that step.")]
    public async Task PGR_HQX2_MPQ3_Accessibility_Progress_Bar_Tab_Does_Not_Navigate()
    {
        var request = QuoteStartPrefillDataProvider.GetStandardPrefillData(Addresses.GetAddress(AddressKey.OH));
        request.SourceName = Mpq3SourceName;
        await StartConsumerQuoteAsync(request);

        // The MPQ3 progress bar renders as <nav> with a step per LOB; AUTO is step 1.
        var autoStep = _currentPage!.Locator("nav li.section")
            .Filter(new() { HasTextRegex = new Regex("auto", RegexOptions.IgnoreCase) })
            .First;

        var tabIndex = await _logger.ExecuteStepAsync("Locate the AUTO step in the progress bar", async () =>
        {
            await autoStep.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15000 });
            return await autoStep.GetAttributeAsync("tabindex");
        });

        var probe = await _logger.ExecuteStepAsync("Focus the AUTO step and press Tab twice", async () =>
        {
            await autoStep.FocusAsync();
            return await _a11yHelper.TabAndDetectNavigationAsync(_currentPage!, presses: 2);
        });

        Assert.Multiple(() =>
        {
            Assert.That(probe.Navigated, Is.False,
                "Tabbing away from the progress bar's AUTO step tore down the document instead of moving focus to the next " +
                $"control (started at '{probe.StartUrl}', settled at '{probe.EndUrl}'). A keyboard user cannot pass the " +
                "progress bar without being thrown into that step, and WCAG 3.2.1 forbids a change of context on focus. " +
                $"Focus reached before the teardown: {(probe.FocusTrail.Count == 0 ? "(none — the very first Tab triggered it)" : string.Join(" -> ", probe.FocusTrail))}");

            Assert.That(tabIndex, Is.Not.EqualTo("1"),
                "The AUTO step carries tabindex=\"1\". A positive tabindex pulls it ahead of every other control in the " +
                "document tab order, so keyboard users meet the progress bar before the form (WCAG 2.4.3).");
        });
    }

    // Not ProgressiveTestHelper.CreateConsumerQuoteAndSettleAsync: that uses full prefill to
    // auto-advance past the interview, and these tests need the interview screens rendered.
    private async Task<HQXConsumer_OverviewPage> StartConsumerQuoteAsync(QuoteStartRequestModel? request = null)
    {
        ScopeContext.Set(ctx => ctx.CurrentUser, TestContextAccessor.CurrentUserCollection.Consumer);

        request ??= QuoteStartPrefillDataProvider.GetStandardPrefillData(Addresses.GetAddress(AddressKey.OH));
        await _progressiveHelper.CallQuoteStartAsync(request, navigate: true);
        return await _progressiveHelper.HandleThreePQIfPresentAsync();
    }
}
