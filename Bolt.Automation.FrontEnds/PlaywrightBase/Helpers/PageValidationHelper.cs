using System.Text.RegularExpressions;
using Bolt.Automation.Common.Exceptions;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.Common.Models;
using Microsoft.Playwright;

namespace Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;

public class PageValidationHelper(IPage page, IAutomationLogger? logger = null, NetworkCorrelationCapture? networkCapture = null)
{
    // Configuration constants for better maintainability
    private const int MinTimeoutPerAttempt = 5000;
    private const int MaxLoaderTimeout = 15000;
    private const int UrlStabilityCheckDelayMs = 150;
    private const int DefaultTimeout = 30000;
    private const int DefaultMaxRetries = 3;
    private const int DefaultRetryDelayMs = 1000;
    private const int MinStepTimeoutMs = 1000;

    private const string CombinedLoaderSelector = ".loader, .loading-overlay, .spinner";

    private static readonly string[] ValidationSelectors =
    [
        ".error-message", ".field-error", ".validation-error", ".form-error",
        ".text-danger", ".invalid-feedback", ".has-error", ".error",
        ".input-error", ".form-field-error",
        "[aria-invalid='true']", "[role='alert']",
        ".ng-invalid .error-message", ".ng-touched.ng-invalid",
        ".is-invalid + .invalid-feedback", ".was-validated .form-control:invalid + .invalid-feedback",
        ".error-wrapper-error.show-error", ".error-wrapper-error",
        ".required:invalid", ".validation-message", "[data-error]"
    ];

    public async Task ValidatePageReadyAsync(
        string expectedUrlPart,
        string pageName,
        int timeout = DefaultTimeout,
        int maxRetries = DefaultMaxRetries,
        int retryDelayMs = DefaultRetryDelayMs)
    {
        var startTime = DateTime.UtcNow;
        var deadline = startTime.AddMilliseconds(timeout);
        try
        {
            logger?.Trace($"Starting page validation for '{pageName}' (url: '{expectedUrlPart}', timeout: {timeout}ms)");

            await WaitForDomContentLoadedAsync(pageName, RemainingMs(deadline));

            await ValidateUrlAsync(expectedUrlPart, pageName, RemainingMs(deadline), maxRetries, retryDelayMs);

            await WaitForLoadersToDisappearAsync(RemainingMs(deadline));

            LogSuccessfulValidation(pageName, startTime);
        }
        catch (TimeoutException ex)
        {
            LogAndThrowTimeoutException(pageName, expectedUrlPart, startTime, ex);
        }
    }

    private static int RemainingMs(DateTime deadline)
    {
        var remaining = (int)(deadline - DateTime.UtcNow).TotalMilliseconds;
        return remaining < MinStepTimeoutMs ? MinStepTimeoutMs : remaining;
    }

