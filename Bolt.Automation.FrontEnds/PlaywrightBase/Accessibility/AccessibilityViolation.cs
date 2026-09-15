namespace Bolt.Automation.FrontEnds.PlaywrightBase.Accessibility;

/// <summary>
/// One element that failed a rule.
/// </summary>
/// <param name="Target">CSS selector axe resolved for the element.</param>
/// <param name="Html">The element's outer HTML, truncated for reporting.</param>
public sealed record AccessibilityViolationNode(string Target, string Html);

/// <summary>One axe rule failure on one screen state, with every element that broke it.</summary>
// Mirrors axe's shape so the axe dependency stays inside AccessibilityScanner — see kb framework:accessibility.
public sealed record AccessibilityViolation
{
    /// <summary>axe rule id, e.g. "color-contrast" or "aria-hidden-focus". Keys the baseline file.</summary>
    public required string RuleId { get; init; }

    public required AccessibilityImpact Impact { get; init; }

    /// <summary>What the rule checks.</summary>
    public required string Description { get; init; }

    /// <summary>What to do about it.</summary>
    public required string Help { get; init; }

    /// <summary>Deque rule documentation.</summary>
    public required string HelpUrl { get; init; }

    /// <summary>
    /// Every axe tag on the rule. Includes the WCAG level tags this scan filtered on
    /// ("wcag2aa", "wcag21aa") and the specific success criterion ("wcag143").
    /// </summary>
    public required IReadOnlyList<string> Tags { get; init; }

    public required IReadOnlyList<AccessibilityViolationNode> Nodes { get; init; }

    /// <summary>The WCAG success-criterion tags only, for reporting against the standard.</summary>
    public IEnumerable<string> WcagCriteria =>
        Tags.Where(t => t.StartsWith("wcag", StringComparison.OrdinalIgnoreCase)
                        && t.Length > 4
                        && char.IsDigit(t[4]));
}
