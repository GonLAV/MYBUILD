namespace Bolt.Automation.WorkerAgent.Models;

// ── Code Version ──

public sealed record CodeVersion(
    string Branch,
    string CommitHash,
    string BuiltAt
);

public sealed record DevelopHead(
    string CommitHash,
    string Branch
);

// ── Deregistration ──
// (Registration is now implicit — the first heartbeat carrying the full
//  static payload auto-registers the worker on the orchestrator.)

public sealed record DeregisterRequest(string WorkerId, string? Reason = null);
public sealed record DeregisterResponse(bool Acknowledged);

// ── Heartbeat ──

public sealed record HeartbeatRequest(
    string WorkerId,
    string Status,
    string[] CurrentWorkItemIds,
    CodeVersion CodeVersion,
    string PodName,
    string? NodeName,
    int CpuCount,
    int MaxConcurrency,
    string Version,
    int ReadySlots
);

public sealed record HeartbeatResponse(
    bool Acknowledged,
    string Action,
    DevelopHead? DevelopHead = null,
    string? LogLevel = null
);

// ── Work ──

public sealed record WorkResponse(
    bool HasWork,
    int? RetryAfter = null,
    WorkItemDto? WorkItem = null,
    Dictionary<string, string>? Environment = null
);

public sealed record WorkItemDto(
    string Id,
    string JobId,
    string FullyQualifiedName,
    string DisplayName,
    int? TestCaseId = null,
    string? Branch = null,
    string? CommitHash = null
);

// ── Release ──
// Worker-initiated release of a claimed work item. Sent when the worker has
// claimed an item via /api/work but discovers it cannot execute it (slot
// contention, code version shifted mid-wait, drain). Orchestrator flips the
// item back to pending and decrements attempt so the claim is not counted.

public sealed record ReleaseWorkItemRequest(string WorkerId, string? Reason = null);
public sealed record ReleaseWorkItemResponse(bool Released);

// ── Results ──

public sealed record SubmitResultRequest(
    string WorkItemId,
    string JobId,
    string WorkerId,
    bool Passed,
    double Duration,
    int TotalTests,
    int PassedTests,
    int FailedTests,
    int SkippedTests,
    string? ErrorSummary = null,
    CodeVersion? CodeVersion = null
);

public sealed record ResultResponse(bool Acknowledged);

// ── Build Status ──

public sealed record BuildStatusRequest(
    string Branch,
    string CommitHash,
    bool Success,
    string? Error,
    long Duration,
    string? WorkItemId = null
);

public sealed record BuildStatusResponse(bool Acknowledged);

// ── Logs ──

public sealed record LogEntryDto(
    string WorkerId,
    string Timestamp,
    string Level,
    string Category,
    string Message,
    string? WorkItemId = null,
    string? JobId = null,
    int? SlotId = null
);

public sealed record LogBatchRequest(
    string WorkerId,
    LogEntryDto[] Entries
);

public sealed record LogBatchResponse(bool Accepted, int Count);

// TrxResult has moved to Bolt.Automation.Common.Reporting so it can be shared
// with Bolt.Automation.AgentTools without WorkerAgent's ASP.NET / Prometheus deps.