    /// <summary>Gets all validation error messages currently displayed on the page</summary>
    public async Task<List<string>> GetValidationMessagesAsync(string[]? customSelectors = null)
    {
        using var step = logger?.StartStep("Get validation messages");
        var selectorsToUse = customSelectors ?? ValidationSelectors;

        try
        {
            // Single JS roundtrip instead of N+1 Playwright IPC calls per selector.
            var found = await page.EvaluateAsync<string[]>(@"
                (selectors) => {
                    const seen = new Set();
                    const out = [];
                    for (const sel of selectors) {
                        let nodes;
                        try { nodes = document.querySelectorAll(sel); } catch { continue; }
                        nodes.forEach(el => {
                            if (!(el.offsetParent || el.getClientRects().length)) return;
                            const t = (el.textContent || '').trim();
                            if (t && !seen.has(t)) { seen.add(t); out.push(t); }
                        });
                    }
                    return out;
                }", selectorsToUse);

            var distinctMessages = found.ToList();
            logger?.Info($"Found {distinctMessages.Count} validation messages");
            step?.Complete($"Found {distinctMessages.Count} messages");
            return distinctMessages;
        }
        catch (Exception ex)
        {
            logger?.LogException(ex, "Failed to get validation messages");
            return [];
        }
    }

    /// <summary>Returns the set of app-control element ids that are currently invalid + visible
    /// (e.g. "PolicyData.YearsWithPriorCarrierHome"). Used by the form-fill retry pass to
    /// detect ajax-revealed required fields that were skipped on the first pass.</summary>
    public async Task<HashSet<string>> GetInvalidFieldIdsAsync()
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        try
        {
            // Find each invalid form element and walk up to its enclosing <app-control id="...">.
            // Using JS evaluation here is dramatically faster than per-element Playwright calls.
            var found = await page.EvaluateAsync<string[]>(@"
                () => {
                    const sel = '.ng-invalid, [aria-invalid=""true""], .has-error, .error';
                    const ids = new Set();
                    document.querySelectorAll(sel).forEach(el => {
                        // skip hidden
                        if (!(el.offsetParent || el.getClientRects().length)) return;
                        const ctrl = el.closest('app-control[id]');
                        if (ctrl && ctrl.id) ids.add(ctrl.id);
                    });
                    return Array.from(ids);
                }");
            foreach (var id in found) ids.Add(id);
        }
        catch (Exception ex)
        {
            logger?.Debug($"GetInvalidFieldIdsAsync failed: {ex.Message}");
        }
        return ids;
    }

    /// <summary>Gets the validation error message for a specific field by its locator name (e.g. "PreviousAddress.AddressLine1").
    /// Targets the standard app error pattern: #FieldName_error p inside .error-wrapper.show</summary>
    public async Task<string?> GetFieldValidationMessageAsync(string fieldLocatorName)
    {
        var escapedId = fieldLocatorName.Replace(".", "\\.");
        var selector = $"#{escapedId}_error.show p";
        logger?.Debug($"Checking field validation message with selector '{selector}'");
        try
        {
            var element = page.Locator(selector);
            var text = await element.TextContentAsync(new() { Timeout = 3000 });
            return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        }
        catch
        {
            return null;
        }
    }

    #region Private Helper Methods

    /// <summary>Waits for DOM content to be loaded</summary>
    private async Task WaitForDomContentLoadedAsync(string pageName, int timeout)
    {
        try
        {
            await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded, new() { Timeout = timeout });
        }
        catch (TimeoutException ex)
        {
            logger?.Error($"DOM load timeout for '{pageName}' after {timeout}ms. Current URL: '{page.Url}'");
            throw new TimeoutException($"Page '{pageName}' DOM did not load within {timeout}ms. Current URL: '{page.Url}'", ex);
        }
    }

