using Bolt.Automation.WorkerAgent;
using Bolt.Automation.WorkerAgent.Health;
using Bolt.Automation.WorkerAgent.Logging;
using Bolt.Automation.WorkerAgent.Services;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using NLog;
using NLog.Config;
using NLog.Extensions.Logging;
using NLog.Layouts;
using NLog.Targets;
using Prometheus;

var options = WorkerOptions.FromEnvironment();

var builder = WebApplication.CreateSlimBuilder(args);

// Listen on configurable port (default 8080) for health + metrics endpoints
var healthPort = Environment.GetEnvironmentVariable("HEALTH_PORT") ?? "8080";
builder.WebHost.UseUrls($"http://+:{healthPort}");

// Bind options — single FromEnvironment() call, registered as IOptions<WorkerOptions>.
// Options.Create() produces a static wrapper.
builder.Services.AddSingleton(Options.Create(options));

// Log buffer (singleton shared between ForwardingTarget and LogForwarderService)
var logBuffer = new LogBuffer();
builder.Services.AddSingleton(logBuffer);

// Configure NLog
var nlogConfig = new LoggingConfiguration();

// Resolve the initial minimum level from LOG_LEVEL (default Info). Falls back
// to Info for any unrecognized value so a typo doesn't crash startup.
NLog.LogLevel initialLogLevel;
try
{
    initialLogLevel = NLog.LogLevel.FromString(options.LogLevel);
}
catch (ArgumentException)
{
    Console.Error.WriteLine(
        $"LOG_LEVEL='{options.LogLevel}' is not a valid NLog level; falling back to Info.");
    initialLogLevel = NLog.LogLevel.Info;
}

// Console target — level-configurable via LOG_LEVEL env var; test subprocess
// stdout/stderr lines are logged at Info/Warn so they appear here by default.
var consoleTarget = new ConsoleTarget("console")
{
    Layout = new SimpleLayout("${longdate} ${level:uppercase=true:padding=-5} [${logger:shortName=true}] ${message}${onexception:inner= ${exception:format=tostring}}"),
};
nlogConfig.AddRule(initialLogLevel, NLog.LogLevel.Fatal, consoleTarget);

// Forwarding target → LogBuffer → orchestrator (flushed every LogBatchIntervalMs).
// HTTP client noise filtering is handled inside ForwardingTarget.Write().
var forwardingTarget = new ForwardingTarget(logBuffer, options.WorkerId);
nlogConfig.AddRule(initialLogLevel, NLog.LogLevel.Fatal, forwardingTarget);

LogManager.Configuration = nlogConfig;

// Replace default logging with NLog
builder.Logging.ClearProviders();
builder.Logging.AddNLog();

// Health checks: /healthz (liveness) and /readyz (readiness)
builder.Services
    .AddHealthChecks()
    .AddCheck<LivenessCheck>("liveness", tags: ["live"])
    .AddCheck<ReadinessCheck>("readiness", tags: ["ready"]);

// Named HttpClient + OrchestratorClient singleton.
// Two clients share the base address but have different resilience profiles:
//  - "Orchestrator": short RPCs (heartbeat, deregister, results, build-status, logs).
//    Tight timeouts (15s attempt, 45s total, 2 retries).
//  - "OrchestratorPoll": long-polling GET /api/work only. Orchestrator may hold
//    the request open for up to ~25s while it waits for work to arrive; the tight
//    attempt timeout above would cut that off.
builder.Services.AddHttpClient("Orchestrator", http =>
{
    http.BaseAddress = new Uri(options.OrchestratorUrl);
    http.Timeout = TimeSpan.FromSeconds(60);
    http.DefaultRequestHeaders.Add("X-API-Key", options.ApiKey);
})
.ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
{
    PooledConnectionLifetime = TimeSpan.FromMinutes(5),
})
.AddStandardResilienceHandler(options =>
{
    // Default 10s was too aggressive — caused false timeouts that cascaded via circuit breaker
    options.AttemptTimeout = new HttpTimeoutStrategyOptions
    {
        Timeout = TimeSpan.FromSeconds(15),
    };
    options.TotalRequestTimeout = new HttpTimeoutStrategyOptions
    {
        Timeout = TimeSpan.FromSeconds(45),
    };
    // Require sustained failure before opening — prevents single slow response from breaking all comms
    options.CircuitBreaker = new HttpCircuitBreakerStrategyOptions
    {
        FailureRatio = 0.8,
        SamplingDuration = TimeSpan.FromSeconds(60),
        MinimumThroughput = 10,
        BreakDuration = TimeSpan.FromSeconds(10),
    };
    options.Retry = new HttpRetryStrategyOptions
    {
        MaxRetryAttempts = 2,
        UseJitter = true,
    };
});

