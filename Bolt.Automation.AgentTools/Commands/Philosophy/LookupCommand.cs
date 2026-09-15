using Bolt.Automation.AgentTools.KnowledgeBase;
using CommandLine;

namespace Bolt.Automation.AgentTools.Commands.Philosophy;

[Verb("lookup", HelpText = "Map a review area to the philosophy docs that govern it.")]
internal sealed class LookupOptions
{
    [Option("area", Required = true, HelpText = "Area name, e.g. page-objects | tests | logging | field-registry | automation | design.")]
    public string Area { get; set; } = "";
}

internal static class LookupCommand
{
    public static Task<int> ExecuteAsync(LookupOptions options)
    {
        var area = options.Area?.Trim() ?? string.Empty;
        if (area.Length == 0)
            return CommandBase.EmitErrorAsync("input_error", "Pass --area <name>.", exitCode: 3);

        var docs = PhilosophyAreas.DocsFor(area);
        if (docs == null)
        {
            return CommandBase.EmitJsonAsync(new
            {
                status = "not_found",
                area,
                available_areas = PhilosophyAreas.Areas,
                hint = "Unknown area. Use one of available_areas, or `kb search \"<term>\"`.",
            }, exitCode: 2);
        }

        // Enrich each doc with its frontmatter summary when the KB root resolves.
        string? kbRoot = null;
        try { kbRoot = KnowledgeBaseLocator.Resolve(); } catch { /* still return the relative paths */ }

        var parser = new FrontmatterParser();
        var entries = docs.Select(rel =>
        {
            string? summary = null;
            var exists = false;
            if (kbRoot != null)
            {
                var abs = Path.Combine(kbRoot, rel.Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(abs))
                {
                    exists = true;
                    try
                    {
                        var (fm, _) = parser.Parse(File.ReadAllText(abs));
                        if (fm != null && fm.TryGetValue("summary", out var s)) summary = s;
                    }
                    catch { /* summary stays null */ }
                }
            }
            return new { path = rel, exists, summary };
        }).ToList();

        return CommandBase.EmitJsonAsync(new
        {
            area,
            docs = entries,
        }, exitCode: 0);
    }
}
