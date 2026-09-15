using System.Text.RegularExpressions;

namespace Bolt.Automation.WorkerAgent;

public sealed class WorkerOptions
{
    public string OrchestratorUrl { get; set; } = "http://localhost:3001";
    public string ApiKey { get; set; } = "dev-api-key";
    public string WorkerId { get; set; } = $"worker-{Environment.MachineName}-{Environment.ProcessId}";
    public string PodName { get; set; } = Environment.MachineName;
    public string? NodeName { get; set; }
    public int Concurrency { get; set; } = 4;
    public int HeartbeatIntervalMs { get; set; } = 10_000;
    public string Version { get; set; } = "1.0.0";
    public string TestProjectPath { get; set; } = "/app/Bolt.Automation.Tests/Bolt.Automation.Tests.csproj";
    public string ResultsDirectory { get; set; } = "/app/results";
    public string SolutionPath { get; set; } = "/app/Bolt.Automation.sln";
    /// <summary>
    /// Per-work-item kill budget for `dotnet test` (one test per work item). 300s
    /// proved too tight in production: over 30 days, zero orchestrator-run tests
    /// finished above 300s (the kill is absolute) while 75 were killed mid-test —
    /// including a suite that passes locally at ~607s average and therefore could
    /// never pass on a worker. 900s covers every legitimate duration observed;
    /// genuine hangs are still reclaimed, and the logger marks their tests
    /// "NoResult" with the timeout reason.
    /// </summary>
    public int TestTimeoutSeconds { get; set; } = 900;
    public int LogBatchIntervalMs { get; set; } = 1000;
    public int LogBatchSize { get; set; } = 500;
    /// <summary>
    /// Initial NLog minimum level for console + forwarding targets. Valid values:
    /// Trace, Debug, Info, Warn, Error, Fatal. The orchestrator can override this
    /// at runtime via the heartbeat response's logLevel field.
    /// </summary>
    public string LogLevel { get; set; } = "Info";

    // Git / self-update options
    public string RepoRootPath { get; set; } = "/app";
    public int BuildTimeoutSeconds { get; set; } = 300;
    public int GitTimeoutSeconds { get; set; } = 120;

    /// <summary>
    /// Azure DevOps Personal Access Token used to authenticate network git
    /// operations (<c>fetch</c>, <c>clone</c>) against the test repository.
    /// Passed per-command via <c>-c http.extraHeader=Authorization: Basic …</c>
    /// to avoid persistent global config and keep the secret off disk.
    /// Empty/null = unconfigured; cross-branch pickup will fail until set.
    /// Always set via <see cref="NormalizePat"/> so trailing newlines from
    /// K8s secrets and similar accidents do not corrupt the Basic credential.
    /// </summary>
    public string AzureDevOpsPat { get; set; } = "";

    /// <summary>
    /// Non-fatal warning emitted by <see cref="NormalizePat"/> when the env
    /// var looked malformed (wrong length, invalid charset). Surfaced at
    /// startup in <c>Program.cs</c> so operators can spot truncation/garbling
    /// without leaking the full secret. Null when the value parses cleanly
    /// or when no PAT was provided.
    /// </summary>
    public string? PatValidationWarning { get; set; }

