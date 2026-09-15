using Bolt.Automation.AgentTools.Code;
using CommandLine;

namespace Bolt.Automation.AgentTools.Commands.Code;

[Verb("diff-impact", HelpText = "For a git ref, return touched files, changed symbols, consumers, and philosophy areas affected. Heuristic (grep-based) in v1.")]
internal sealed class DiffImpactOptions
{
    [Option("ref", Required = true, HelpText = "Git ref to diff from, e.g. 'origin/develop' or 'HEAD~5'.")]
    public string Ref { get; set; } = string.Empty;
}

internal static class DiffImpactCommand
{
    public static Task<int> ExecuteAsync(DiffImpactOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Ref))
            return CommandBase.EmitErrorAsync("input_error", "Pass --ref <git-ref>.", exitCode: 3);

        var repoRoot = RepoLocator.Resolve();
        if (repoRoot == null)
            return CommandBase.EmitErrorAsync("repo_not_found", "Could not locate the nexus repo root.", exitCode: 2);

        var result = new DiffImpactAnalyzer(repoRoot).Analyze(options.Ref, out var error);
        if (result == null)
            return CommandBase.EmitErrorAsync("git_error", error ?? "diff failed.", exitCode: 2,
                detail: new { @ref = options.Ref });

        return CommandBase.EmitJsonAsync(new
        {
            @ref = result.Ref,
            changed_file_count = result.ChangedFileCount,
            changes = result.Changes,
        }, exitCode: 0);
    }
}
