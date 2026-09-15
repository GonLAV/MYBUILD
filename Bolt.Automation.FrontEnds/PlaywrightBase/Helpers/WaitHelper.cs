using System.Text.RegularExpressions;
using Bolt.Automation.Common.Exceptions;
using Microsoft.Playwright;

namespace Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;

public class WaitHelper(IPage page, Common.Logging.Core.IAutomationLogger? logger)
{
    private MongoDB.Bson.BsonDocument CreateUiActionPayload(
        string action,
        string status,
        long durationMs,
        Dictionary<string, object>? additionalFields = null,
        string? error = null)
    {
        var payload = new MongoDB.Bson.BsonDocument
        {
            { "action", action },
            { "status", status },
            { "durationMs", durationMs },
            { "pageUrl", page.Url }
        };

        if (additionalFields != null)
        {
            foreach (var field in additionalFields)
            {
                payload[field.Key] = MongoDB.Bson.BsonValue.Create(field.Value);
            }
        }

        if (error != null)
        {
            payload["error"] = error;
        }

        return payload;
    }

    public async Task<ILocator?> WaitForElementAsync(ILocator locator, int timeout = 10000, bool waitForVisibility = false, int retries = 3)
    {
        var waitState = waitForVisibility ? WaitForSelectorState.Visible : WaitForSelectorState.Attached;
        var sw = System.Diagnostics.Stopwatch.StartNew();

        for (int attempt = 0; attempt < retries; attempt++)
        {
            try
            {
                await locator.WaitForAsync(new LocatorWaitForOptions { State = waitState, Timeout = timeout });
                sw.Stop();

                var payload = CreateUiActionPayload("WaitForElement", "Success", sw.ElapsedMilliseconds, new()
                {
                    { "waitState", waitState.ToString() },
                    { "timeout", timeout },
                    { "retries", retries },
                    { "attempts", attempt + 1 }
                });
                logger?.LogUiAction("WaitForElement", $"state: {waitState}", payload);
                return locator;
            }
            catch (TimeoutException ex) when (attempt < retries - 1)
            {
                logger?.Debug($"Element not found on attempt {attempt + 1}/{retries}, retrying...");
                await Task.Delay(1000);
            }
            catch (TimeoutException ex) when (attempt == retries - 1)
            {
                sw.Stop();
                var payload = CreateUiActionPayload("WaitForElement", "Failed", sw.ElapsedMilliseconds, new()
                {
                    { "waitState", waitState.ToString() },
                    { "timeout", timeout },
                    { "retries", retries },
                    { "attempts", attempt + 1 }
                }, ex.Message);
                logger?.LogUiAction("WaitForElement", $"state: {waitState}", payload);
                throw new PageElementException("element", $"not found after {retries} attempts", ex);
            }
        }

        throw new PageElementException("element", $"not found after {retries} attempts");
    }

