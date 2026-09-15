using System.Text.RegularExpressions;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.Common.Models;
using Microsoft.Playwright;
using MongoDB.Bson;

namespace Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;

/// <summary>
/// Detects application error pages and extracts structured error context
/// (ticket number, correlation id, error message) from three sources:
/// URL path/query, visible DOM text, and browser network headers (via <see cref="NetworkCorrelationCapture"/>).
/// Groups results by <see cref="AppErrorSource"/> and <see cref="AppErrorSeverity"/>.
/// </summary>
public static class ErrorPageDetector
{
    // ── URL patterns ──────────────────────────────────────────────────────────

    // /error/{ticketId}  or  /error/technical  or  /error  (bare)
    private static readonly Regex PathTicketRegex =
        new(@"/error/([A-Za-z0-9_\-]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // ?ticket=xxx  or  ?ticketNumber=xxx
    private static readonly Regex QueryTicketRegex =
        new(@"[?&]ticket(?:Number)?=([^&\s]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // ?correlationId=xxx  or  ?correlation_id=xxx  or  ?correlationid=xxx
    private static readonly Regex QueryCorrelationRegex =
        new(@"[?&]correlat(?:ion)?[_-]?(?:[Ii]d)?=([^&\s]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // ── DOM text patterns ─────────────────────────────────────────────────────

    // Matches: "Ticket: abc123", "Ticket Number: abc", "Ticket ID: abc", "Reference #: abc"
    private static readonly Regex DomTicketRegex =
        new(@"(?:Ticket\s*(?:Number|ID|#)?|Reference\s*#|Incident)\s*[:=]\s*([A-Za-z0-9_\-]+)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Matches: "Correlation ID: abc", "CorrelationId: abc"
    private static readonly Regex DomCorrelationRegex =
        new(@"Correlation\s*(?:ID|Id)\s*[:=]\s*([A-Za-z0-9_\-]+)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Bare UUID (fallback — only captured near error context in DOM scan)
    private static readonly Regex UuidRegex =
        new(@"\b([0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12})\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // ── Non-id path labels that should not be treated as ticket numbers ───────
    private static readonly HashSet<string> NonTicketPathLabels =
        new(StringComparer.OrdinalIgnoreCase) { "technical", "generic", "forbidden", "unauthorized", "notfound", "maintenance" };

    // ── Kickout page patterns ─────────────────────────────────────────────────

    // CSS selector for the kickout error message element (ADBX and similar frontends)
    private const string KickoutMessageSelector = "app-kickout-page p.subtitle";

    // Matches "Error code: 301" or "Error code: ABC" inside a kickout message
    private static readonly Regex KickoutErrorCodeRegex =
        new(@"Error code:\s*([A-Za-z0-9]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Attempts to detect an application error context purely from the URL string.
    /// Fast, synchronous, no Playwright round-trip required.
    /// Returns null if the URL does not appear to be an app error page.
    /// </summary>
    public static AppErrorContext? TryDetectFromUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;

        string? ticketNumber = null;
        string? correlationId = null;
        var source = AppErrorSource.UrlPath;

        // 1. Path segment: /error/{id}
        var pathMatch = PathTicketRegex.Match(url);
        if (pathMatch.Success)
        {
            var segment = pathMatch.Groups[1].Value;
            if (!NonTicketPathLabels.Contains(segment))
                ticketNumber = segment;
            // Even if segment is "technical" (no ticket id), we still return a context for the error URL.
        }
        else if (!url.Contains("/error", StringComparison.OrdinalIgnoreCase))
        {
            // Not an error URL at all — only try query params if URL already looks like an error page
            // (partial match for e.g. ?ticket= on non-error pages would be noise)
        }

        // 2. Query: ?ticket= / ?ticketNumber=
        var qTicket = QueryTicketRegex.Match(url);
        if (qTicket.Success)
        {
            ticketNumber ??= Uri.UnescapeDataString(qTicket.Groups[1].Value);
            source = AppErrorSource.UrlQuery;
        }

        // 3. Query: ?correlationId=
        var qCorr = QueryCorrelationRegex.Match(url);
        if (qCorr.Success)
        {
            correlationId = Uri.UnescapeDataString(qCorr.Groups[1].Value);
            source = AppErrorSource.UrlQuery;
        }

        // Only return a context if the URL looks like an error page
        if (!url.Contains("/error", StringComparison.OrdinalIgnoreCase))
            return null;

        return new AppErrorContext
        {
            TicketNumber = ticketNumber,
            CorrelationId = correlationId,
            ErrorUrl = url,
            Source = source,
            Severity = AppErrorSeverity.Unexpected // caller overrides when expected
        };
    }

    /// <summary>
    /// Scans the visible page DOM for error ticket numbers and correlation ids.
    /// Uses a single JS round-trip (same pattern as <c>GetValidationMessagesAsync</c>).
    /// Returns null if nothing is found or the DOM scan fails.
    /// </summary>
    public static async Task<AppErrorContext?> TryDetectFromDomAsync(IPage page, IAutomationLogger? logger = null)
    {
        try
        {
            var texts = await page.EvaluateAsync<string[]>(@"
                () => {
                    const errorKeywords = /error|ticket|incident|reference|correlation|fault/i;
                    const seen = new Set();
                    const out = [];
                    // Walk all visible text nodes inside body
                    const walker = document.createTreeWalker(document.body, NodeFilter.SHOW_TEXT);
                    let node;
                    while ((node = walker.nextNode())) {
                        const t = (node.textContent || '').trim();
                        if (!t || t.length < 4 || t.length > 300) continue;
                        // Only include text nodes near error context
                        const parent = node.parentElement;
                        if (!parent) continue;
                        // Skip hidden elements
                        const style = window.getComputedStyle(parent);
                        if (style.display === 'none' || style.visibility === 'hidden' || parseFloat(style.opacity) === 0) continue;
                        if (!seen.has(t)) { seen.add(t); out.push(t); }
                    }
                    return out;
                }");

            if (texts == null || texts.Length == 0)
                return null;

            string? ticketNumber = null;
            string? correlationId = null;
            string? errorMessage = null;

            foreach (var text in texts)
            {
                var m = DomTicketRegex.Match(text);
                if (m.Success) ticketNumber ??= m.Groups[1].Value;

                var c = DomCorrelationRegex.Match(text);
                if (c.Success) correlationId ??= c.Groups[1].Value;

                // Capture first non-trivially short text that looks like an error message
                if (errorMessage == null && text.Length > 20 &&
                    Regex.IsMatch(text, @"error|sorry|problem|failed|unavailable|ticket", RegexOptions.IgnoreCase))
                {
                    errorMessage = text;
                }
            }

            // Fallback: bare UUID in any visible text (last resort)
            if (ticketNumber == null && correlationId == null)
            {
                foreach (var text in texts)
                {
                    var u = UuidRegex.Match(text);
                    if (u.Success)
                    {
                        ticketNumber = u.Value;
                        break;
                    }
                }
            }

            if (ticketNumber == null && correlationId == null && errorMessage == null)
                return null;

            return new AppErrorContext
            {
                TicketNumber = ticketNumber,
                CorrelationId = correlationId,
                ErrorUrl = page.Url,
                ErrorMessage = errorMessage,
                Source = AppErrorSource.UiDomText,
                Severity = AppErrorSeverity.Unexpected
            };
        }
        catch (Exception ex)
        {
            logger?.Debug($"[ErrorPageDetector] DOM scan failed: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Combines URL detection and DOM scanning, merging results into a single context.
    /// The URL result takes precedence; DOM fills in any missing fields.
    /// </summary>
    public static async Task<AppErrorContext?> TryDetectAsync(IPage page, IAutomationLogger? logger = null)
    {
        var urlCtx = TryDetectFromUrl(page.Url);
        var domCtx = await TryDetectFromDomAsync(page, logger);

        if (urlCtx == null && domCtx == null)
            return null;

        // Merge: URL is primary source for ticket/url, DOM fills in missing fields
        return new AppErrorContext
        {
            TicketNumber = urlCtx?.TicketNumber ?? domCtx?.TicketNumber,
            CorrelationId = urlCtx?.CorrelationId ?? domCtx?.CorrelationId,
            ErrorUrl = page.Url,
            ErrorMessage = domCtx?.ErrorMessage,
            Source = urlCtx != null ? urlCtx.Source : AppErrorSource.UiDomText,
            Severity = AppErrorSeverity.Unexpected  // caller overrides to Expected when intentional
        };
    }

    /// <summary>
    /// Enriches an existing <see cref="AppErrorContext"/> with correlation id and network status
    /// captured by the browser network capture. The existing <paramref name="ctx"/> fields are
    /// not overwritten — network data only fills in what is missing.
    /// </summary>
    public static AppErrorContext MergeWithNetworkCapture(AppErrorContext ctx, NetworkCorrelationCapture capture)
    {
        var networkCtx = capture.ToAppErrorContext(ctx.ErrorUrl.Length > 10 ? new Uri(ctx.ErrorUrl).Host : null);

        return ctx with
        {
            CorrelationId = ctx.CorrelationId ?? networkCtx?.CorrelationId,
            NetworkStatusCode = ctx.NetworkStatusCode ?? networkCtx?.NetworkStatusCode,
            RawNetworkHeaders = ctx.RawNetworkHeaders ?? networkCtx?.RawNetworkHeaders,
            ErrorMessage = ctx.ErrorMessage ?? networkCtx?.ErrorMessage
        };
    }

    /// <summary>
    /// Overload that accepts the network entries exposed via <c>IPageHelper.GetNetworkErrors()</c>
    /// so callers do not need a direct reference to <see cref="NetworkCorrelationCapture"/>.
    /// </summary>
    public static AppErrorContext MergeWithNetworkErrors(AppErrorContext ctx, IReadOnlyList<NetworkCorrelationEntry> networkErrors)
    {
        var best = networkErrors.LastOrDefault(e =>
            ctx.ErrorUrl.Length > 0 && e.Url.Contains(
                new Uri(ctx.ErrorUrl.StartsWith("http") ? ctx.ErrorUrl : "http://x" + ctx.ErrorUrl).Host,
                StringComparison.OrdinalIgnoreCase))
            ?? networkErrors.LastOrDefault();

        if (best == null) return ctx;

        return ctx with
        {
            CorrelationId = ctx.CorrelationId ?? best.CorrelationId,
            NetworkStatusCode = ctx.NetworkStatusCode ?? best.StatusCode,
            RawNetworkHeaders = ctx.RawNetworkHeaders ?? best.Headers.ToDictionary(k => k.Key, v => v.Value),
            ErrorMessage = ctx.ErrorMessage ?? best.ErrorMessage
        };
    }

    /// <summary>
    /// Builds a structured BSON payload for <c>logger.LogUiAction("ApplicationError", ...)</c>.
    /// Pass <paramref name="allNetworkErrors"/> (from <c>IPageHelper.GetNetworkErrors()</c>) to embed
    /// a full breakdown of every 4xx/5xx request that fired during the navigation, grouped by status band.
    /// </summary>
    public static BsonDocument BuildLogPayload(AppErrorContext ctx, IReadOnlyList<NetworkCorrelationEntry>? allNetworkErrors = null)
    {
        var doc = new BsonDocument
        {
            { "severity", ctx.Severity.ToString() },
            { "source", ctx.Source.ToString() },
            { "errorUrl", ctx.ErrorUrl },
            { "detectedAt", ctx.DetectedAt.ToString("O") }
        };

        doc["ticketNumber"] = ctx.TicketNumber != null ? (BsonValue)new BsonString(ctx.TicketNumber) : BsonNull.Value;
        doc["correlationId"] = ctx.CorrelationId != null ? (BsonValue)new BsonString(ctx.CorrelationId) : BsonNull.Value;
        doc["errorMessage"] = ctx.ErrorMessage != null ? (BsonValue)new BsonString(ctx.ErrorMessage) : BsonNull.Value;
        doc["networkStatusCode"] = ctx.NetworkStatusCode.HasValue ? (BsonValue)new BsonInt32(ctx.NetworkStatusCode.Value) : BsonNull.Value;

        // Embed ALL 4xx/5xx network errors captured during this navigation, grouped by band
        if (allNetworkErrors is { Count: > 0 })
        {
            doc["networkErrorCount"] = allNetworkErrors.Count;

            var bands = allNetworkErrors
                .GroupBy(e => e.StatusCode >= 500 ? "5xx" : "4xx")
                .ToDictionary(g => g.Key, g => g.OrderBy(e => e.CapturedAt).ToList());

            var networkArray = new BsonArray();
            foreach (var entry in allNetworkErrors.OrderBy(e => e.CapturedAt))
            {
                var entryDoc = new BsonDocument
                {
                    { "band", entry.StatusCode >= 500 ? "5xx" : "4xx" },
                    { "status", entry.StatusCode },
                    { "url", entry.Url },
                    { "correlationId", entry.CorrelationId != null ? (BsonValue)new BsonString(entry.CorrelationId) : BsonNull.Value },
                    { "message", entry.ErrorMessage != null ? (BsonValue)new BsonString(entry.ErrorMessage) : BsonNull.Value },
                    { "capturedAt", entry.CapturedAt.ToString("O") }
                };
                networkArray.Add(entryDoc);
            }
            doc["networkErrors"] = networkArray;

            // Summary counts per band for quick scanning in logs
            var summaryDoc = new BsonDocument();
            foreach (var (band, entries) in bands)
                summaryDoc[band] = entries.Count;
            doc["networkErrorsByBand"] = summaryDoc;
        }

        return doc;
    }

    // ── Kickout page detection ────────────────────────────────────────────────

    /// <summary>
    /// Checks whether the current page is a kickout page and, if so, returns a populated
    /// <see cref="KickoutContext"/> with the error code and message scraped from the DOM.
    /// Returns <c>null</c> when the URL does not contain "kickout" (fast-path exit).
    /// </summary>
    public static async Task<KickoutContext?> TryDetectKickoutAsync(IPage page, IAutomationLogger? logger = null)
    {
        if (!page.Url.Contains("kickout", StringComparison.OrdinalIgnoreCase))
            return null;

        string? errorMessage = null;
        string? errorCode = null;

        try
        {
            var text = await page.Locator(KickoutMessageSelector)
                .TextContentAsync(new() { Timeout = 3000 });

            if (!string.IsNullOrWhiteSpace(text))
            {
                errorMessage = text.Trim();
                var match = KickoutErrorCodeRegex.Match(errorMessage);
                if (match.Success)
                    errorCode = match.Groups[1].Value;
            }
        }
        catch (Exception ex)
        {
            logger?.Debug($"[ErrorPageDetector] Kickout DOM scan failed: {ex.Message}");
        }

        return new KickoutContext
        {
            ErrorCode = errorCode,
            ErrorMessage = errorMessage,
            KickoutUrl = page.Url,
            Severity = AppErrorSeverity.Unexpected
        };
    }

    /// <summary>
    /// Builds a structured BSON payload for <c>logger.LogUiAction("UnexpectedKickout", ...)</c>.
    /// </summary>
    public static BsonDocument BuildKickoutLogPayload(KickoutContext ctx) =>
        new BsonDocument
        {
            { "severity",     ctx.Severity.ToString() },
            { "errorCode",    ctx.ErrorCode   != null ? (BsonValue)new BsonString(ctx.ErrorCode)   : BsonNull.Value },
            { "errorMessage", ctx.ErrorMessage != null ? (BsonValue)new BsonString(ctx.ErrorMessage) : BsonNull.Value },
            { "kickoutUrl",   ctx.KickoutUrl },
            { "detectedAt",   ctx.DetectedAt.ToString("O") }
        };
}
