namespace Bolt.Automation.AgentTools.Commands.Philosophy;

/// <summary>
/// Maps a review "area" to the philosophy KB leaves that govern it. The inverse
/// direction of <see cref="Code.PhilosophyAreaMap"/> (which maps file paths to
/// areas): here the agent names an area it's reasoning about and gets the
/// philosophy docs to read. Aliases collapse onto the same doc set.
/// </summary>
internal static class PhilosophyAreas
{
    private static readonly Dictionary<string, string[]> Map = new(StringComparer.OrdinalIgnoreCase)
    {
        ["page-objects"]     = new[] { "philosophy/fluent-page-objects.md", "philosophy/tests-stay-clean.md" },
        ["pages"]            = new[] { "philosophy/fluent-page-objects.md", "philosophy/tests-stay-clean.md" },
        ["field-registry"]   = new[] { "philosophy/sparse-dictionaries.md" },
        ["fields"]           = new[] { "philosophy/sparse-dictionaries.md" },
        ["ui-fields"]        = new[] { "philosophy/sparse-dictionaries.md" },
        ["logging"]          = new[] { "philosophy/logging-where-work-happens.md" },
        ["tests"]            = new[] { "philosophy/tests-stay-clean.md" },
        ["test-design"]      = new[] { "philosophy/tests-stay-clean.md", "philosophy/when-to-automate.md" },
        ["automation"]       = new[] { "philosophy/when-to-automate.md" },
        ["when-to-automate"] = new[] { "philosophy/when-to-automate.md" },
        ["design"]           = new[] { "philosophy/design-decisions.md" },
        ["decisions"]        = new[] { "philosophy/design-decisions.md" },
        ["adr"]              = new[] { "philosophy/design-decisions.md" },
    };

    public static IReadOnlyList<string> Areas =>
        Map.Keys.OrderBy(k => k, StringComparer.Ordinal).ToList();

    public static string[]? DocsFor(string area) =>
        Map.TryGetValue(area.Trim(), out var docs) ? docs : null;
}
