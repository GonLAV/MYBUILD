using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.Common.Logging.Extentions;
using Microsoft.Playwright;

namespace Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;

public class NavigationHelper
{
    // Best-effort cap for the post-back NetworkIdle wait (see WaitForDestinationReadyAsync). Short on
    // purpose: DOMContentLoaded is the load-bearing signal; NetworkIdle is a bonus we don't block on.
    private const int NetworkIdleBestEffortTimeoutMs = 10_000;

    private readonly IPage _page;
    private readonly IAutomationLogger? _logger;

    public NavigationHelper(IPage page, IAutomationLogger? logger = null)
    {
        _page = page;
        _logger = logger;
    }

    public async Task ClickBrowserBackButton(int stabilityDelayMs = 500)
    {
        try
        {
            try
            {
                await _page.GoBackAsync();
            }
            catch (PlaywrightException ex) when (IsBenignBackNavigationAbort(ex))
            {
                // Going back FROM a cross-origin carrier-bridge page abandons the carrier page's
                // in-flight load, so Playwright's GoBackAsync (which waits for the previous entry to
                // reach "load") surfaces "net::ERR_ABORTED; maybe frame was detached?". The back entry
                // (the Bolt kick-out page) still commits and renders — confirmed in artifacts — so this
                // abort is benign. Swallow it and fall through to confirm the destination DOM below;
                // the caller's page-ready validation is the real landing check.
                _logger?.Warning($"Browser back aborted the outgoing cross-origin page ({ex.Message}) — the back entry still committed, continuing.");
            }

            // Confirm the destination is usable regardless of whether GoBack returned an IResponse
            // (a same-document / cached back entry yields a null response but is still a real landing).
            await WaitForDestinationReadyAsync();

            if (stabilityDelayMs > 0)
            {
                await Task.Delay(stabilityDelayMs);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogException(ex, "Failed to navigate browser back");
            throw;
        }
    }

    /// <summary>
    /// Best-effort wait for the page reached by a browser-back to be ready to interact with. Waits for
    /// DOMContentLoaded, then NetworkIdle as a bonus. Both are tolerant: DOMContentLoaded can itself
    /// abort during a cross-origin back-transition (same root cause as the GoBack abort), and NetworkIdle
    /// routinely times out when the destination still has ongoing activity (e.g. returning from a carrier
    /// bridge). DOMContentLoaded reaching (or having already reached) is sufficient to continue.
    /// </summary>
    private async Task WaitForDestinationReadyAsync()
    {
        try
        {
            await _page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        }
        catch (PlaywrightException ex) when (IsBenignBackNavigationAbort(ex))
        {
            _logger?.Warning($"DOMContentLoaded aborted during browser back ({ex.Message}) — destination already committed, continuing.");
        }

        try
        {
            // Bounded, best-effort: NetworkIdle is a nice-to-have on top of DOMContentLoaded. Cap the
            // wait so a destination with steady background traffic (SPA polling, carrier-bridge return)
            // doesn't stall the caller for the full default timeout before we give up on it.
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle,
                new PageWaitForLoadStateOptions { Timeout = NetworkIdleBestEffortTimeoutMs });
        }
        catch (TimeoutException)
        {
            // NetworkIdle may time out when the destination page has ongoing network activity
            // (e.g. returning from a carrier bridge page). DOMContentLoaded is sufficient.
            _logger?.Warning("NetworkIdle timed out after browser back — page is loaded, continuing.");
        }
        catch (PlaywrightException ex) when (IsBenignBackNavigationAbort(ex))
        {
            _logger?.Warning($"NetworkIdle wait aborted during browser back ({ex.Message}) — page is loaded, continuing.");
        }
    }

    /// <summary>
    /// True for the transient navigation aborts that a cross-origin browser-back legitimately produces —
    /// the outgoing page's load being cancelled (<c>net::ERR_ABORTED</c>) or its frame being torn down
    /// (<c>frame was detached</c>). These mean "the page we were leaving stopped loading", not that the
    /// back-navigation itself failed, so they are safe to swallow. Any other PlaywrightException (a real
    /// failure) is left to propagate.
    /// </summary>
    private static bool IsBenignBackNavigationAbort(PlaywrightException ex) =>
        ex.Message.Contains("ERR_ABORTED", StringComparison.OrdinalIgnoreCase)
        || ex.Message.Contains("frame was detached", StringComparison.OrdinalIgnoreCase);

    public async Task RefreshPageAsync(double waitSeconds = 0)
    {
        try
        {
            _logger?.LogUiAction("Refresh", "Page", $"Refreshing page: {_page.Url}");
            await Task.Delay(500);
            await _page.ReloadAsync();
            await _page.WaitForURLAsync(_page.Url, new() { Timeout = 5000 });
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            if (waitSeconds > 0) await Task.Delay(TimeSpan.FromSeconds(waitSeconds));
        }
        catch
        {
            _logger?.Warning("Failed to refresh page");
        }
    }

    public async Task ScrollPageAsync(string position)
    {
        try
        {
            _logger?.Debug("Scroll", "Page", $"Scrolling to position: {position}");

            var command = position.ToLower() switch
            {
                "top" => "window.scrollTo(0, 0)",
                "bottom" => "window.scrollTo(0, document.body.scrollHeight)",
                _ => int.TryParse(position, out int pixels) ? $"window.scrollTo(0, {pixels})" : null
            };

            if (command == null) { _logger?.Warning($"Invalid scroll position: {position}"); return; }

            await _page.EvaluateAsync(command);
            await Task.Delay(100);
            _logger?.Info($"Page scrolled successfully to {position}");
        }
        catch (Exception ex)
        {
            _logger?.LogException(ex, $"Failed to scroll page to position '{position}'");
            throw;
        }
    }
}
