namespace Bolt.Automation.FrontEnds.PlaywrightBase.Accessibility;

/// <summary>The violations one axe run found on one screen state.</summary>
public sealed record AccessibilityScanResult
{
    /// <summary>Screen the scan ran against, e.g. "Details Page".</summary>
    public required string Screen { get; init; }

    /// <summary>The state within that screen, e.g. "default" or "UtilitiesUpdated=true".</summary>
    public required string State { get; init; }

    public required string Url { get; init; }

    public required DateTimeOffset ScannedAt { get; init; }

    public required IReadOnlyList<AccessibilityViolation> Violations { get; init; }

    /// <summary>Screen and state as one human-readable label.</summary>
    public string Label => $"{Screen} [{State}]";

    public IEnumerable<AccessibilityViolation> AtOrAbove(AccessibilityImpact minimum) =>
        Violations.Where(v => v.Impact >= minimum);
}
