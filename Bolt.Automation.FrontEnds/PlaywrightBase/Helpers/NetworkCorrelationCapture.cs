using System.Collections.Concurrent;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.Common.Models;
using Microsoft.Playwright;

namespace Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;

/// <summary>
/// A single entry in the network correlation history — one captured 4xx/5xx response.
/// </summary>
public record NetworkCorrelationEntry(
    string Url,
    string? CorrelationId,
    int StatusCode,
    string? ErrorMessage,
    IReadOnlyDictionary<string, string> Headers,
    DateTime CapturedAt);

/// <summary>
/// Subscribes to Playwright's <see cref="IPage.Response"/> event (equivalent to watching the
/// browser DevTools Network tab) and extracts correlation/tracing ids and error messages
/// from 4xx/5xx responses. Zero API-client dependency — pure UI layer.
/// </summary>
public sealed class NetworkCorrelationCapture : IDisposable
{
    // Header names checked in priority order
    private static readonly string[] CorrelationHeaders =
    [
        "x-correlation-id",
        "x-request-id",
        "x-trace-id",
        "traceparent",
        "X_Bolt_CorrelationId"
    ];

    // Shared across ALL NetworkCorrelationCapture instances for the same IPage.
    // PageFactory.CreatePage() creates a new PageHelper (and therefore a new capture)
    // on every call but never disposes old ones, so multiple captures end up subscribed
    // to the same page.Response event simultaneously. Using a static per-page key set
    // ensures only the very first occurrence of a path+status is ever logged as Debug,
    // regardless of how many capture instances are alive for that page.
    private static readonly ConcurrentDictionary<IPage, ConcurrentDictionary<string, byte>> _pageLoggedKeys = new();

    private readonly IPage _page;
    private readonly IAutomationLogger? _logger;
    private readonly List<NetworkCorrelationEntry> _history = [];
    private readonly object _lock = new();

    private volatile string? _latestCorrelationId;

    public NetworkCorrelationCapture(IPage page, IAutomationLogger? logger = null)
    {
        _page = page;
        _logger = logger;
        _page.Response += OnResponseAsync;
        _page.Close += OnPageClosed;
    }

    private void OnPageClosed(object? sender, IPage page)
    {
        _pageLoggedKeys.TryRemove(_page, out _);
    }

    /// <summary>The correlation id from the most recent qualifying (4xx/5xx) response. Thread-safe read.</summary>
    public string? LatestCorrelationId => _latestCorrelationId;

    /// <summary>Full ordered history of qualifying network responses captured since construction.</summary>
    public IReadOnlyList<NetworkCorrelationEntry> History
    {
        get { lock (_lock) { return _history.ToList(); } }
    }

    /// <summary>
    /// All captured errors grouped by status band: <c>"4xx"</c> and <c>"5xx"</c>, in chronological order.
    /// Useful for assertions and diagnostics when multiple requests fail during a single navigation.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<NetworkCorrelationEntry>> GroupedHistory
    {
        get
        {
            lock (_lock)
            {
                return _history
                    .GroupBy(e => e.StatusCode >= 500 ? "5xx" : "4xx")
                    .ToDictionary(
                        g => g.Key,
                        g => (IReadOnlyList<NetworkCorrelationEntry>)g.OrderBy(e => e.CapturedAt).ToList());
            }
        }
    }

