using Bolt.Automation.Common.Reporting;

namespace Bolt.Automation.AgentTools.Failure;

/// <summary>A located on-disk artifact (page source or screenshot).</summary>
public sealed record ArtifactRef(
    bool Found,
    string? Path,
    string? CapturedAt,
    long? Bytes);

/// <summary>
/// Page-source / screenshot lookup result for a test, including the resolved
/// folder and a nearest-match suggestion when the exact folder isn't present.
/// </summary>
public sealed record TestArtifacts(
    string Method,
    string FolderName,
    bool FolderFound,
    string? FolderPath,
    ArtifactRef PageSource,
    ArtifactRef Screenshot,
    string? Suggestion);

/// <summary>The matching test's record extracted from a TRX run file.</summary>
public sealed record TrxMatch(
    bool Found,
    string? TrxPath,
    string? Outcome,
    double? DurationMs,
    string? StartTime,
    string? Message,
    string? StackTrace,
    string? ClassName,
    string? TestName,
    TrxResult? RunSummary);

/// <summary>The full cross-referenced diagnosis returned by <c>failure summarize</c>.</summary>
public sealed record FailureDiagnosis(
    string Test,
    string Method,
    string? Class,
    TrxMatch Trx,
    ArtifactRef PageSource,
    ArtifactRef Screenshot,
    string? Suggestion);
