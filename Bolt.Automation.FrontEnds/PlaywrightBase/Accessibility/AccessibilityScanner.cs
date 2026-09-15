using Bolt.Automation.Common.Logging.Core;
using Deque.AxeCore.Commons;
using Deque.AxeCore.Playwright;
using Microsoft.Playwright;

namespace Bolt.Automation.FrontEnds.PlaywrightBase.Accessibility;

/// <summary>Runs axe-core against a page or region and maps the result onto framework types.</summary>
public sealed class AccessibilityScanner(IAutomationLogger? logger = null) : IAccessibilityScanner
{
    private const int MaxNodeHtmlLength = 300;

    public async Task<AccessibilityScanResult> ScanAsync(
        IPage page,
        string screen,
        string state,
        AccessibilityScanOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(page);
        var opts = options ?? AccessibilityScanOptions.Default;

        logger?.Debug($"Running an accessibility scan on '{screen}' [{state}] against tags: {string.Join(", ", opts.WcagTags)}");
        var result = await page.RunAxe(BuildAxeOptions(opts));

        return Map(result, screen, state);
    }

    public async Task<AccessibilityScanResult> ScanAsync(
        ILocator locator,
        string screen,
        string state,
        AccessibilityScanOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(locator);
        var opts = options ?? AccessibilityScanOptions.Default;

        logger?.Debug($"Running a scoped accessibility scan on '{screen}' [{state}].");
        var result = await locator.RunAxe(BuildAxeOptions(opts));

        return Map(result, screen, state);
    }

    private static AxeRunOptions BuildAxeOptions(AccessibilityScanOptions options)
    {
        var axeOptions = new AxeRunOptions
        {
            RunOnly = new RunOnlyOptions { Type = "tag", Values = [.. options.WcagTags] },
            Iframes = options.IncludeIframes
        };

        if (options.DisabledRules.Count > 0)
        {
            axeOptions.Rules = options.DisabledRules
                .ToDictionary(rule => rule, _ => new RuleOptions { Enabled = false });
        }

        return axeOptions;
    }

    private AccessibilityScanResult Map(AxeResult result, string screen, string state)
    {
        var violations = (result.Violations ?? []).Select(MapViolation).ToList();

        logger?.Info(violations.Count == 0
            ? $"Accessibility scan of '{screen}' [{state}] found no violations."
            : $"Accessibility scan of '{screen}' [{state}] found {violations.Count} violation(s): " +
              string.Join(", ", violations.Select(v => $"{v.RuleId} ({v.Impact})")));

        return new AccessibilityScanResult
        {
            Screen = screen,
            State = state,
            Url = result.Url ?? string.Empty,
            ScannedAt = result.Timestamp ?? DateTimeOffset.UtcNow,
            Violations = violations
        };
    }

    private static AccessibilityViolation MapViolation(AxeResultItem item) => new()
    {
        RuleId = item.Id ?? "(unknown-rule)",
        Impact = AccessibilityImpactParser.Parse(item.Impact),
        Description = item.Description ?? string.Empty,
        Help = item.Help ?? string.Empty,
        HelpUrl = item.HelpUrl ?? string.Empty,
        Tags = item.Tags ?? [],
        Nodes = (item.Nodes ?? []).Select(MapNode).ToList()
    };

    private static AccessibilityViolationNode MapNode(AxeResultNode node) => new(
        node.Target?.ToString() ?? "(unknown-target)",
        Truncate(node.Html));

    private static string Truncate(string? html)
    {
        if (string.IsNullOrEmpty(html)) return string.Empty;
        return html.Length <= MaxNodeHtmlLength ? html : html[..MaxNodeHtmlLength] + "…";
    }
}
