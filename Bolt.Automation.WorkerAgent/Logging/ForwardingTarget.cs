using Bolt.Automation.WorkerAgent.Models;
using NLog;
using NLog.Targets;

namespace Bolt.Automation.WorkerAgent.Logging;

/// <summary>
/// Custom NLog target that forwards log entries to the LogBuffer
/// for batch-sending to the orchestrator.
/// </summary>
[Target("Forwarding")]
public sealed class ForwardingTarget : Target
{
    private readonly LogBuffer _logBuffer;
    private readonly string _workerId;

    public ForwardingTarget(LogBuffer logBuffer, string workerId)
    {
        _logBuffer = logBuffer;
        _workerId = workerId;
        Name = "forwarding";
    }

    // Logger prefixes that generate high-frequency noise (20+ lines/sec)
    // and flood the orchestrator's 1000-entry ring buffer
    private static readonly string[] _noisyPrefixes =
    [
        "System.Net.Http.",
        "Microsoft.Extensions.Http.",
        "Polly",                          // Resilience pipeline attempt-level logs
    ];

    protected override void Write(LogEventInfo logEvent)
    {
        // Suppress HTTP client noise at Info/Debug level — only forward Warn+ for these
        var loggerName = logEvent.LoggerName ?? "";
        if (logEvent.Level < NLog.LogLevel.Warn)
        {
            foreach (var prefix in _noisyPrefixes)
            {
                if (loggerName.StartsWith(prefix, StringComparison.Ordinal))
                    return;
            }
        }

        var level = logEvent.Level.Name.ToLowerInvariant() switch
        {
            "trace" => "trace",
            "debug" => "debug",
            "info" => "info",
            "warn" => "warn",
            "error" => "error",
            "fatal" => "fatal",
            _ => "info",
        };

        // Extract scope properties for work item / job / slot context
        string? workItemId = null;
        string? jobId = null;
        int? slotId = null;

        if (logEvent.Properties.TryGetValue("WorkItemId", out var wiObj))
            workItemId = wiObj?.ToString();

        if (logEvent.Properties.TryGetValue("JobId", out var jObj))
            jobId = jObj?.ToString();

        if (logEvent.Properties.TryGetValue("SlotId", out var slotObj)
            && slotObj is int sid)
            slotId = sid;

        var entry = new LogEntryDto(
            WorkerId: _workerId,
            Timestamp: logEvent.TimeStamp.ToUniversalTime().ToString("O"),
            Level: level,
            Category: logEvent.LoggerName ?? "Unknown",
            Message: logEvent.FormattedMessage,
            WorkItemId: workItemId,
            JobId: jobId,
            SlotId: slotId
        );

        _logBuffer.Add(entry);
    }
}
