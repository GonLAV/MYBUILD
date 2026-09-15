using Bolt.Automation.AgentTools.Code;
using CommandLine;

namespace Bolt.Automation.AgentTools.Commands.Code;

[Verb("wip-stop", HelpText = "Strategic pause: stage all changes, commit with WIP-STOP: prefix, write RESUME-NOTE.md, return commit SHA.")]
internal sealed class WipStopOptions
{
    [Option("note", Required = true, HelpText = "What's incomplete and what the resumer should do next.")]
    public string Note { get; set; } = string.Empty;
}

internal static class WipStopCommand
{
    public static Task<int> ExecuteAsync(WipStopOptions options)
    {
        var note = options.Note?.Trim() ?? string.Empty;
        if (note.Length == 0)
            return CommandBase.EmitErrorAsync("input_error", "Pass --note <text>.", exitCode: 3);

        var repoRoot = RepoLocator.Resolve();
        if (repoRoot == null)
            return CommandBase.EmitErrorAsync("repo_not_found", "Could not locate the nexus repo root.", exitCode: 2);

        // Stamp the resume note. Date.Now is fine here (one-shot CLI, not a workflow).
        var timestamp = DateTime.UtcNow.ToString("o");
        var result = new WipStop(repoRoot).Run(note, timestamp);

        if (!result.Ok)
            return CommandBase.EmitErrorAsync("wip_stop_failed", result.Message ?? "wip-stop failed.", exitCode: 2,
                detail: new { branch = result.Branch, files_staged = result.FilesStaged });

        return CommandBase.EmitJsonAsync(new
        {
            status = "committed",
            commit_sha = result.CommitSha,
            branch = result.Branch,
            files_staged = result.FilesStaged,
            resume_note = result.ResumeNotePath,
        }, exitCode: 0);
    }
}