    public static WorkerOptions FromEnvironment()
    {
        var opts = new WorkerOptions
        {
            // TODO: once migration is validated, update k8s ConfigMap ORCHESTRATOR_URL
            // to https://nexus-logger.auto.boltx.us and revert this to Env("ORCHESTRATOR_URL", "http://localhost:3001")
            OrchestratorUrl = "https://nexus-logger.auto.boltx.us",
            ApiKey = Env("API_KEY", "dev-api-key"),
            WorkerId = Env("WORKER_ID", $"worker-{Environment.MachineName}-{Environment.ProcessId}"),
            PodName = Env("POD_NAME", Environment.MachineName),
            NodeName = Environment.GetEnvironmentVariable("NODE_NAME"),
            Concurrency = ParseInt("WORKER_CONCURRENCY", 4),
            HeartbeatIntervalMs = ParseInt("HEARTBEAT_INTERVAL", 10000),
            Version = Env("WORKER_VERSION", "1.0.0"),
            TestProjectPath = Env("TEST_PROJECT_PATH", "/app/Bolt.Automation.Tests/Bolt.Automation.Tests.csproj"),
            ResultsDirectory = Env("RESULTS_DIRECTORY", "/app/results"),
            SolutionPath = Env("SOLUTION_PATH", "/app/Bolt.Automation.sln"),
            TestTimeoutSeconds = ParseInt("TEST_TIMEOUT_SECONDS", 900),
            LogBatchIntervalMs = ParseInt("LOG_BATCH_INTERVAL_MS", 1000),
            LogBatchSize = ParseInt("LOG_BATCH_SIZE", 500),
            LogLevel = Env("LOG_LEVEL", "Info"),
            RepoRootPath = Env("REPO_ROOT_PATH", "/app"),
            BuildTimeoutSeconds = ParseInt("BUILD_TIMEOUT_SECONDS", 300),
            GitTimeoutSeconds = ParseInt("GIT_TIMEOUT_SECONDS", 120),
        };

        opts.AzureDevOpsPat = NormalizePat(Env("AZURE_DEVOPS_PAT", ""), out var patWarning);
        opts.PatValidationWarning = patWarning;
        return opts;
    }

    /// <summary>
    /// Cleans up a PAT-shaped env var value before it reaches the credential
    /// path. K8s <c>Secret</c>s populated via <c>--from-file</c> or YAML
    /// literal-block scalars (<c>value: |</c>) carry a trailing newline; YAML
    /// quoting accidents leave the value wrapped in <c>" "</c> or <c>' '</c>.
    /// Either of those corrupts the <c>Basic</c> credential we Base64 it
    /// into and produces the same misleading <c>could not read Username</c>
    /// fatal that a missing PAT does.
    ///
    /// We trim, strip a single layer of surrounding quotes, then warn (but do
    /// not fail) if the result doesn't match the canonical Azure DevOps PAT
    /// shape (52 chars, lowercase base32-ish). The shape check is a soft
    /// guard — Microsoft has changed PAT formats before — so we never refuse
    /// to start; we just flag obvious truncation/garbling so an operator can
    /// see it next to the startup banner.
    /// </summary>
    internal static string NormalizePat(string raw, out string? warning)
    {
        warning = null;
        if (string.IsNullOrEmpty(raw))
        {
            return "";
        }

        // Strip whitespace + the four common zero-width characters that
        // survive copy/paste through Slack, browsers, and YAML literal
        // scalars. The second .Trim() catches whitespace exposed by the
        // zero-width strip (e.g. " <ZWSP> " → "<ZWSP>" → "").
        var cleaned = raw
            .Trim()
            .Trim('​', '‌', '‍', '﻿')
            .Trim();

        // Strip a single layer of surrounding quotes (a common YAML accident).
        if (cleaned.Length >= 2 &&
            ((cleaned[0] == '"' && cleaned[^1] == '"') ||
             (cleaned[0] == '\'' && cleaned[^1] == '\'')))
        {
            cleaned = cleaned[1..^1].Trim();
        }

        if (cleaned.Length == 0)
        {
            return "";
        }

        // Soft validation. Azure DevOps PATs today are 52 chars of [a-z2-7],
        // but we don't refuse non-matching values — Microsoft has changed
        // formats before, and the worker must still try them.
        const int ExpectedLength = 52;
        var shapeOk = cleaned.Length == ExpectedLength
            && Regex.IsMatch(cleaned, "^[a-z2-7]+$");
        if (!shapeOk)
        {
            warning =
                $"AZURE_DEVOPS_PAT does not match the expected Azure DevOps PAT shape "
                + $"({ExpectedLength} chars of [a-z2-7]); got {cleaned.Length} chars. "
                + "If git fetch fails with 'could not read Username', the value may be "
                + "truncated or wrapped in quotes — check the K8s Secret source.";
        }

        return cleaned;
    }

    private static string Env(string name, string defaultValue)
        => Environment.GetEnvironmentVariable(name) ?? defaultValue;

    private static int ParseInt(string envName, int defaultValue)
    {
        var raw = Environment.GetEnvironmentVariable(envName);
        if (raw is null) return defaultValue;
        if (int.TryParse(raw, out var value)) return value;
        throw new InvalidOperationException(
            $"Environment variable '{envName}' has value '{raw}' which is not a valid integer");
    }
}
