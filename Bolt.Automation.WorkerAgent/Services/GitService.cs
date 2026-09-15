using System.Diagnostics;
using Microsoft.Extensions.Options;

namespace Bolt.Automation.WorkerAgent.Services;

/// <summary>
/// Carries the work-item / branch / commit context of an in-flight git
/// operation so any error log emitted inside <see cref="GitService"/> can
/// say which job triggered the failure. Optional everywhere — local startup
/// ops (e.g. resolving the baked-in commit hash) pass <see cref="Empty"/>.
/// </summary>
public readonly record struct GitOpContext(string? WorkItemId, string? Branch, string? Commit)
{
    public static readonly GitOpContext Empty = new(null, null, null);
}

/// <summary>Base type for every git failure surfaced to the work loop.</summary>
public class GitOperationException : InvalidOperationException
{
    public GitOpContext Context { get; }

    public GitOperationException(string message, GitOpContext context) : base(message)
    {
        Context = context;
    }
}

/// <summary>Thrown when a network-touching git op (e.g. <c>fetch</c>) failed.</summary>
public sealed class GitFetchException : GitOperationException
{
    public GitFetchException(string message, GitOpContext context) : base(message, context) { }
}

/// <summary>
/// Specialisation of <see cref="GitFetchException"/> raised when the failure
/// signature looks like an authentication problem. The work loop treats this
/// differently — there's no point retrying every 2s while the PAT or the
/// baked config is broken.
/// </summary>
public sealed class GitAuthException : GitOperationException
{
    public GitAuthException(string message, GitOpContext context) : base(message, context) { }
}

/// <summary>Thrown when a local checkout operation failed.</summary>
public sealed class GitCheckoutException : GitOperationException
{
    public GitCheckoutException(string message, GitOpContext context) : base(message, context) { }
}

public sealed class GitService
{
    private readonly string _repoRoot;
    private readonly int _timeoutSeconds;
    private readonly string _pat;
    private readonly ILogger<GitService> _logger;
    private readonly GitConfigSanitizer.ScrubResult _scrubResult;
    private bool _authDiagnosticsEmitted;

    public GitService(IOptions<WorkerOptions> options, ILogger<GitService> logger)
    {
        _repoRoot = options.Value.RepoRootPath;
        _timeoutSeconds = options.Value.GitTimeoutSeconds;
        _pat = options.Value.AzureDevOpsPat ?? "";
        _logger = logger;

        // Constructor-time scrub. Runs synchronously before any git op, before
        // the work loop starts, and exactly once per process (singleton). If
        // it throws, that's a misbuilt image (no git binary) and crashing at
        // startup is correct — better than running blind and serving 401s on
        // every fetch.
        _scrubResult = GitConfigSanitizer.ScrubRepo(_repoRoot, _logger);
    }

    /// <summary>Result of the startup scrub, exposed for the startup banner.</summary>
    public GitConfigSanitizer.ScrubResult ConfigScrubResult => _scrubResult;

    /// <summary>
    /// Returns false when no .git directory exists under the repo root —
    /// meaning the image was built without git history (e.g. local dev or stripped CI image).
    /// Self-update is skipped in that case.
    /// </summary>
    public bool IsGitRepository => Directory.Exists(Path.Combine(_repoRoot, ".git"));

    public Task FetchAsync(CancellationToken ct = default) =>
        FetchAsync(GitOpContext.Empty, ct);

    public async Task FetchAsync(GitOpContext context, CancellationToken ct = default)
    {
        _logger.LogInformation("Running git fetch");
        await RunGitAsync(context, ct, AuthArgs().Concat(new[] { "fetch", "--prune" }).ToArray());
    }

    public Task CheckoutAsync(string branchOrRef, CancellationToken ct = default) =>
        CheckoutAsync(branchOrRef, GitOpContext.Empty, ct);

    public async Task CheckoutAsync(string branchOrRef, GitOpContext context, CancellationToken ct = default)
    {
        _logger.LogInformation("Checking out origin/{Branch} (detached)", branchOrRef);
        await RunGitAsync(context, ct, "checkout", $"origin/{branchOrRef}", "--detach");
    }

    public Task CheckoutCommitAsync(string commitHash, CancellationToken ct = default) =>
        CheckoutCommitAsync(commitHash, GitOpContext.Empty, ct);

    public async Task CheckoutCommitAsync(string commitHash, GitOpContext context, CancellationToken ct = default)
    {
        _logger.LogInformation("Checking out commit {Commit} (detached)", commitHash[..Math.Min(8, commitHash.Length)]);
        await RunGitAsync(context, ct, "checkout", commitHash, "--detach");
    }

    public async Task<string> GetCurrentCommitHashAsync(CancellationToken ct = default)
    {
        var output = await RunGitAsync(GitOpContext.Empty, ct, "rev-parse", "HEAD");
        return output.Trim();
    }

