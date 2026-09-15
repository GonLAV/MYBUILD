namespace Bolt.Automation.Common.Models;

/// <summary>
/// Structured record of an unexpected kickout page encountered during navigation.
/// Captures the displayed error code and message extracted from the page DOM.
/// </summary>
public record KickoutContext
{
    /// <summary>
    /// Numeric or alphanumeric error code extracted from the kickout message
    /// (e.g. "301", "203", "109"). Null if the DOM scan did not find a match.
    /// </summary>
    public string? ErrorCode { get; init; }

    /// <summary>Full error message text scraped from the kickout page subtitle. Null if not found.</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>The full URL at the time the kickout was detected.</summary>
    public string KickoutUrl { get; init; } = string.Empty;

    /// <summary>
    /// Whether the test expected to land on a kickout page (<see cref="AppErrorSeverity.Expected"/>)
    /// or arrived there unintentionally (<see cref="AppErrorSeverity.Unexpected"/>).
    /// Always <see cref="AppErrorSeverity.Unexpected"/> when thrown by <c>PageValidationHelper</c>;
    /// set to <see cref="AppErrorSeverity.Expected"/> in tests that explicitly assert kickout behaviour.
    /// </summary>
    public AppErrorSeverity Severity { get; init; } = AppErrorSeverity.Unexpected;

    /// <summary>UTC timestamp when the kickout was detected.</summary>
    public DateTime DetectedAt { get; init; } = DateTime.UtcNow;

    public override string ToString() =>
        $"[{Severity}] Kickout detected. ErrorCode={ErrorCode ?? "N/A"} URL={KickoutUrl}";
}
