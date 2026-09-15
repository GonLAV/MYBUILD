namespace Bolt.Automation.Common.Models;

/// <summary>
/// Describes where in the UI the error was detected.
/// </summary>
public enum AppErrorSource
{
    /// <summary>Ticket/error extracted from the URL path segment (e.g. /error/abc123-xyz).</summary>
    UrlPath,

    /// <summary>Ticket/correlation id extracted from a URL query parameter.</summary>
    UrlQuery,

    /// <summary>Ticket/correlation id scraped from visible DOM text (e.g. "Reference #: INC-001").</summary>
    UiDomText,

    /// <summary>Correlation id captured from a browser network response header (e.g. x-correlation-id).</summary>
    NetworkResponse
}

/// <summary>
/// Describes whether the error page/state was intentional in the test scenario.
/// </summary>
public enum AppErrorSeverity
{
    /// <summary>
    /// The test explicitly navigates to or asserts an error page — the error is the subject of the test.
    /// </summary>
    Expected,

    /// <summary>
    /// The test landed on an error page when it should not have — the error indicates an unintended failure.
    /// </summary>
    Unexpected
}

/// <summary>
/// A compact summary of a single 4xx/5xx response captured during the navigation that led to an
/// error page. A list of these is attached to <see cref="AppErrorContext"/> so diagnostics show
/// EVERY failing request in order (e.g. an extractToken 500 followed by an update 400), not just
/// the single response the primary context happened to merge.
/// </summary>
public record NetworkErrorSummary(int StatusCode, string Url, string? CorrelationId, string? Message)
{
    public override string ToString() =>
         $"{StatusCode} {AppErrorContext.TruncateForDisplay(Url)}" +
        (CorrelationId != null ? $" (corr={CorrelationId})" : string.Empty) +
        (Message != null ? $" — {Message}" : string.Empty);
}

/// <summary>
/// Structured record of a UI-layer application error: ticket number, correlation id, source, severity,
/// and any network-level context captured from the browser.
/// </summary>
public record AppErrorContext
{
    /// <summary>Ticket / incident number extracted from URL path or DOM (e.g. "abc123-xyz", "INC-001"). Null if not found.</summary>
    public string? TicketNumber { get; init; }

    /// <summary>Correlation id extracted from URL query, DOM, or a network response header. Null if not found.</summary>
    public string? CorrelationId { get; init; }

    /// <summary>The full URL at the time of detection.</summary>
    public string ErrorUrl { get; init; } = string.Empty;

    /// <summary>Error message scraped from the DOM or from a JSON response body. Null if not found.</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>How/where the error was detected.</summary>
    public AppErrorSource Source { get; init; }

    /// <summary>Whether the test expected this error (asserted) or hit it accidentally.</summary>
    public AppErrorSeverity Severity { get; init; }

    /// <summary>HTTP status code from the network response that triggered detection (4xx/5xx). Null for URL/DOM-only detection.</summary>
    public int? NetworkStatusCode { get; init; }

    /// <summary>Selected response headers from the network capture (e.g. x-correlation-id). Null for URL/DOM-only detection.</summary>
    public Dictionary<string, string>? RawNetworkHeaders { get; init; }

    /// <summary>
    /// Every 4xx/5xx response captured during the navigation, in chronological order. Populated when a
    /// network capture is available so the reader sees the full failure sequence (e.g. the request that
    /// actually redirected to the error page AND the earlier request that broke the flow), not just the
    /// single merged response. Empty when no network capture ran.
    /// </summary>
    public IReadOnlyList<NetworkErrorSummary> NetworkErrors { get; init; } = [];

    /// <summary>UTC timestamp when this context was captured.</summary>
    public DateTime DetectedAt { get; init; } = DateTime.UtcNow;

    public override string ToString() =>
       $"[{Severity}/{Source}] Ticket={TicketNumber ?? "N/A"} CorrelationId={CorrelationId ?? "N/A"} Status={NetworkStatusCode?.ToString() ?? "-"} URL={TruncateForDisplay(ErrorUrl)}" +
        (NetworkErrors.Count > 0 ? $" | Network errors: {string.Join("; ", NetworkErrors)}" : string.Empty);

    /// <summary>Caps a URL at a display-friendly length so a pathological value (e.g. a data: URI or
    /// base64 blob passed as a query param) doesn't blow up exception messages and test output.</summary>
    internal static string TruncateForDisplay(string value, int maxLength = 300) =>
        value.Length <= maxLength
            ? value
            : $"{value[..maxLength]}...[truncated, {value.Length} chars total]";
}
