using System.Text;
using System.Text.Json;

namespace Bolt.Automation.FrontEnds.PlaywrightBase.Accessibility;

/// <summary>One violation paired with the state it was found on.</summary>
public sealed record AccessibilityFinding(string Screen, string State, AccessibilityViolation Violation);

/// <summary>Accumulates scan results across a flow so the test can assert once at the end.</summary>
public sealed class AccessibilityFindings
{
    private static readonly JsonSerializerOptions ReportOptions = new() { WriteIndented = true };

    private readonly List<AccessibilityScanResult> _results = [];

    public IReadOnlyList<AccessibilityScanResult> Results => _results;

    public void Add(AccessibilityScanResult result) => _results.Add(result);

    public int StatesScanned => _results.Count;

    public IEnumerable<AccessibilityFinding> All =>
        _results.SelectMany(r => r.Violations.Select(v => new AccessibilityFinding(r.Screen, r.State, v)));

    /// <summary>Findings at or above <paramref name="minimum"/> that the baseline does not already track.</summary>
    public IReadOnlyList<AccessibilityFinding> Blocking(
        AccessibilityImpact minimum,
        AccessibilityBaselineStore? baseline = null)
    {
        var store = baseline ?? AccessibilityBaselineStore.Empty;
        return [.. All
            .Where(f => f.Violation.Impact >= minimum)
            .Where(f => !store.IsKnown(f.Screen, f.State, f.Violation.RuleId))];
    }

    /// <summary>The full report, for upload as a test artifact.</summary>
    public string ToJson() => JsonSerializer.Serialize(new
    {
        statesScanned = StatesScanned,
        totalViolations = All.Count(),
        byImpact = All.GroupBy(f => f.Violation.Impact)
                      .OrderByDescending(g => g.Key)
                      .ToDictionary(g => g.Key.ToString(), g => g.Count()),
        results = _results
    }, ReportOptions);

    /// <summary>Groups findings by rule for an assertion message that can be acted on directly.</summary>
    public static string Describe(IReadOnlyCollection<AccessibilityFinding> findings)
    {
        if (findings.Count == 0) return "No accessibility violations.";

        var report = new StringBuilder()
            .AppendLine($"{findings.Count} accessibility violation(s):");

        foreach (var group in findings
                     .GroupBy(f => new { f.Violation.RuleId, f.Violation.Impact })
                     .OrderByDescending(g => g.Key.Impact)
                     .ThenBy(g => g.Key.RuleId))
        {
            var first = group.First().Violation;
            var criteria = string.Join(", ", first.WcagCriteria);
            var elementCount = group.Sum(f => f.Violation.Nodes.Count);

            report.AppendLine(
                $"  [{group.Key.Impact}] {group.Key.RuleId} — {first.Help} " +
                $"({elementCount} element(s){(criteria.Length > 0 ? $"; {criteria}" : "")})")
                .AppendLine($"      states: {string.Join(", ", group.Select(f => $"{f.Screen} [{f.State}]").Distinct())}")
                .AppendLine($"      {first.HelpUrl}");
        }

        return report.ToString();
    }
}
