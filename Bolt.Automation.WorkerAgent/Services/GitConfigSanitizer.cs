using System.Diagnostics;

namespace Bolt.Automation.WorkerAgent.Services;

/// <summary>
/// Strips inherited Authorization headers from git config before any worker
/// git operation runs.
///
/// **Why this exists.** The worker's <c>worker.Dockerfile</c> uses
/// <c>COPY . .</c> which bakes the build host's <c>.git/</c> directory into
/// the image. Azure DevOps Pipelines (default <c>checkout: self</c> with
/// <c>persistCredentials: true</c>) writes a session-scoped bearer into
/// <c>.git/config</c> as
/// <c>[http "https://azure.devops.boltx.us/..."] extraheader = AUTHORIZATION:
/// bearer &lt;build-token&gt;</c>. That bearer expires when the build session
/// ends but the entry survives in the image. Per <c>git-config(7)</c>, our
/// per-command <c>-c http.extraHeader=Authorization: Basic &lt;PAT&gt;</c>
/// override does NOT replace baked <c>extraheader</c> entries — git sends
/// BOTH headers, the server picks the first/expired bearer, returns 401, and
/// git falls back to a credential prompt → no TTY → fatal.
///
/// We can't switch to runtime <c>git clone</c> like the orchestrator does
/// (the worker depends on the baked NuGet/build cache), so instead we scrub
/// the inherited config at startup. After this runs, only our per-command
/// <c>-c</c> header reaches the server.
///
/// All mutations go through <c>git config --unset-all</c> — we never
/// hand-edit the config file, since git owns the format and re-implementing
/// the parser is the kind of footgun that introduces a worse bug than the
/// one it's trying to fix.
/// </summary>
public static class GitConfigSanitizer
{
    /// <summary>
    /// Counts and (sanitized) keys removed by <see cref="ScrubRepo"/>, for
    /// observability in the startup banner. Header values are deliberately
    /// not captured — only key names — so a stale Basic credential cannot
    /// leak through this object even if one was present in the config.
    /// </summary>
    public sealed record ScrubResult(
        int LocalRemoved,
        int GlobalRemoved,
        int SystemRemoved,
        IReadOnlyList<string> RemovedKeys)
    {
        public static readonly ScrubResult Empty =
            new(0, 0, 0, Array.Empty<string>());

        public int TotalRemoved => LocalRemoved + GlobalRemoved + SystemRemoved;
    }

    /// <summary>
    /// Idempotent. Runs <c>git config --get-regexp ^http\..*\.extraheader$</c>
    /// at <c>--local</c>, <c>--global</c>, and <c>--system</c> scopes, unsets
    /// every match via <c>--unset-all</c>, then sets a belt-and-suspenders
    /// empty <c>http.extraheader</c> at <c>--local</c> scope to disable any
    /// inheritance we might have missed. Tolerant of a missing
    /// <c>.git</c> directory (returns <see cref="ScrubResult.Empty"/>) so
    /// dev/test runs without git history don't blow up.
    /// </summary>
    public static ScrubResult ScrubRepo(string repoRoot, ILogger logger)
    {
        var gitDir = Path.Combine(repoRoot, ".git");
        if (!Directory.Exists(gitDir))
        {
            logger.LogInformation(
                "Git config scrub skipped — no .git directory at {RepoRoot}", repoRoot);
            return ScrubResult.Empty;
        }

        var removed = new List<string>();
        var local  = ScrubScope(repoRoot, "--local",  removed, logger);
        var global = ScrubScope(repoRoot, "--global", removed, logger);
        var system = ScrubScope(repoRoot, "--system", removed, logger);

        // Empty-string override on local — even if a stale extraheader survived
        // some fixed-point bug above, an explicit empty value at the most
        // specific scope wins per git-config precedence rules.
        TryRunGit(repoRoot, ["config", "--local", "http.extraheader", ""], logger);

        var result = new ScrubResult(local, global, system, removed);
        if (result.TotalRemoved == 0)
        {
            logger.LogInformation(
                "Git config scrub: no inherited extraheader entries found");
        }
        else
        {
            logger.LogInformation(
                "Git config scrub: removed {Total} extraheader entries "
                + "(local: {Local}, global: {Global}, system: {System}); keys=[{Keys}]",
                result.TotalRemoved, local, global, system, string.Join(", ", removed));
        }
        return result;
    }

    private static int ScrubScope(
        string repoRoot, string scope, List<string> removedKeys, ILogger logger)
    {
        // List matching keys. `--get-regexp` exits non-zero (1) when no match
        // is found, which is the common, healthy path — not an error.
        var (exit, stdout, _) =
            RunGit(repoRoot, ["config", scope, "--get-regexp", "^http\\..*\\.extraheader$"], logger);

        if (exit != 0 || string.IsNullOrWhiteSpace(stdout))
        {
            return 0;
        }

        var count = 0;
        foreach (var line in stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            // Lines look like: `http.https://host/.extraheader AUTHORIZATION: bearer xyz`
            // We split on the first whitespace so we never read the value into
            // memory or logs — only the key.
            var key = line.Split(' ', 2)[0].Trim();
            if (string.IsNullOrEmpty(key)) continue;

            var (unsetExit, _, unsetStderr) =
                RunGit(repoRoot, ["config", scope, "--unset-all", key], logger);
            if (unsetExit == 0)
            {
                removedKeys.Add(key);
                count++;
            }
            else
            {
                logger.LogWarning(
                    "Git config scrub: could not unset {Key} at {Scope}: {Stderr}",
                    key, scope, unsetStderr.Trim());
            }
        }
        return count;
    }

    /// <summary>
    /// Runs <c>git</c> synchronously with a hard 10s ceiling. We're at process
    /// startup before the DI container is fully built, so an
    /// <c>IHostedService</c> lifecycle / cancellation token isn't available
    /// yet — we just want this to finish fast or fail loudly.
    /// </summary>
    private static (int ExitCode, string Stdout, string Stderr) RunGit(
        string workingDirectory, string[] arguments, ILogger logger)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "git",
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var a in arguments) psi.ArgumentList.Add(a);

        using var process = new Process { StartInfo = psi };
        process.Start();

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();

        if (!process.WaitForExit(10_000))
        {
            try { process.Kill(entireProcessTree: true); } catch { /* best effort */ }
            logger.LogWarning(
                "Git config scrub: `git {Args}` timed out after 10s — skipping",
                string.Join(' ', arguments));
            return (-1, "", "timeout");
        }

        return (process.ExitCode, stdoutTask.Result, stderrTask.Result);
    }

    /// <summary>
    /// Fire-and-log variant of <see cref="RunGit"/> for the belt-and-suspenders
    /// override — we don't care about its exit code, only that we tried.
    /// </summary>
    private static void TryRunGit(
        string workingDirectory, string[] arguments, ILogger logger)
    {
        var (exit, _, stderr) = RunGit(workingDirectory, arguments, logger);
        if (exit != 0)
        {
            logger.LogDebug(
                "Git config scrub: belt-and-suspenders `git {Args}` returned {Exit}: {Stderr}",
                string.Join(' ', arguments), exit, stderr.Trim());
        }
    }
}
