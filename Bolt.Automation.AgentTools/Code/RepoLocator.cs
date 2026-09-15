namespace Bolt.Automation.AgentTools.Code;

/// <summary>
/// Resolves the nexus repo root. Prefers <c>git rev-parse --show-toplevel</c>
/// (correct for git operations and worktrees); falls back to walking up for
/// <c>Bolt.Automation.sln</c>.
/// </summary>
internal static class RepoLocator
{
    private const string SolutionFileName = "Bolt.Automation.sln";

    public static string? Resolve(string? startDir = null)
    {
        var start = startDir ?? Directory.GetCurrentDirectory();

        var git = ProcessRunner.Run("git", ["-C", start, "rev-parse", "--show-toplevel"], start, 10_000);
        if (git.Started && git.ExitCode == 0)
        {
            var top = git.StdOut.Trim();
            if (top.Length > 0 && Directory.Exists(top)) return Path.GetFullPath(top);
        }

        foreach (var seed in new[] { start, AppContext.BaseDirectory })
        {
            var dir = new DirectoryInfo(seed);
            while (dir != null)
            {
                if (File.Exists(Path.Combine(dir.FullName, SolutionFileName)))
                    return dir.FullName;
                dir = dir.Parent;
            }
        }
        return null;
    }
}
