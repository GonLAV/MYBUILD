using Microsoft.Extensions.Options;
using NLog;

namespace Bolt.Automation.WorkerAgent.Services;

/// <summary>
/// Sends periodic heartbeats to the orchestrator.
/// Triggers graceful drain if the orchestrator responds with "drain".
/// Signals the worker to rebuild if the orchestrator responds with "update".
/// Shuts down the application once drain is complete or after prolonged heartbeat failure.
/// </summary>
public sealed class HeartbeatService : BackgroundService
{
    private readonly OrchestratorClient _client;
    private readonly WorkerOptions _options;
    private readonly WorkerService _workerService;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<HeartbeatService> _logger;

    /// <summary>
    /// Number of consecutive heartbeat failures before forcing shutdown.
    /// At a 10s heartbeat interval, 30 failures ≈ 5 minutes.
    /// </summary>
    private const int MaxConsecutiveFailures = 30;

    private int _consecutiveHeartbeatFailures;

    /// <summary>
    /// Currently-applied runtime log-level override. Tracked so we only reconfigure
    /// NLog when the orchestrator changes (or clears) the override, not on every
    /// heartbeat response.
    /// </summary>
    private string? _appliedLogLevel;

    public HeartbeatService(
        OrchestratorClient client,
        IOptions<WorkerOptions> options,
        WorkerService workerService,
        IHostApplicationLifetime lifetime,
        ILogger<HeartbeatService> logger)
    {
        _client = client;
        _options = options.Value;
        _workerService = workerService;
        _lifetime = lifetime;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Send an immediate heartbeat on startup — it doubles as registration.
        // WorkerService.ExecuteAsync is blocked on _firstHeartbeatComplete until this succeeds.
        await SendOnceAsync(stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(_options.HeartbeatIntervalMs));

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await SendOnceAsync(stoppingToken);
                if (stoppingToken.IsCancellationRequested) return;
            }
        }
        catch (OperationCanceledException)
        {
            // normal shutdown
        }
    }

    private async Task SendOnceAsync(CancellationToken stoppingToken)
    {
        try
        {
            var activeIds = _workerService.ActiveWorkItemIds;
            var status = _workerService.CurrentStatus;
            var codeVersion = _workerService.CodeVersion;
            var readySlots = _workerService.ReadySlots;
            var response = await _client.HeartbeatAsync(activeIds, status, codeVersion, readySlots, _options, stoppingToken);

            _consecutiveHeartbeatFailures = 0;

            // First successful heartbeat acts as registration — release the WorkerService gate.
            _workerService.SignalFirstHeartbeatComplete();

            if (response.Action == "drain")
            {
                _logger.LogWarning("Received drain signal from orchestrator — initiating graceful drain");
                _workerService.RequestDrain();
            }

            if (response.DevelopHead != null)
            {
                // Always pass developHead — OnDevelopHeadChanged decides whether a rebuild
                // is needed. On the first heartbeat this replaces the old synchronous
                // SyncToDevelopHeadAsync path.
                _workerService.OnDevelopHeadChanged(response.DevelopHead);
            }

            ApplyLogLevelOverride(response.LogLevel);

            if (_workerService.IsDrained)
            {
                _logger.LogInformation("Drain complete — all work items finished, shutting down");
                _lifetime.StopApplication();
            }
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _consecutiveHeartbeatFailures++;
            _logger.LogWarning(ex, "Heartbeat failed ({Count}/{Max} consecutive failures)",
                _consecutiveHeartbeatFailures, MaxConsecutiveFailures);

            if (_consecutiveHeartbeatFailures >= MaxConsecutiveFailures)
            {
                _logger.LogError("Heartbeat failed {Count} consecutive times — forcing shutdown",
                    _consecutiveHeartbeatFailures);
                _lifetime.StopApplication();
                return;
            }

            if (_workerService.IsDrained)
            {
                _logger.LogInformation("Drain complete — shutting down (heartbeat unavailable)");
                _lifetime.StopApplication();
            }
        }
    }

    /// <summary>
    /// Applies a runtime log-level override from the orchestrator's heartbeat response.
    /// A null/empty value reverts to the env-var default (<see cref="WorkerOptions.LogLevel"/>).
    /// Reconfigures NLog only when the effective level changes, so steady-state heartbeats
    /// don't thrash the logging pipeline.
    /// </summary>
    private void ApplyLogLevelOverride(string? requestedLevel)
    {
        var target = string.IsNullOrWhiteSpace(requestedLevel) ? _options.LogLevel : requestedLevel;
        if (string.Equals(_appliedLogLevel, target, StringComparison.OrdinalIgnoreCase)) return;

        NLog.LogLevel parsed;
        try
        {
            parsed = NLog.LogLevel.FromString(target);
        }
        catch (ArgumentException)
        {
            _logger.LogWarning("Orchestrator sent unknown log level '{Level}' — ignoring", target);
            return;
        }

        var config = LogManager.Configuration;
        if (config == null) return;

        foreach (var rule in config.LoggingRules)
        {
            rule.SetLoggingLevels(parsed, NLog.LogLevel.Fatal);
        }
        LogManager.ReconfigExistingLoggers();

        _logger.LogInformation("Log level changed to {Level} ({Source})",
            parsed.Name,
            string.IsNullOrWhiteSpace(requestedLevel) ? "env default" : "orchestrator override");

        _appliedLogLevel = target;
    }
}