    public async Task<string> GetCurrentBranchAsync(CancellationToken ct = default)
    {
        // In detached HEAD state, this returns "HEAD". That's expected —
        // we track the intended branch name separately.
        var output = await RunGitAsync(GitOpContext.Empty, ct, "rev-parse", "--abbrev-ref", "HEAD");
        return output.Trim();
    }

    public Task<string> GetRemoteHeadHashAsync(string branch, CancellationToken ct = default) =>
        GetRemoteHeadHashAsync(branch, GitOpContext.Empty, ct);

    public async Task<string> GetRemoteHeadHashAsync(string branch, GitOpContext context, CancellationToken ct = default)
    {
        await FetchAsync(context, ct);
        var output = await RunGitAsync(context, ct, "rev-parse", $"origin/{branch}");
        return output.Trim();
    }

    /// <summary>
    /// Builds the per-command auth args injected ahead of every network-touching
    /// git command (<c>fetch</c>, <c>clone</c>, anything that hits a remote).
    /// Mirrors the orchestrator's pattern in <c>packages/logger/src/services/git.ts</c>
    /// — per-command <c>-c http.extraHeader=Authorization: Basic &lt;b64(:PAT)&gt;</c>,
    /// no persistent global config, secret never written to disk.
    /// Returns an empty array when no PAT is configured; the first git auth failure
    /// triggers a one-shot operator-visible diagnostic.
    /// </summary>
    private string[] AuthArgs()
    {
        if (string.IsNullOrEmpty(_pat)) return Array.Empty<string>();
        var b64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($":{_pat}"));
        return new[] { "-c", $"http.extraHeader=Authorization: Basic {b64}" };
    }

    private async Task<string> RunGitAsync(GitOpContext context, CancellationToken ct, params string[] arguments)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "git",
            WorkingDirectory = _repoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        // ArgumentList preserves quoting for values containing spaces
        // (e.g. `-c "http.extraHeader=Authorization: Basic <b64>"`).
        foreach (var a in arguments) psi.ArgumentList.Add(a);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(_timeoutSeconds));

        using var process = new Process { StartInfo = psi };
        process.Start();

        // Read stdout and stderr concurrently to avoid deadlock when either pipe fills its buffer
        var stdoutTask = process.StandardOutput.ReadToEndAsync(cts.Token);
        var stderrTask = process.StandardError.ReadToEndAsync(cts.Token);

        try
        {
            await process.WaitForExitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            try { process.Kill(entireProcessTree: true); }
            catch { /* process may have already exited */ }

            try { process.WaitForExit(5000); }
            catch { /* best effort reap */ }

            // Drain pipe tasks so handles are released
            try { await Task.WhenAll(stdoutTask, stderrTask).WaitAsync(TimeSpan.FromSeconds(5)); }
            catch { /* best effort */ }

            // Explicitly close streams to release handles if orphaned tasks are still pending
            try { process.StandardOutput.Close(); } catch { }
            try { process.StandardError.Close(); } catch { }

            throw;
        }

        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        if (process.ExitCode != 0)
        {
            var sanitizedArgs = SanitizeArgsForLog(arguments);
            var sanitizedStderr = SanitizeArgsForLog(new[] { stderr }).Single().Trim();
            var operation = ExtractOperation(arguments);
            var contextSuffix = ContextSuffix(context);

            _logger.LogError(
                "git {Operation} failed (exit {Code}){Context}: args=`{Args}` stderr=`{Stderr}`",
                operation, process.ExitCode, contextSuffix,
                string.Join(' ', sanitizedArgs), sanitizedStderr);

            // First time we see an auth-failure stderr signature, dump the
            // (sanitized) git config so an operator can immediately see
            // whether a stale extraheader survived the startup scrub. One
            // shot per process — the same diagnostic every poll would drown
            // the log.
            var looksLikeAuthFailure =
                stderr.Contains("could not read Username", StringComparison.OrdinalIgnoreCase)
                || stderr.Contains("Authentication failed", StringComparison.OrdinalIgnoreCase);

            if (looksLikeAuthFailure)
            {
                // Distinguish the two flavours of misconfig: no PAT at all
                // (classic env-var omission) vs. PAT present but rejected
                // (the structural bug this whole change is built around).
                if (string.IsNullOrEmpty(_pat))
                {
                    _logger.LogError(
                        "git auth not configured — set AZURE_DEVOPS_PAT env var on the worker pod; "
                        + "cross-branch work-item pickup will fail until this is set.");
                }

                if (!_authDiagnosticsEmitted)
                {
                    _authDiagnosticsEmitted = true;
                    await DumpAuthFailureDiagnosticsAsync(operation, ct);
                }

                throw new GitAuthException(
                    $"git {operation} authentication failed (exit {process.ExitCode}): {sanitizedStderr}",
                    context);
            }

            throw operation switch
            {
                "fetch"    => (Exception)new GitFetchException(
                    $"git fetch failed (exit {process.ExitCode}): {sanitizedStderr}", context),
                "checkout" => new GitCheckoutException(
                    $"git checkout failed (exit {process.ExitCode}): {sanitizedStderr}", context),
                _          => new GitOperationException(
                    $"git {operation} failed (exit {process.ExitCode}): {sanitizedStderr}", context),
            };
        }

        if (!string.IsNullOrWhiteSpace(stderr))
            _logger.LogDebug("git {Args} stderr: {Stderr}", string.Join(' ', SanitizeArgsForLog(arguments)), stderr.Trim());

        return stdout;
    }

    /// <summary>
    /// Dumps every <c>http.*</c> git config entry across all scopes with
    /// values redacted, so the operator can see whether something the scrub
    /// missed is shadowing our per-command Authorization header. Runs only
    /// after the first auth failure so we don't bloat a healthy worker's
    /// logs with config dumps.
    /// </summary>
    private async Task DumpAuthFailureDiagnosticsAsync(string operation, CancellationToken ct)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "git",
                WorkingDirectory = _repoRoot,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            psi.ArgumentList.Add("config");
            psi.ArgumentList.Add("--list");
            psi.ArgumentList.Add("--show-origin");

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(10));

            using var process = new Process { StartInfo = psi };
            process.Start();

            // Drain both pipes concurrently — a small stderr write would
            // otherwise block the child indefinitely on a full pipe buffer.
            var stdoutTask = process.StandardOutput.ReadToEndAsync(cts.Token);
            var stderrTask = process.StandardError.ReadToEndAsync(cts.Token);
            await process.WaitForExitAsync(cts.Token);
            var stdout = await stdoutTask;
            _ = await stderrTask;

            var httpLines = stdout
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Where(l => l.Contains("\thttp.", StringComparison.OrdinalIgnoreCase))
                .Select(RedactConfigValue)
                .ToArray();

            _logger.LogError(
                "Auth failure diagnostic ({Operation}): "
                + "PAT length={PatLength}, scrub removed {ScrubTotal} entries at startup. "
                + "Current http.* config (values redacted):\n{Config}",
                operation, _pat.Length, _scrubResult.TotalRemoved,
                httpLines.Length == 0 ? "  (none)" : string.Join('\n', httpLines.Select(l => "  " + l)));
        }
        catch (Exception ex)
        {
            // Diagnostics must never crash the worker — log and move on.
            _logger.LogWarning(ex, "Failed to capture git auth diagnostic");
        }
    }

    /// <summary>
    /// Redacts the value half of a <c>git config --list --show-origin</c>
    /// line so a stale Authorization header in any scope cannot leak into
    /// logs. Keeps the origin (file path) and key visible — that's the
    /// useful part for the operator.
    /// </summary>
    private static string RedactConfigValue(string line)
    {
        // Format: `<origin>\t<key>=<value>`. Redact everything after the
        // first `=` past the tab.
        var tab = line.IndexOf('\t');
        if (tab < 0) return line;

        var head = line[..tab];
        var rest = line[(tab + 1)..];
        var eq = rest.IndexOf('=');
        if (eq < 0) return line;

        return $"{head}\t{rest[..eq]}=<REDACTED>";
    }

    /// <summary>
    /// Walks the argv we passed to <c>git</c> and returns the first
    /// subcommand name (e.g. <c>fetch</c>, <c>checkout</c>, <c>rev-parse</c>),
    /// skipping over <c>-c &lt;key=value&gt;</c> pairs and other top-level
    /// flags. Naive <c>FirstOrDefault(!StartsWith("-"))</c> would return the
    /// VALUE of <c>-c</c> (e.g. <c>http.extraHeader=...</c>) and end up
    /// logging a nonsense operation name.
    /// </summary>
    private static string ExtractOperation(string[] arguments)
    {
        for (int i = 0; i < arguments.Length; i++)
        {
            var a = arguments[i];
            // -c is the only top-level flag we currently emit and it always
            // takes one value. If we ever add others (--git-dir, etc.),
            // extend this.
            if (a == "-c") { i++; continue; }
            if (a.StartsWith('-')) continue;
            return a;
        }
        return "<unknown>";
    }

    private static string ContextSuffix(GitOpContext context)
    {
        var parts = new List<string>(3);
        if (!string.IsNullOrEmpty(context.WorkItemId)) parts.Add($"workItem={context.WorkItemId}");
        if (!string.IsNullOrEmpty(context.Branch))     parts.Add($"branch={context.Branch}");
        if (!string.IsNullOrEmpty(context.Commit))     parts.Add($"commit={context.Commit[..Math.Min(8, context.Commit.Length)]}");
        return parts.Count == 0 ? "" : " [" + string.Join(", ", parts) + "]";
    }

    /// <summary>
    /// Replaces any argument that looks like an HTTP Authorization header with
    /// a redacted placeholder so PATs never appear in logs or exception messages.
    /// </summary>
    private static IEnumerable<string> SanitizeArgsForLog(IEnumerable<string> args)
    {
        foreach (var a in args)
        {
            if (a.Contains("Authorization:", StringComparison.OrdinalIgnoreCase))
                yield return "http.extraHeader=Authorization: Basic <REDACTED>";
            else
                yield return a;
        }
    }
}