    /// <summary>Validates the current URL contains the expected part with retry mechanism</summary>
    private async Task ValidateUrlAsync(string expectedUrlPart, string pageName, int timeout, int maxRetries, int retryDelayMs)
    {
        var parts = SplitExpectedUrlParts(expectedUrlPart);

        if (await IsUrlStableAndCorrectAsync(parts, pageName))
        {
            return;
        }

        var urlRegex = BuildUrlRegex(parts);
        var attempt = 1;
        Exception? lastException = null;
        var timeoutPerAttempt = Math.Max(timeout / maxRetries, MinTimeoutPerAttempt);

        while (attempt <= maxRetries)
        {
            try
            {
                await page.WaitForURLAsync(urlRegex, new() { Timeout = timeoutPerAttempt });

                logger?.Info($"URL validated: '{pageName}'" + (attempt > 1 ? $" (attempt {attempt})" : ""));
                return;
            }
            catch (TimeoutException ex)
            {
                lastException = ex;
                var currentUrl = page.Url;

                // Check if URL actually matches despite timeout (race condition mitigation)
                if (UrlContainsExpectedParts(currentUrl, parts))
                {
                    logger?.Debug($"URL matches '{expectedUrlPart}' despite timeout - proceeding");
                    return;
                }

                if (attempt < maxRetries)
                {
                    logger?.Debug($"URL not yet '{expectedUrlPart}' (attempt {attempt}/{maxRetries}). Current: '{currentUrl}'. Retrying in {retryDelayMs}ms...");
                    await Task.Delay(retryDelayMs);
                }
                attempt++;
            }
        }

        // Before throwing a generic timeout, check if we landed on an app error page.
        // Detection order: URL → DOM → network response body (each layer fills in what the previous missed).
        var finalUrl = page.Url;
        if (finalUrl.Contains("/error", StringComparison.OrdinalIgnoreCase))
        {
            // 1. URL detection (fast, synchronous)
            var errorCtx = ErrorPageDetector.TryDetectFromUrl(finalUrl) ?? new AppErrorContext
            {
                ErrorUrl = finalUrl,
                Source = AppErrorSource.UrlPath,
                Severity = AppErrorSeverity.Unexpected
            };

            // 2. DOM scan — fills in ticket/correlation/message the URL didn't have
            var domCtx = await ErrorPageDetector.TryDetectFromDomAsync(page, logger);
            if (domCtx != null)
            {
                errorCtx = errorCtx with
                {
                    TicketNumber = errorCtx.TicketNumber ?? domCtx.TicketNumber,
                    CorrelationId = errorCtx.CorrelationId ?? domCtx.CorrelationId,
                    ErrorMessage = errorCtx.ErrorMessage ?? domCtx.ErrorMessage,
                    Source = errorCtx.TicketNumber == null && domCtx.TicketNumber != null
                        ? AppErrorSource.UiDomText : errorCtx.Source
                };
            }

            // 3. Network capture — fills in correlation id and error message from response body
            //    when the UI surface shows nothing (e.g. /error/technical with a 500 behind it),
            //    and attaches the FULL list of failing requests so an earlier failure that broke the
            //    flow (e.g. extractToken 500) is not hidden behind the request that redirected here.
            var networkHistory = networkCapture?.History ?? [];
            if (networkCapture != null)
            {
                errorCtx = ErrorPageDetector.MergeWithNetworkErrors(errorCtx, networkHistory);
                errorCtx = errorCtx with
                {
                    NetworkErrors = networkHistory
                        .OrderBy(e => e.CapturedAt)
                        .Select(e => new NetworkErrorSummary(e.StatusCode, e.Url, e.CorrelationId, e.ErrorMessage))
                        .ToList()
                };
            }

            errorCtx = errorCtx with { Severity = AppErrorSeverity.Unexpected };
            logger?.LogUiAction("ApplicationError", finalUrl, ErrorPageDetector.BuildLogPayload(errorCtx, networkHistory));
            throw new ApplicationTicketException(errorCtx, lastException);
        }

        // Before throwing a generic timeout, check if we landed on an unexpected kickout page.
        if (finalUrl.Contains("kickout", StringComparison.OrdinalIgnoreCase))
        {
            var kickoutCtx = await ErrorPageDetector.TryDetectKickoutAsync(page, logger)
                ?? new KickoutContext { KickoutUrl = finalUrl, Severity = AppErrorSeverity.Unexpected };

            logger?.LogUiAction("UnexpectedKickout", finalUrl, ErrorPageDetector.BuildKickoutLogPayload(kickoutCtx));
            throw new UnexpectedKickoutException(kickoutCtx, lastException);
        }

        throw new TimeoutException(
            $"Navigation failed for '{pageName}' after {maxRetries} attempts (total {timeout}ms). Expected URL containing '{expectedUrlPart}', but got '{page.Url}'.",
            lastException);
    }

