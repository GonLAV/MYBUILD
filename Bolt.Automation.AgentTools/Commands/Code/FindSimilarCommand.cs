using Bolt.Automation.AgentTools.Code;
using CommandLine;

namespace Bolt.Automation.AgentTools.Commands.Code;

[Verb("find-similar", HelpText = "Surface existing usages of a pattern (class, attribute, method) to guide new code.")]
internal sealed class FindSimilarOptions
{
    [Option("pattern", Required = true, HelpText = "Symbol or text to search for, e.g. 'InterviewBase' or 'IsXxxPopUpExists'.")]
    public string Pattern { get; set; } = string.Empty;

    [Option("limit", Default = 5, HelpText = "Maximum files to return. Default: 5.")]
    public int Limit { get; set; } = 5;
}

internal static class FindSimilarCommand
{
    public static Task<int> ExecuteAsync(FindSimilarOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Pattern))
            return CommandBase.EmitErrorAsync("input_error", "Pass --pattern <symbol>.", exitCode: 3);

        var repoRoot = RepoLocator.Resolve();
        if (repoRoot == null)
            return CommandBase.EmitErrorAsync("repo_not_found", "Could not locate the nexus repo root.", exitCode: 2);

        var hits = new GrepRunner(repoRoot).Search(options.Pattern, maxCount: 1000);

        // Group by file; rank by hit count; cap to --limit files.
        var byFile = hits
            .GroupBy(h => h.File, StringComparer.Ordinal)
            .Select(g => new
            {
                file = g.Key,
                count = g.Count(),
                sample_lines = g.OrderBy(h => h.Line).Take(3)
                    .Select(h => new { line = h.Line, text = h.Text }).ToList(),
            })
            .OrderByDescending(x => x.count)
            .ThenBy(x => x.file, StringComparer.Ordinal)
            .Take(Math.Max(1, options.Limit))
            .ToList();

        if (byFile.Count == 0)
        {
            return CommandBase.EmitJsonAsync(new
            {
                status = "no_hits",
                pattern = options.Pattern,
            }, exitCode: 2);
        }

        return CommandBase.EmitJsonAsync(new
        {
            pattern = options.Pattern,
            total_hits = hits.Count,
            file_count = byFile.Count,
            files = byFile,
        }, exitCode: 0);
    }
}
