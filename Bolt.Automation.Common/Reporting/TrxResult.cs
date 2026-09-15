namespace Bolt.Automation.Common.Reporting;

/// <summary>
/// Structured result of parsing a Visual Studio Test Results (.trx) file.
/// Shared by <see cref="TrxParser"/> and consumers that need test-run counts,
/// duration, and a bounded error summary.
/// </summary>
public sealed record TrxResult
{
    public int Total { get; init; }
    public int Passed { get; init; }
    public int Failed { get; init; }
    public int Skipped { get; init; }
    public double DurationMs { get; init; }
    public string? ErrorSummary { get; init; }
}