    private async void OnResponseAsync(object? sender, IResponse response)
    {
        if (response.Status < 400)
            return;

        try
        {
            // Case-insensitive so the ContainsKey/indexer lookups below match a mixed-case
            // candidate against the lowercased names AllHeadersAsync() returns.
            var allHeaders = new Dictionary<string, string>(
                await response.AllHeadersAsync(), StringComparer.OrdinalIgnoreCase);

            var correlationId = ExtractCorrelationId(allHeaders);
            var errorMessage = await TryExtractJsonErrorMessageAsync(response, allHeaders);

            var capturedHeaders = CorrelationHeaders
                .Where(h => allHeaders.ContainsKey(h))
                .ToDictionary(h => h, h => allHeaders[h], StringComparer.OrdinalIgnoreCase);

            var entry = new NetworkCorrelationEntry(
                Url: response.Url,
                CorrelationId: correlationId,
                StatusCode: response.Status,
                ErrorMessage: errorMessage,
                Headers: capturedHeaders,
                CapturedAt: DateTime.UtcNow);

            // Normalise the dedup key to path-only so that requests to the same endpoint
            // with different query strings (timestamps, nonces, cache-busters) are treated
            // as the same URL for logging purposes.
            string urlPath = Uri.TryCreate(response.Url, UriKind.Absolute, out var parsedUri)
                ? parsedUri.AbsolutePath
                : response.Url;
            string dedupKey = $"{response.Status}:{urlPath}";

            lock (_lock) { _history.Add(entry); }

            if (correlationId != null)
                _latestCorrelationId = correlationId;

            // Use the shared per-page key set — TryAdd is atomic on ConcurrentDictionary,
            // so even if multiple capture instances fire for the same response simultaneously,
            // only the first TryAdd wins and logs at Debug; all others are silently dropped.
            var pageKeys = _pageLoggedKeys.GetOrAdd(_page, _ => new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase));
            if (pageKeys.TryAdd(dedupKey, 0))
            {
                _logger?.Debug(
                    $"[NetworkCapture] {response.Status} {response.Url} " +
                    $"CorrelationId={correlationId ?? "-"} " +
                    $"Message={errorMessage ?? "-"} " +
                    $"FullResponse = {await response.TextAsync()}");
            }
        }
        catch
        {
            // Swallow — never let network event handling break the test
        }
    }

    /// <summary>
    /// Picks the first correlation/tracing id present in <paramref name="headers"/>, in
    /// <see cref="CorrelationHeaders"/> priority order. Shared with <see cref="WaitHelper"/>
    /// so an API-response wait reports the same id this capture would have logged.
    /// </summary>
    internal static string? ExtractCorrelationId(IDictionary<string, string> headers)
    {
        foreach (var name in CorrelationHeaders)
        {
            // Case-insensitively: Playwright lowercases every name in AllHeadersAsync(), so an
            // exact-match lookup can never hit a mixed-case candidate like X_Bolt_CorrelationId.
            var value = headers
                .FirstOrDefault(h => string.Equals(h.Key, name, StringComparison.OrdinalIgnoreCase))
                .Value;

            if (!string.IsNullOrWhiteSpace(value))
                return value.Trim();
        }
        return null;
    }

    private static async Task<string?> TryExtractJsonErrorMessageAsync(IResponse response, IDictionary<string, string> headers)
    {
        if (!headers.TryGetValue("content-type", out var contentType))
            return null;
        if (!contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase))
            return null;

        try
        {
            var body = await response.TextAsync();
            if (string.IsNullOrWhiteSpace(body))
                return null;

            using var doc = System.Text.Json.JsonDocument.Parse(body);
            var root = doc.RootElement;

            // RFC 7807 ProblemDetails + common API error shapes
            foreach (var candidate in new[] { "detail", "title", "message", "error", "errorMessage", "description" })
            {
                if (root.TryGetProperty(candidate, out var prop) &&
                    prop.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    var text = prop.GetString();
                    if (!string.IsNullOrWhiteSpace(text))
                        return text.Trim();
                }
            }
        }
        catch
        {
            // Not parseable JSON — ignore
        }

        return null;
    }

    /// <summary>
    /// Finds the most recent captured entry whose URL matches the given <paramref name="urlPart"/>.
    /// Useful for correlating a specific navigation with its network error.
    /// </summary>
    public NetworkCorrelationEntry? FindLatestForUrl(string urlPart)
    {
        lock (_lock)
        {
            return _history.LastOrDefault(e =>
                e.Url.Contains(urlPart, StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <summary>
    /// Builds an <see cref="AppErrorContext"/> from the most recent 4xx/5xx network entry,
    /// optionally filtered to a specific URL part.
    /// </summary>
    public AppErrorContext? ToAppErrorContext(string? urlPart = null)
    {
        NetworkCorrelationEntry? entry;
        lock (_lock)
        {
            entry = urlPart != null
                ? _history.LastOrDefault(e => e.Url.Contains(urlPart, StringComparison.OrdinalIgnoreCase))
                : _history.LastOrDefault();
        }

        if (entry == null) return null;

        return new AppErrorContext
        {
            CorrelationId = entry.CorrelationId,
            ErrorUrl = entry.Url,
            ErrorMessage = entry.ErrorMessage,
            Source = AppErrorSource.NetworkResponse,
            NetworkStatusCode = entry.StatusCode,
            RawNetworkHeaders = entry.Headers.ToDictionary(k => k.Key, v => v.Value)
        };
    }

    public void Dispose()
    {
        _page.Response -= OnResponseAsync;
        _page.Close -= OnPageClosed;
    }
}
