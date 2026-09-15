using System.Text.Json;
using Bolt.Automation.Common.Logging.Core;

namespace Bolt.Automation.FrontEnds.PlaywrightBase.Accessibility;

/// <summary>A violation that is known, triaged, and tracked against a bug.</summary>
/// <param name="State">The exact state, or "*" to mute the rule across every state of the screen.</param>
public sealed record KnownAccessibilityIssue(
    string Screen,
    string State,
    string RuleId,
    string BugId,
    string? Note = null);

/// <summary>File shape for the known-issues baseline.</summary>
public sealed class AccessibilityBaselineFile
{
    public List<KnownAccessibilityIssue> KnownIssues { get; set; } = [];
}

/// <summary>Known, bug-tracked violations that should not fail the gate.</summary>
public sealed class AccessibilityBaselineStore
{
    private static readonly JsonSerializerOptions ReadOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly IReadOnlyList<KnownAccessibilityIssue> _known;

    private AccessibilityBaselineStore(IReadOnlyList<KnownAccessibilityIssue> known) => _known = known;

    public static AccessibilityBaselineStore Empty { get; } = new([]);

    public IReadOnlyList<KnownAccessibilityIssue> Entries => _known;

    /// <summary>Loads the baseline, or returns <see cref="Empty"/> when the file is absent.</summary>
    /// <exception cref="InvalidOperationException">An entry is missing its bug id, or the file is malformed.</exception>
    public static AccessibilityBaselineStore Load(string path, IAutomationLogger? logger = null)
    {
        if (!File.Exists(path))
        {
            logger?.Info($"No accessibility baseline at '{path}' — every violation will be treated as new.");
            return Empty;
        }

        AccessibilityBaselineFile? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<AccessibilityBaselineFile>(File.ReadAllText(path), ReadOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"The accessibility baseline at '{path}' is not valid JSON: {ex.Message}", ex);
        }

        var entries = parsed?.KnownIssues ?? [];

        // Muting without a bug id is how a suite quietly stops meaning anything.
        var untracked = entries.Where(e => string.IsNullOrWhiteSpace(e.BugId)).ToList();
        if (untracked.Count > 0)
        {
            throw new InvalidOperationException(
                $"{untracked.Count} accessibility baseline entr(ies) in '{path}' have no bugId: " +
                string.Join(", ", untracked.Select(e => $"{e.Screen} [{e.State}] {e.RuleId}")) +
                ". Every muted violation must reference the ADO bug tracking its fix.");
        }

        logger?.Info($"Loaded {entries.Count} known accessibility issue(s) from '{path}'.");
        return new AccessibilityBaselineStore(entries);
    }

    public bool IsKnown(string screen, string state, string ruleId) =>
        _known.Any(e =>
            string.Equals(e.Screen, screen, StringComparison.OrdinalIgnoreCase)
            && string.Equals(e.RuleId, ruleId, StringComparison.OrdinalIgnoreCase)
            && (e.State == "*" || string.Equals(e.State, state, StringComparison.OrdinalIgnoreCase)));
}
