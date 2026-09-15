namespace Bolt.Automation.FrontEnds.PlaywrightBase.Accessibility;

/// <summary>
/// Severity axe assigns to a violation. Ordered so severities can be compared —
/// a gate of <see cref="Serious"/> also catches <see cref="Critical"/>.
/// </summary>
public enum AccessibilityImpact
{
    /// <summary>axe returned no impact for the rule. Treated as the lowest severity.</summary>
    Unknown = 0,
    Minor = 1,
    Moderate = 2,
    Serious = 3,
    Critical = 4
}

public static class AccessibilityImpactParser
{
    /// <summary>
    /// Maps axe's impact string ("minor" / "moderate" / "serious" / "critical") onto the enum.
    /// Anything unrecognised or absent becomes <see cref="AccessibilityImpact.Unknown"/> rather
    /// than throwing — a new axe severity must never break a scan.
    /// </summary>
    public static AccessibilityImpact Parse(string? impact) => impact?.Trim().ToLowerInvariant() switch
    {
        "critical" => AccessibilityImpact.Critical,
        "serious" => AccessibilityImpact.Serious,
        "moderate" => AccessibilityImpact.Moderate,
        "minor" => AccessibilityImpact.Minor,
        _ => AccessibilityImpact.Unknown
    };
}
