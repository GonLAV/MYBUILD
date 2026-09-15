using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.Interfaces;
using Bolt.Automation.FrontEnds.PlaywrightBase.Accessibility;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.HQXConsumer.Base;
using Bolt.Automation.FrontEnds.Projects.HQXConsumer.FormData;
using Bolt.Automation.FrontEnds.Projects.HQXConsumer.Pages;
using Bolt.Automation.FrontEnds.Projects.HQXConsumer.Services.Accessibility;
using Microsoft.Playwright;

namespace Bolt.Automation.Tests.TestHelpers.Progressive;

/// <summary>How a revealed question announces itself to assistive technology.</summary>
public sealed record RevealAnnouncement
{
    public required string ParentField { get; init; }
    public required string ChildField { get; init; }

    /// <summary>The parent control renders for this quote — reveals gated on another LOB are not drivable here.</summary>
    public required bool ParentPresent { get; init; }

    public required bool ChildBecameVisible { get; init; }
    public required bool ParentExposesExpandedState { get; init; }
    public required bool ParentControlsRevealedRegion { get; init; }
    public required bool RevealedRegionIsLive { get; init; }
    public required bool FocusMovedIntoReveal { get; init; }

    /// <summary>WCAG 4.1.3 / 3.2.2 are satisfied by any one of these mechanisms.</summary>
    public bool IsAnnounced =>
        ParentExposesExpandedState || ParentControlsRevealedRegion || RevealedRegionIsLive || FocusMovedIntoReveal;

    public override string ToString() =>
        $"{ParentField} -> {ChildField}: visible={ChildBecameVisible}, aria-expanded={ParentExposesExpandedState}, " +
        $"aria-controls={ParentControlsRevealedRegion}, live-region={RevealedRegionIsLive}, focus-moved={FocusMovedIntoReveal}";
}