    public async Task<IReadOnlyList<string>> WaitForElementsValuesAsync(ILocator elementsLocator, int timeout = 10000)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            await elementsLocator.First.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Attached, Timeout = timeout });

            var elements = await elementsLocator.AllAsync();
            if (elements.Count == 0)
            {
                sw.Stop();
                var payload = CreateUiActionPayload("WaitForElementsValues", "Failed", sw.ElapsedMilliseconds, 
                    new() { { "timeout", timeout }, { "elementsFound", 0 } }, "No elements found");
                logger?.LogUiAction("WaitForElementsValues", "locator", payload);
                throw new PageElementException("elements", "no elements found matching the locator");
            }

            var values = new List<string>();
            foreach (var element in elements)
                values.Add(await element.EvaluateAsync<string>("el => el.value"));

            sw.Stop();
            var successPayload = CreateUiActionPayload("WaitForElementsValues", "Success", sw.ElapsedMilliseconds, 
                new() { { "timeout", timeout }, { "elementsFound", values.Count } });
            logger?.LogUiAction("WaitForElementsValues", $"{values.Count} element(s)", successPayload);
            return values;
        }
        catch (Exception ex) when (ex.Message != "No elements found matching the locator")
        {
            sw.Stop();
            var payload = CreateUiActionPayload("WaitForElementsValues", "Failed", sw.ElapsedMilliseconds, 
                new() { { "timeout", timeout } }, ex.Message);
            logger?.LogUiAction("WaitForElementsValues", "locator", payload);
            throw;
        }
    }


    public async Task<bool> WaitForNavigationOrUrlContainsAsync(string urlPart, int timeout = 15000, bool exactMatch = false)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // When Chrome navigates to an unresolvable URL (ERR_NAME_NOT_RESOLVED), page.Url
            // becomes chrome-error://chromewebdata/ while the intended URL is stored in Chrome's
            // navigation history. Resolve the effective URL via CDP in that case.
            var currentUrl = page.Url;
            var effectiveUrl = currentUrl.StartsWith("chrome-error://")
                ? await GetLastNavigationUrlViaCdpAsync() ?? currentUrl
                : currentUrl;

            if (effectiveUrl != currentUrl)
                logger?.Debug($"chrome-error detected � CDP resolved URL: [{effectiveUrl}]");

            if (UrlMatches(effectiveUrl, urlPart, exactMatch))
            {
                sw.Stop();
                var payload = CreateUiActionPayload("WaitForNavigation", "Success", sw.ElapsedMilliseconds, new()
                {
                    { "urlPart", urlPart },
                    { "exactMatch", exactMatch },
                    { "resolvedVia", effectiveUrl != currentUrl ? "cdpNavigationHistory" : "currentUrl" }
                });
                logger?.LogUiAction("WaitForNavigation", urlPart, payload);
                return true;
            }

            var urlPattern = exactMatch
                ? new Regex($"^{Regex.Escape(urlPart)}$", RegexOptions.IgnoreCase)
                : new Regex($".*{Regex.Escape(urlPart)}.*", RegexOptions.IgnoreCase);

            await page.WaitForURLAsync(urlPattern, new PageWaitForURLOptions { Timeout = timeout });

            sw.Stop();
            var successPayload = CreateUiActionPayload("WaitForNavigation", "Success", sw.ElapsedMilliseconds, new()
            {
                { "urlPart", urlPart },
                { "exactMatch", exactMatch },
                { "timeout", timeout },
                { "resolvedVia", "urlMatch" }
            });
            logger?.LogUiAction("WaitForNavigation", urlPart, successPayload);
            return true;
        }
        catch (TimeoutException ex)
        {
            sw.Stop();
            var matchType = exactMatch ? "exact match" : "to contain";
            var payload = CreateUiActionPayload("WaitForNavigation", "Failed", sw.ElapsedMilliseconds, new()
            {
                { "urlPart", urlPart },
                { "exactMatch", exactMatch },
                { "timeout", timeout }
            }, $"Timeout waiting for URL {matchType} '{urlPart}'");
            logger?.LogUiAction("WaitForNavigation", urlPart, payload);
            throw new NavigationException($"Timeout waiting for URL {matchType} '{urlPart}' within {timeout}ms. Current: '{page.Url}'", ex);
        }
    }

    /// <summary>
    /// Waits until the URL contains ANY of <paramref name="urlParts"/> and returns the fragment
    /// that matched.
    /// </summary>
    /// <remarks>
    /// For a step the app renders conditionally, where the caller cannot know in advance which of
    /// several pages it will land on. Returning the matched fragment — rather than a bool — is what
    /// lets the caller pick the right page object without reading <c>page.Url</c> itself.
    /// </remarks>
    public async Task<string> WaitForNavigationOrUrlContainsAsync(IReadOnlyList<string> urlParts, int timeout = 15000)
    {
        if (urlParts is null || urlParts.Count == 0)
            throw new ArgumentException("At least one URL fragment is required.", nameof(urlParts));

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var candidates = string.Join(" | ", urlParts);

        try
        {
            // Same chrome-error/CDP resolution as the single-fragment overload above.
            var currentUrl = page.Url;
            var effectiveUrl = currentUrl.StartsWith("chrome-error://")
                ? await GetLastNavigationUrlViaCdpAsync() ?? currentUrl
                : currentUrl;

            var alreadyThere = FirstMatchingPart(effectiveUrl, urlParts);
            if (alreadyThere is not null)
            {
                sw.Stop();
                logger?.LogUiAction("WaitForNavigationAny", candidates, CreateUiActionPayload(
                    "WaitForNavigationAny", "Success", sw.ElapsedMilliseconds, new()
                    {
                        { "urlParts", candidates },
                        { "matched", alreadyThere },
                        { "resolvedVia", effectiveUrl != currentUrl ? "cdpNavigationHistory" : "currentUrl" }
                    }));
                return alreadyThere;
            }

            var urlPattern = new Regex(
                string.Join("|", urlParts.Select(part => $".*{Regex.Escape(part)}.*")),
                RegexOptions.IgnoreCase);

            await page.WaitForURLAsync(urlPattern, new PageWaitForURLOptions { Timeout = timeout });

            // WaitForURLAsync only proves the alternation matched, not which branch did.
            var matched = FirstMatchingPart(page.Url, urlParts)
                ?? throw new NavigationException(
                    $"URL '{page.Url}' matched the wait pattern but none of '{candidates}' on re-check.");

            sw.Stop();
            logger?.LogUiAction("WaitForNavigationAny", candidates, CreateUiActionPayload(
                "WaitForNavigationAny", "Success", sw.ElapsedMilliseconds, new()
                {
                    { "urlParts", candidates },
                    { "matched", matched },
                    { "timeout", timeout },
                    { "resolvedVia", "urlMatch" }
                }));
            return matched;
        }
        catch (TimeoutException ex)
        {
            sw.Stop();
            logger?.LogUiAction("WaitForNavigationAny", candidates, CreateUiActionPayload(
                "WaitForNavigationAny", "Failed", sw.ElapsedMilliseconds, new()
                {
                    { "urlParts", candidates },
                    { "timeout", timeout }
                }, $"Timeout waiting for URL to contain any of '{candidates}'"));
            throw new NavigationException(
                $"Timeout waiting for URL to contain any of '{candidates}' within {timeout}ms. Current: '{page.Url}'", ex);
        }
    }

    private static string? FirstMatchingPart(string url, IReadOnlyList<string> urlParts) =>
        urlParts.FirstOrDefault(part => url.Contains(part, StringComparison.OrdinalIgnoreCase));

    private async Task<string?> GetLastNavigationUrlViaCdpAsync()
    {
        ICDPSession? cdpSession = null;
        try
        {
            cdpSession = await page.Context.NewCDPSessionAsync(page);
            var historyRaw = await cdpSession.SendAsync("Page.getNavigationHistory");

            if (historyRaw.HasValue &&
                historyRaw.Value.TryGetProperty("currentIndex", out var currentIndexEl) &&
                historyRaw.Value.TryGetProperty("entries", out var entriesEl))
            {
                var currentIndex = currentIndexEl.GetInt32();
                var entries = entriesEl.EnumerateArray().ToList();

                if (currentIndex >= 0 && currentIndex < entries.Count)
                {
                    var entry = entries[currentIndex];
                    if (entry.TryGetProperty("url", out var urlProp))
                        return urlProp.GetString();
                }
            }
        }
        catch (Exception ex)
        {
            logger?.Debug($"CDP navigation history lookup failed: {ex.Message}");
        }
        finally
        {
            if (cdpSession != null)
                await cdpSession.DetachAsync();
        }
        return null;
    }

    private static bool UrlMatches(string url, string urlPart, bool exactMatch) =>
        exactMatch
            ? url.Equals(urlPart, StringComparison.OrdinalIgnoreCase)
            : url.Contains(urlPart, StringComparison.OrdinalIgnoreCase);


    private const int DisappearPollIntervalMs = 100;

    /// <summary>
    /// Waits for <paramref name="element"/> to go away - either detached from the DOM or hidden.
    /// </summary>
    /// <remarks>
    /// <paramref name="timeout"/> is the budget for the WHOLE call. The fast detach wait and the
    /// polling fallback share it, so a 30s budget cannot take 60s.
    /// </remarks>
    public async Task<bool> WaitForElementToDisappearAsync(ILocator element, int timeout = 10000, int initialRetries = 3, int retryDelay = 500)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var description = DescribeLocator(element);

        try
        {
            // Check if element exists after retries
            for (int attempt = 0; attempt < initialRetries; attempt++)
            {
                if (await element.CountAsync() > 0) break;
                if (attempt < initialRetries - 1) await Task.Delay(retryDelay);
            }

            if (await element.CountAsync() == 0)
            {
                sw.Stop();
                var payload = CreateUiActionPayload("WaitForElementToDisappear", "Success", sw.ElapsedMilliseconds,
                    new() { { "timeout", timeout }, { "alreadyGone", true } });
                logger?.LogUiAction("WaitForElementToDisappear", description, payload);
                return true;
            }

            // Fast path. Only ever waits for what is left of the budget - passing the full `timeout`
            // here is what used to double the real wall time. A zero budget is skipped outright,
            // because Playwright reads Timeout = 0 as "wait forever".
            var detachBudget = RemainingBudgetMs(timeout, sw);
            if (detachBudget > 0)
            {
                try
                {
                    await element.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Detached, Timeout = detachBudget });
                    sw.Stop();
                    var payload = CreateUiActionPayload("WaitForElementToDisappear", "Success", sw.ElapsedMilliseconds,
                        new() { { "timeout", timeout } });
                    logger?.LogUiAction("WaitForElementToDisappear", description, payload);
                    return true;
                }
                catch (Exception ex) when (ex is PlaywrightException || ex is TimeoutException || ex is OperationCanceledException)
                {
                    // Not detached. It may merely be hidden, which a Detached wait can never
                    // satisfy, so fall through and poll for the rest of the budget.
                    logger?.Debug($"'{description}' did not detach within {detachBudget}ms; polling for a hidden state.");
                }
            }

            while (RemainingBudgetMs(timeout, sw) > 0)
            {
                if (await element.CountAsync() == 0)
                {
                    sw.Stop();
                    var payload = CreateUiActionPayload("WaitForElementToDisappear", "Success", sw.ElapsedMilliseconds,
                        new() { { "timeout", timeout }, { "method", "polling" } });
                    logger?.LogUiAction("WaitForElementToDisappear", description, payload);
                    return true;
                }

                bool stillVisible;
                try
                {
                    stillVisible = await element.IsVisibleAsync();
                }
                catch (PlaywrightException) when (!page.IsClosed)
                {
                    // The element was torn out from under the check, which is exactly what we are
                    // waiting for. A CLOSED page is not that, so it is left to propagate rather
                    // than being reported as a success.
                    sw.Stop();
                    var payload = CreateUiActionPayload("WaitForElementToDisappear", "Success", sw.ElapsedMilliseconds,
                        new() { { "timeout", timeout }, { "method", "detached mid-check" } });
                    logger?.LogUiAction("WaitForElementToDisappear", description, payload);
                    return true;
                }

                if (!stillVisible)
                {
                    sw.Stop();
                    var payload = CreateUiActionPayload("WaitForElementToDisappear", "Success", sw.ElapsedMilliseconds,
                        new() { { "timeout", timeout }, { "method", "not visible" } });
                    logger?.LogUiAction("WaitForElementToDisappear", description, payload);
                    return true;
                }

                // Deliberately NOT cancellable. A cancelled delay threw TaskCanceledException,
                // which escaped this loop and surfaced as the useless "A task was canceled."
                // instead of the real "remained visible" diagnostic below.
                await Task.Delay(DisappearPollIntervalMs);
            }

            sw.Stop();
            var failure = $"remained visible after {sw.ElapsedMilliseconds}ms (budget {timeout}ms). Current URL: '{page.Url}'.";
            var errorPayload = CreateUiActionPayload("WaitForElementToDisappear", "Failed", sw.ElapsedMilliseconds,
                new() { { "timeout", timeout } }, failure);
            logger?.LogUiAction("WaitForElementToDisappear", description, errorPayload);
            throw new PageElementException(description, failure);
        }
        catch (PageElementException)
        {
            // Already the specific, actionable failure - re-wrapping it here is what buried the
            // "remained visible" message behind the generic one.
            throw;
        }
        catch (Exception ex)
        {
            sw.Stop();
            var payload = CreateUiActionPayload("WaitForElementToDisappear", "Failed", sw.ElapsedMilliseconds,
                new() { { "timeout", timeout } }, ex.Message);
            logger?.LogUiAction("WaitForElementToDisappear", description, payload);
            throw new PageElementException(description,
                $"error while waiting to disappear after {sw.ElapsedMilliseconds}ms (budget {timeout}ms). Current URL: '{page.Url}'. {ex.Message}", ex);
        }
    }

    /// <summary>How much of <paramref name="timeout"/> is left, never negative.</summary>
    private static int RemainingBudgetMs(int timeout, System.Diagnostics.Stopwatch sw) =>
        (int)Math.Max(0L, timeout - sw.ElapsedMilliseconds);

    /// <summary>
    /// Best-effort readable locator for error messages, so a failure names the element it was
    /// waiting on instead of the literal "element".
    /// </summary>
    private static string DescribeLocator(ILocator element)
    {
        try
        {
            var text = element.ToString();
            return string.IsNullOrWhiteSpace(text) ? "element" : text;
        }
        catch
        {
            return "element";
        }
    }

    public async Task<IResponse> WaitForApiResponseAsync(
    Func<Task> action,
    string endpoint,
    int expectedStatus = 200,
    int timeoutMs = 30000,
    Func<IResponse, bool>? predicate = null)
    {
        if (action == null)
            throw new ArgumentNullException(nameof(action));
        if (string.IsNullOrWhiteSpace(endpoint))
            throw new ArgumentException("Endpoint cannot be null or empty.", nameof(endpoint));

        var sw = System.Diagnostics.Stopwatch.StartNew();

        // Every response seen for this endpoint, so a timeout can distinguish
        // "the request was never sent" from "it came back and we rejected it".
        var observed = new List<string>();
        var observedLock = new object();
        void OnResponse(object? _, IResponse r)
        {
            if (!r.Url.Contains(endpoint, StringComparison.OrdinalIgnoreCase))
                return;

            // Playwright raises Response on its own event loop, so guard the list.
            lock (observedLock)
                observed.Add($"{r.Request.Method} {r.Status} {r.Url}");
        }

        page.Response += OnResponse;
        try
        {
            IResponse response;
            try
            {
                response = await page.RunAndWaitForResponseAsync(
                    async () => await action(),
                    r =>
                        r.Url.Contains(endpoint, StringComparison.OrdinalIgnoreCase) &&
                        // A CORS preflight and a redirect hop are not the endpoint's answer;
                        // matching either would judge the status of the wrong response.
                        !string.Equals(r.Request.Method, "OPTIONS", StringComparison.OrdinalIgnoreCase) &&
                        (r.Status < 300 || r.Status >= 400) &&
                        (predicate == null || predicate(r)),
                    new PageRunAndWaitForResponseOptions { Timeout = timeoutMs });
            }
            catch (TimeoutException ex)
            {
                sw.Stop();
                var seen = DescribeObservedResponses(observed, observedLock);
                var timeoutPayload = CreateUiActionPayload("WaitForApiResponse", "Failed", sw.ElapsedMilliseconds, new()
            {
                { "endpoint", endpoint },
                { "expectedStatus", expectedStatus },
                { "timeout", timeoutMs },
                { "observedResponses", seen }
            }, ex.Message);
                logger?.LogUiAction("WaitForApiResponse", endpoint, timeoutPayload);

                throw new TimeoutException(
                    $"Timeout waiting for response containing '{endpoint}' within {timeoutMs}ms. {seen}", ex);
            }

            // Status is deliberately NOT in the match predicate: folding it in makes a 500 look
            // identical to no response at all — full timeout, error detail discarded.
            if (response.Status != expectedStatus)
            {
                sw.Stop();
                var detail = await DescribeFailedResponseAsync(response);
                var statusPayload = CreateUiActionPayload("WaitForApiResponse", "Failed", sw.ElapsedMilliseconds, new()
            {
                { "endpoint", endpoint },
                { "expectedStatus", expectedStatus },
                { "timeout", timeoutMs },
                { "responseUrl", response.Url },
                { "responseStatus", response.Status }
            }, detail);
                logger?.LogUiAction("WaitForApiResponse", endpoint, statusPayload);

                throw new ApiResponseException(
                    $"'{endpoint}' returned {response.Status}, expected {expectedStatus}. {detail}");
            }

            // RunAndWaitForResponseAsync fires at the network layer. Angular's
            // change detection (Zone.js → ApplicationRef.tick()) runs as a
            // subsequent macrotask. This setTimeout(100) queues a macrotask that
            // executes only after all pending microtasks — including Angular's
            // full subscriber + CD cycle — have flushed, ensuring the DOM
            // reflects the new data before we return.
            await page.EvaluateAsync("() => new Promise(r => setTimeout(r, 100))");

            sw.Stop();
            var payload = CreateUiActionPayload("WaitForApiResponse", "Success", sw.ElapsedMilliseconds, new()
        {
            { "endpoint", endpoint },
            { "expectedStatus", expectedStatus },
            { "timeout", timeoutMs },
            { "responseUrl", response.Url },
            { "responseStatus", response.Status }
        });
            logger?.LogUiAction("WaitForApiResponse", endpoint, payload);

            return response;
        }
        finally
        {
            page.Response -= OnResponse;
        }
    }

    private static string DescribeObservedResponses(List<string> observed, object observedLock)
    {
        string[] seen;
        lock (observedLock)
            seen = [.. observed];

        return seen.Length == 0
            ? "No response for that endpoint reached the browser at all — the request was most likely never sent."
            : $"Responses seen for that endpoint ({seen.Length}): {string.Join("; ", seen)}.";
    }

    /// <summary>
    /// Builds the diagnostic tail for a response that arrived with the wrong status: its URL, the
    /// correlation id from its headers, and a truncated body. The body is what carries the server's
    /// own error handle (e.g. a ticket id), which is the only thing that makes a 500 actionable.
    /// </summary>
    private static async Task<string> DescribeFailedResponseAsync(IResponse response)
    {
        const int maxBodyChars = 500;
        var parts = new List<string> { $"URL: {response.Url}" };

        try
        {
            var correlationId = NetworkCorrelationCapture.ExtractCorrelationId(await response.AllHeadersAsync());
            if (!string.IsNullOrWhiteSpace(correlationId))
                parts.Add($"CorrelationId: {correlationId}");
        }
        catch
        {
            // Headers unavailable — the status is already captured, which is the point.
        }

        try
        {
            var body = (await response.TextAsync())?.Trim();
            if (!string.IsNullOrWhiteSpace(body))
                parts.Add($"Body: {(body.Length > maxBodyChars ? body[..maxBodyChars] + "…[truncated]" : body)}");
        }
        catch
        {
            // Body already consumed, binary, or a download — not worth failing the report over.
        }

        return string.Join(", ", parts);
    }
}
