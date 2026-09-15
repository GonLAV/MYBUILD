using System.Text.RegularExpressions;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Microsoft.Playwright;

namespace Bolt.Automation.FrontEnds.Projects.HQXConsumer.Services.Tooltips;

/// <summary>
/// Service for validating tooltip content on pages
/// </summary>
public class TooltipValidationService
{
    private readonly IPage _page;
    private readonly IPageHelper _pageHelper;
    private readonly IAutomationLogger? _logger;
    private const int TooltipPopupTimeoutMs = 5000;

    public TooltipValidationService(IPage page, IPageHelper pageHelper, IAutomationLogger? logger = null)
    {
        _page = page ?? throw new ArgumentNullException(nameof(page));
        _pageHelper = pageHelper ?? throw new ArgumentNullException(nameof(pageHelper));
        _logger = logger;
    }

    public async Task<IReadOnlyCollection<TooltipValidationIssue>> ValidateTooltipsAsync(
        IDictionary<string, string> expectedTitleToTooltip)
    {
        _logger?.Debug($"Validating {expectedTitleToTooltip.Count} tooltip(s)");
        var issues = new List<TooltipValidationIssue>();

        foreach (var (title, expectedTooltip) in expectedTitleToTooltip)
        {
            var expectedNorm = StripLeadingTitle(NormalizeTooltip(expectedTooltip), title);
            var btn = await ResolveTooltipButtonAsync(title);

            if (btn == null)
            {
                _logger?.Debug($"Tooltip button not found for '{title}'");
                issues.Add(new TooltipValidationIssue(title, "LearnMoreMissing", expectedNorm, string.Empty));
                continue;
            }

            string? raw = null;
            try
            {
                await btn.ClickAsync();
                var popup = _page.Locator("popup-wrapper .tooltip-content, .popup-panel .tooltip-content, .cdk-overlay-container .tooltip-content");
                await _pageHelper.WaitForElementAsync(popup, TooltipPopupTimeoutMs, true);
                raw = await popup.First.InnerTextAsync();
            }
            catch (Exception ex)
            {
                _logger?.Debug($"Failed to get tooltip content for '{title}': {ex.Message}");
            }

            await CloseTooltipAsync();

            if (string.IsNullOrWhiteSpace(raw))
            {
                issues.Add(new TooltipValidationIssue(title, "EmptyTooltip", expectedNorm, string.Empty));
                continue;
            }

            var norm = NormalizeTooltip(raw);
            var actualStripped = StripLeadingTitle(norm, title);
            var match = actualStripped.Contains(expectedNorm, StringComparison.OrdinalIgnoreCase) ||
                        expectedNorm.Contains(actualStripped, StringComparison.OrdinalIgnoreCase);

            if (!match)
            {
                issues.Add(new TooltipValidationIssue(title, "ContentMismatch", expectedNorm, actualStripped));
            }
            else
            {
                _logger?.Info($"Tooltip OK - Title: '{title}' Expected: '{expectedNorm}' Actual: '{actualStripped}'");
            }
        }

        _logger?.Debug($"Tooltip validation complete: {issues.Count} issue(s) found");
        return issues;
    }

    private async Task<ILocator?> ResolveTooltipButtonAsync(string title)
    {
        return await TryResolveViaInfoTitleAsync(title)
            ?? await TryResolveViaQuestionLabelAsync(title)
            ?? await TryResolveViaCheckboxLabelAsync(title);
    }

    /// <summary>Matches a label whose text is <paramref name="title"/>, tolerating the mandatory "*"
    /// marker and any trailing ".question-description" hint text.</summary>
    private static Regex TitleRegex(string title) =>
        new(@"^\s*" + Regex.Escape(title) + @"\s*\*?(?:\s[\s\S]*)?$", RegexOptions.IgnoreCase);

    /// <summary>
    /// Strategy 1: title is a coverage info-title span; button is inside the same info-title container.
    /// </summary>
    private async Task<ILocator?> TryResolveViaInfoTitleAsync(string title)
    {
        var span = _page.Locator("p.info-title span").Filter(new LocatorFilterOptions { HasTextRegex = TitleRegex(title) });
        if (await span.CountAsync() == 0) return null;

        var container = span.First.Locator(
            "xpath=ancestor::*[self::app-label or contains(@class,'info-title-container') or contains(@class,'info-container')][1]");
        var btn = container.Locator("button.link-label");
        if (await btn.CountAsync() > 0) return btn.First;

        var following = span.First.Locator("xpath=following::button[contains(@class,'link-label')][1]");
        if (await following.CountAsync() > 0) return following.First;

        return null;
    }

    /// <summary>
    /// Strategy 2: title is a question-label; button follows the label in the DOM.
    /// </summary>
    private async Task<ILocator?> TryResolveViaQuestionLabelAsync(string title)
    {
        var label = _page.Locator("label.question-label").Filter(new LocatorFilterOptions { HasTextRegex = TitleRegex(title) });
        if (await label.CountAsync() == 0) return null;

        var following = label.First.Locator("xpath=following::button[contains(@class,'link-label')][1]");
        if (await following.CountAsync() > 0) return following.First;

        return null;
    }

    /// <summary>
    /// Strategy 3: title matches a checkbox label inside app-parent-child-container;
    /// button is the popup-link inside the same wrapper (e.g. ExoticAnimals).
    /// </summary>
    private async Task<ILocator?> TryResolveViaCheckboxLabelAsync(string title)
    {
        var wrapper = _page.Locator("app-parent-child-container.question-wrapper")
            .Filter(new LocatorFilterOptions
            {
                Has = _page.Locator(".custom-checkbox-label").Filter(new LocatorFilterOptions { HasTextRegex = TitleRegex(title) })
            });
        if (await wrapper.CountAsync() == 0) return null;

        var popupBtn = wrapper.First.Locator("popup-link button.link-label");
        if (await popupBtn.CountAsync() > 0) return popupBtn.First;

        return null;
    }

    private async Task CloseTooltipAsync()
    {
        try
        {
            var closeBtn = _page.Locator("button.close-btn");
            if (await closeBtn.CountAsync() > 0)
                await closeBtn.First.ClickAsync();
            else
                await _page.Keyboard.PressAsync("Escape");
        }
        catch { }
        await Task.Delay(50);
    }

    private static string NormalizeTooltip(string text)
    {
        var t = (text ?? string.Empty).Trim();
        t = t.Replace('\u2019', '\'').Replace('\u2018', '\'').Replace('\u201C', '"').Replace('\u201D', '"');
        t = Regex.Replace(t, "[\r\n]+", " ");
        t = Regex.Replace(t, "\\s+", " ");
        t = Regex.Replace(t, @"Questions\?\s*Call.*$", string.Empty, RegexOptions.IgnoreCase);
        return t.Trim();
    }

    private static string StripLeadingTitle(string content, string title)
    {
        if (string.IsNullOrWhiteSpace(content) || string.IsNullOrWhiteSpace(title)) return content;
        var norm = content.Trim();
        if (norm.StartsWith(title, StringComparison.OrdinalIgnoreCase))
        {
            norm = norm[title.Length..];
            norm = norm.TrimStart(' ', '-', ':');
            return norm;
        }
        return norm;
    }
}
