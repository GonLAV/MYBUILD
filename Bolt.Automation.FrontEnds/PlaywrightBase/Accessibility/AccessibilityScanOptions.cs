namespace Bolt.Automation.FrontEnds.PlaywrightBase.Accessibility;

/// <summary>Which axe rules a scan runs and how.</summary>
public sealed class AccessibilityScanOptions
{
    /// <summary>WCAG 2.1 AA and everything it subsumes.</summary>
    public static readonly IReadOnlyList<string> Wcag21AaTags = ["wcag2a", "wcag2aa", "wcag21a", "wcag21aa"];

    public IReadOnlyList<string> WcagTags { get; init; } = Wcag21AaTags;

    /// <summary>Rules to skip, by axe rule id.</summary>
    public IReadOnlyCollection<string> DisabledRules { get; init; } = [];

    // axe aggregates cross-frame results by opening a blank page, which desynchronises
    // BrowserManager's tab tracking. Off unless a screen genuinely needs iframe coverage.
    public bool IncludeIframes { get; init; }

    public static AccessibilityScanOptions Default => new();
}