/// <summary>Gathers accessibility evidence for the Progressive consumer flow. Tests do the asserting.</summary>
public class AccessibilityTestHelper(
    IAccessibilityScanner scanner,
    KeyboardTraversalHelper keyboard,
    IPageHelper pageHelper,
    IAutomationLogger logger)
{
    // Takes the element, not a selector: registry locators are a mix of CSS and XPath,
    // and document.querySelector cannot parse XPath.
    private const string AnnouncementScript = """
        (child) => {
            if (!child) return null;
            const wrapper = child.closest('.question-wrapper') || child;
            const region = wrapper.id ? wrapper.id : null;
            const live = !!wrapper.closest('[aria-live]') || wrapper.getAttribute('aria-live');
            const controllers = region
                ? document.querySelectorAll(`[aria-controls~="${region}"]`).length > 0
                : false;
            return {
                live: !!live,
                controlled: controllers,
                focusInside: !!(document.activeElement && wrapper.contains(document.activeElement))
            };
        }
        """;

    /// <summary>Scans a screen's default state, plus Overview's sections. Never mutates form data.</summary>
    public async Task SweepNonMutatingStatesAsync(IPage page, IInterview interview, AccessibilityFindings findings)
    {
        var screen = ScreenNameOf(interview);

        findings.Add(await scanner.ScanAsync(page, screen, HQXConsumerAccessibilityStates.DefaultState));

        if (interview is not HQXConsumer_OverviewPage overview) return;

        foreach (var section in HQXConsumerAccessibilityStates.OverviewSections)
        {
            await overview.ExpandSectionAsync(section);
            findings.Add(await scanner.ScanAsync(page, screen, $"section:{section}"));
        }
    }

    /// <summary>Tabs a screen and reports focus order, traps and focus indicators.</summary>
    public Task<KeyboardTraversalResult> TraverseAsync(IPage page) => keyboard.TraverseAsync(page);

    /// <summary>True when focus is left on the body — what an unhandled SPA route change does.</summary>
    public Task<bool> IsFocusOnBodyAsync(IPage page) => keyboard.IsFocusOnBodyAsync(page);

    /// <summary>Drives a reveal and reports how (or whether) its appearance is exposed non-visually.</summary>
    public async Task<RevealAnnouncement> DriveRevealAsync(HQXConsumerBase pageObject, ConditionalReveal reveal)
    {
        var page = pageObject.Page;
        var childField = reveal.ChildFields[0];
        var child = FieldRegistryHQXConsumer.Fields[childField];
        var childLabel = child.Label;

        // Pages tags the child, not the parent: a reveal can surface for a page whose parent control
        // only renders for another LOB (PLHighRiseCondo is condo-only). Report it, do not crash.
        if (!await ParentIsPresentAsync(page, reveal))
        {
            logger.Info($"Reveal '{reveal.StateName}' is not drivable on this quote — the parent control is not on the page.");
            return NotDrivable(reveal.ParentField, childField);
        }

        var expandedBefore = await ParentExpandedStateAsync(page, reveal.ParentField);
        var labelsBefore = await pageObject.GetQuestionLabelsAsync();

        logger.Info($"Driving reveal '{reveal.StateName}' to inspect how '{childField}' is announced.");
        await pageHelper.InteractWithField(reveal.ParentField, reveal.ParentValue);

        // Detect the reveal by the question label appearing, not by a value-substituted locator:
        // GetLocators lowercases the value and XPath contains() is case-sensitive.
        var visible = await WaitForQuestionLabelAsync(pageObject, labelsBefore, childLabel);
        var expandedAfter = await ParentExpandedStateAsync(page, reveal.ParentField);
        var probe = visible && childLabel is not null
            ? await QuestionWrapperFor(page, childLabel).EvaluateAsync<System.Text.Json.JsonElement?>(AnnouncementScript)
            : null;

        var announcement = new RevealAnnouncement
        {
            ParentField = reveal.ParentField,
            ChildField = childField,
            ParentPresent = true,
            ChildBecameVisible = visible,
            ParentExposesExpandedState = expandedBefore is not null && expandedBefore != expandedAfter,
            ParentControlsRevealedRegion = ReadFlag(probe, "controlled"),
            RevealedRegionIsLive = ReadFlag(probe, "live"),
            FocusMovedIntoReveal = ReadFlag(probe, "focusInside")
        };

        logger.Info($"Reveal announcement: {announcement}");
        return announcement;
    }

    /// <summary>Where a run of Tab presses left the browser.</summary>
    /// <param name="Navigated">Tab caused a change of context — a WCAG 3.2.1 failure.</param>
    public sealed record TabProbeResult(bool Navigated, string StartUrl, string EndUrl, IReadOnlyList<string> FocusTrail);

    /// <summary>
    /// Presses Tab from the currently focused element and reports whether it navigated.
    /// Survives the navigation it is looking for — reading focus destroys the JS context once the page goes.
    /// </summary>
    public async Task<TabProbeResult> TabAndDetectNavigationAsync(IPage page, int presses)
    {
        var startUrl = page.Url;
        var trail = new List<string>();

        for (var press = 1; press <= presses; press++)
        {
            await page.Keyboard.PressAsync("Tab");
            await Task.Delay(750);

            if (page.Url != startUrl)
            {
                logger.Warning($"Tab press {press} navigated from '{startUrl}' to '{page.Url}'.");
                return new TabProbeResult(true, startUrl, page.Url, trail);
            }

            try
            {
                trail.Add((await keyboard.DescribeFocusAsync(page))?.ToString() ?? "(body)");
            }
            catch (PlaywrightException)
            {
                // The context died mid-read: a document teardown, which is a change of context even
                // if the SPA settles back on the same URL. Let it land before reporting where it went.
                var destination = await SettledUrlAsync(page);
                logger.Warning($"Tab press {press} destroyed the execution context — the page navigated to '{destination}'.");
                return new TabProbeResult(true, startUrl, destination, trail);
            }
        }

        logger.Info($"{presses} Tab press(es) moved focus without navigating: {string.Join(" -> ", trail)}");
        return new TabProbeResult(false, startUrl, page.Url, trail);
    }

    private static async Task<string> SettledUrlAsync(IPage page)
    {
        try
        {
            await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded, new() { Timeout = 10000 });
        }
        catch (TimeoutException)
        {
            // Best effort — report whatever URL we have rather than losing the finding.
        }

        return page.Url;
    }

    /// <summary>The page scrolls horizontally — content has not reflowed to the viewport.</summary>
    public async Task<bool> HasHorizontalOverflowAsync(IPage page)
    {
        var overflow = await page.EvaluateAsync<int>(
            "() => document.documentElement.scrollWidth - document.documentElement.clientWidth");
        logger.Info($"Horizontal overflow: {overflow}px.");

        // Sub-pixel layout rounding routinely yields 1px; that is not a reflow failure.
        return overflow > 1;
    }

    public static string ScreenNameOf(IInterview interview) =>
        interview is HQXConsumerBase consumerPage ? consumerPage.PageDisplayName : interview.GetType().Name;

    /// <summary>Registry locators are a mix of CSS and XPath; Playwright needs XPath prefixed.</summary>
    private static ILocator ResolveLocator(IPage page, string selector) =>
        selector.StartsWith("//") ? page.Locator($"xpath={selector}") : page.Locator(selector);

    private static RevealAnnouncement NotDrivable(string parentField, string childField) => new()
    {
        ParentField = parentField,
        ChildField = childField,
        ParentPresent = false,
        ChildBecameVisible = false,
        ParentExposesExpandedState = false,
        ParentControlsRevealedRegion = false,
        RevealedRegionIsLive = false,
        FocusMovedIntoReveal = false
    };

    private async Task<bool> ParentIsPresentAsync(IPage page, ConditionalReveal reveal)
    {
        var parent = FieldRegistryHQXConsumer.Fields[reveal.ParentField];
        var selector = parent.GetLocators(reveal.ParentValue)[0];
        var locator = ResolveLocator(page, selector);

        // Attached is not enough: a parent gated to another LOB stays in the DOM but hidden.
        return await locator.CountAsync() > 0 && await locator.First.IsVisibleAsync();
    }

    private async Task<string?> ParentExpandedStateAsync(IPage page, string parentField)
    {
        var parent = FieldRegistryHQXConsumer.Fields[parentField];
        var selector = parent.GetLocators(parent.DefaultValue ?? string.Empty)[0];
        var locator = ResolveLocator(page, selector);

        return await locator.CountAsync() == 0 ? null : await locator.First.GetAttributeAsync("aria-expanded");
    }

    /// <summary>The revealed question's wrapper, located by its label text.</summary>
    private static ILocator QuestionWrapperFor(IPage page, string label) =>
        page.Locator(".question-wrapper")
            .Filter(new LocatorFilterOptions { HasText = label })
            .First;

    /// <summary>Polls the page's visible question labels until the child's label appears.</summary>
    private static async Task<bool> WaitForQuestionLabelAsync(
        HQXConsumerBase pageObject, IReadOnlyList<string> before, string? childLabel)
    {
        if (string.IsNullOrWhiteSpace(childLabel)) return false;

        for (var attempt = 0; attempt < 10; attempt++)
        {
            var now = await pageObject.GetQuestionLabelsAsync();
            if (now.Any(l => l.Contains(childLabel, StringComparison.OrdinalIgnoreCase))
                && !before.Any(l => l.Contains(childLabel, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            await Task.Delay(500);
        }

        return false;
    }

    private static bool ReadFlag(System.Text.Json.JsonElement? probe, string property) =>
        probe is { ValueKind: System.Text.Json.JsonValueKind.Object } element
        && element.TryGetProperty(property, out var value)
        && value.ValueKind == System.Text.Json.JsonValueKind.True;
}
