using Bolt.Automation.WorkerAgent.Logging;
using Bolt.Automation.WorkerAgent.Models;
using Microsoft.Extensions.Options;

namespace Bolt.Automation.WorkerAgent.Services;

/// <summary>
/// Periodically drains the LogBuffer and POSTs batches to the orchestrator.
/// Best-effort — failures are logged but don't crash the worker.
/// </summary>
public sealed class LogForwarderService : BackgroundService
{
    private readonly LogBuffer _logBuffer;
    private readonly OrchestratorClient _client;
    private readonly WorkerOptions _options;
    private readonly ILogger<LogForwarderService> _logger;

    public LogForwarderService(
        LogBuffer logBuffer,
        OrchestratorClient client,
        IOptions<WorkerOptions> options,
        ILogger<LogForwarderService> logger)
    {
        _logBuffer = logBuffer;
        _client = client;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(_options.LogBatchIntervalMs));

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await FlushAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Shutdown — do a final flush
        }

        // Final flush with timeout so we don't block container exit
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await FlushAsync(cts.Token);
    }

    private async Task FlushAsync(CancellationToken ct)
    {
        try
        {
            var entries = _logBuffer.Drain(_options.LogBatchSize);
            if (entries.Length == 0) return;

            var batch = new LogBatchRequest(_options.WorkerId, entries);
            await _client.SendLogsAsync(batch, ct);
        }
        catch (OperationCanceledException)
        {
            // ignore during shutdown
        }
        catch (Exception ex)
        {
            var remaining = _logBuffer.Count;
            var dropped = _logBuffer.DroppedCount;
            _logger.LogWarning(ex,
                "Failed to forward log batch to orchestrator ({RemainingCount} entries remaining, {DroppedCount} total dropped)",
                remaining, dropped);
        }
    }
}