    /// <summary>Checks if the URL already contains the expected parts and remains stable.
    /// Skips the stability delay when the URL hasn't changed during a tight re-read — the
    /// common case after navigation has already settled.</summary>
    private async Task<bool> IsUrlStableAndCorrectAsync(string[] parts, string pageName)
    {
        var urlBefore = page.Url;
        if (!UrlContainsExpectedParts(urlBefore, parts))
        {
            return false;
        }

        // Fast path: if the URL is unchanged across an immediate re-read, skip the blocking delay.
        if (page.Url == urlBefore)
        {
            logger?.Trace($"Already on correct page '{pageName}'. URL: '{urlBefore}'");
            return true;
        }

        await Task.Delay(UrlStabilityCheckDelayMs);

        if (UrlContainsExpectedParts(page.Url, parts))
        {
            logger?.Trace($"Already on correct page '{pageName}'. URL: '{page.Url}'");
            return true;
        }

        logger?.Debug($"URL changed during stability check for '{pageName}' - proceeding with full validation");
        return false;
    }

    private static bool UrlContainsExpectedParts(string url, string[] parts)
    {
        foreach (var part in parts)
        {
            if (!url.Contains(part, StringComparison.OrdinalIgnoreCase)) return false;
        }
        return true;
    }

    private static Regex BuildUrlRegex(string[] parts)
    {
        var lookaheads = string.Join("", parts.Select(part => $"(?=.*{Regex.Escape(part)})"));
        return new Regex($".*{lookaheads}.*", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    }

    private static string[] SplitExpectedUrlParts(string expectedUrlPart)
    {
        return expectedUrlPart.Split(['|', ',', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }


    /// <summary>Waits for any loader element (combined selector) to disappear.
    /// One CountAsync + at most one WaitForAsync replaces the previous per-selector loop.</summary>
    private async Task WaitForLoadersToDisappearAsync(int timeout)
    {
        var loaderTimeout = Math.Min(timeout / 2, MaxLoaderTimeout);
        try
        {
            var loader = page.Locator(CombinedLoaderSelector);
            var count = await loader.CountAsync();

            if (count > 0)
            {
                logger?.Debug($"Waiting for loader '{CombinedLoaderSelector}' to disappear (found {count} instances, timeout: {loaderTimeout}ms)");
                foreach (var l in await loader.AllAsync())
                    await l.WaitForAsync(new() { State = WaitForSelectorState.Hidden, Timeout = loaderTimeout });
                logger?.Debug("Loader disappeared");
            }
        }
        catch (TimeoutException)
        {
            logger?.Debug($"Loader did not disappear within {loaderTimeout}ms - continuing anyway");
        }
        catch (Exception ex)
        {
            logger?.Debug($"Error waiting for loader: {ex.Message} - continuing anyway");
        }
    }

    /// <summary>Logs successful page validation</summary>
    private void LogSuccessfulValidation(string pageName, DateTime startTime)
    {
        var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
        logger?.LogUiAction("PageValidation", pageName, new MongoDB.Bson.BsonDocument
        {
            { "status", "Success" },
            { "pageLoadMs", (long)elapsed },
            { "url", page.Url }
        });
        logger?.Info($"Page ready: {pageName} ({elapsed:F0}ms)");
    }

    /// <summary>Logs and throws timeout exception. Note: only DOM-load and URL-validation
    /// timeouts reach here. The loader wait swallows its own timeout internally
    /// (see WaitForLoadersToDisappearAsync), so re-throwing here will not cause a slow
    /// rates loader to fail a test.</summary>
    private void LogAndThrowTimeoutException(string pageName, string expectedUrlPart, DateTime startTime, TimeoutException ex)
    {
        var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
        logger?.Error($"Page validation timeout for '{pageName}' after {elapsed:F0}ms. Expected: '{expectedUrlPart}', Actual: '{page.Url}'");
                 
        throw new TimeoutException(
            $"Page validation timeout for '{pageName}' after {elapsed:F0}ms. Expected URL containing '{expectedUrlPart}', Actual: '{page.Url}'",
            ex);
         
    }

    #endregion
}