builder.Services.AddHttpClient("OrchestratorPoll", http =>
{
    http.BaseAddress = new Uri(options.OrchestratorUrl);
    // HttpClient-level timeout must accommodate the orchestrator's 25s long-poll window
    // plus network overhead. Keep some headroom for a retry.
    http.Timeout = TimeSpan.FromSeconds(90);
    http.DefaultRequestHeaders.Add("X-API-Key", options.ApiKey);
})
.ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
{
    PooledConnectionLifetime = TimeSpan.FromMinutes(5),
})
.AddStandardResilienceHandler(pollOptions =>
{
    // Per-attempt ceiling must exceed the server's long-poll max (25s) plus buffer.
    pollOptions.AttemptTimeout = new HttpTimeoutStrategyOptions
    {
        Timeout = TimeSpan.FromSeconds(35),
    };
    // 90s = 2 attempts × 35s + headroom for handshakes/retries. Matches the
    // HttpClient-level Timeout above so neither layer is the bottleneck.
    pollOptions.TotalRequestTimeout = new HttpTimeoutStrategyOptions
    {
        Timeout = TimeSpan.FromSeconds(90),
    };
    pollOptions.CircuitBreaker = new HttpCircuitBreakerStrategyOptions
    {
        FailureRatio = 0.8,
        SamplingDuration = TimeSpan.FromSeconds(120),
        MinimumThroughput = 5,
        BreakDuration = TimeSpan.FromSeconds(10),
    };
    // Long polls are expensive to retry and the server returns 200 on timeout
    // anyway; one retry is enough for transient network glitches.
    pollOptions.Retry = new HttpRetryStrategyOptions
    {
        MaxRetryAttempts = 1,
        UseJitter = true,
    };
});

builder.Services.AddSingleton(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    var http = factory.CreateClient("Orchestrator");
    var pollHttp = factory.CreateClient("OrchestratorPoll");
    return new OrchestratorClient(http, pollHttp, options.WorkerId);
});

// Services
builder.Services.AddSingleton<GitService>();
builder.Services.AddSingleton<BuildService>();
builder.Services.AddSingleton<TestExecutor>();
builder.Services.AddSingleton<WorkerService>();

// Hosted service registration order matters: the Generic Host stops them in REVERSE order.
// We want shutdown to be: WorkerService (drain work) → HeartbeatService → LogForwarderService (final flush).
// So register in reverse: LogForwarderService first, HeartbeatService second, WorkerService last.
builder.Services.AddHostedService<LogForwarderService>();
builder.Services.AddHostedService<HeartbeatService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<WorkerService>());

var app = builder.Build();

// Health endpoints — Kubernetes probes
app.MapHealthChecks("/healthz", new() { Predicate = check => check.Tags.Contains("live") });
app.MapHealthChecks("/readyz", new() { Predicate = check => check.Tags.Contains("ready") });

// Prometheus metrics endpoint — scraped by Grafana/Prometheus
app.MapMetrics("/metrics");

var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("=== Test Orchestrator Worker Agent (.NET) ===");
logger.LogInformation("Worker ID:    {WorkerId}", options.WorkerId);
logger.LogInformation("Orchestrator: {OrchestratorUrl}", options.OrchestratorUrl);
logger.LogInformation("Concurrency:  {Concurrency}", options.Concurrency);
logger.LogInformation("Version:      {Version}", options.Version);
logger.LogInformation("Repo root:    {RepoRoot}", options.RepoRootPath);
logger.LogInformation("Health/metrics on port {Port}", healthPort);

// Surface PAT state at startup, before the first cross-branch work item hits
// and fails a live `git fetch`. Warn-only (not fatal) because develop-only
// workers with baked-in code can still run; cross-branch pickup will fail
// until the PAT is set. GitService emits a follow-up error the first time a
// network fetch actually fails with "could not read Username".
if (options.PatValidationWarning is not null)
{
    logger.LogWarning("{Warning}", options.PatValidationWarning);
}

if (string.IsNullOrEmpty(options.AzureDevOpsPat))
{
    logger.LogWarning(
        "AZURE_DEVOPS_PAT is not set — git fetch will fail for cross-branch work items. "
        + "Set it via the worker pod env (same secret the orchestrator uses).");
}
else
{
    // Length + redacted prefix + sha256 prefix gives operators enough to
    // confirm two pods are reading the same secret without leaking it.
    var pat = options.AzureDevOpsPat;
    var byteLen = System.Text.Encoding.UTF8.GetByteCount(pat);
    var prefix = pat.Length >= 6 ? $"{pat[..4]}…{pat[^2..]}" : "<short>";
    var hashHex = Convert.ToHexString(
        System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(pat)))[..8].ToLowerInvariant();
    logger.LogInformation(
        "AZURE_DEVOPS_PAT: configured ({ByteLen} bytes, {CharLen} chars, prefix={Prefix}, sha256={Hash})",
        byteLen, pat.Length, prefix, hashHex);
}

// Eagerly resolve GitService so the constructor-time `.git/config` scrub runs
// before any work loop starts. Surfaces a misbuilt image (no git binary) at
// boot rather than at first fetch.
var git = app.Services.GetRequiredService<Bolt.Automation.WorkerAgent.Services.GitService>();
var scrub = git.ConfigScrubResult;
if (scrub.TotalRemoved > 0)
{
    logger.LogInformation(
        "Git config scrub at startup: removed {Total} inherited extraheader entries "
        + "(local={Local}, global={Global}, system={System})",
        scrub.TotalRemoved, scrub.LocalRemoved, scrub.GlobalRemoved, scrub.SystemRemoved);
}

await app.RunAsync();

LogManager.Shutdown();
