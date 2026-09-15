using Automation.Configuration.FrontEnds;
using Bolt.Automation.Common.Exceptions;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Screenshot;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;

public class BrowserManager : IBrowserManager, IDisposable, IAsyncDisposable, IConfigurable<BrowserOptions>
{
    private readonly IAutomationLogger _logger;
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IBrowserContext? _context;
    private IPage? _page;
    private IScreenshotManager? _screenshotManager;
    private static readonly BrowserOptions DefaultConfig = new();
    private BrowserOptions _currentConfig;
    private bool _disposed = false;
    private bool _initialized = false;
    private readonly object _lock = new();

    // Event for notifying about page changes
    public event Action<IPage>? PageChanged;

    public BrowserManager(IAutomationLogger logger, IOptions<BrowserOptions>? options = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _currentConfig = options?.Value ?? DefaultConfig;
    }

    // Helper method to notify subscribers about page changes
    private void NotifyPageChanged(IPage newPage)
    {
        try
        {
            UpdateScreenshotManager(newPage);
            PageChanged?.Invoke(newPage);
        }
        catch (Exception ex)
        {
            _logger.Error($"Error notifying page change subscribers: {ex.Message}");
        }
    }


    private void UpdateScreenshotManager(IPage page)
    {
        var screenshotOptions = new ScreenshotOptions
        {
            BaseDirectory = _currentConfig.Screenshots.BaseDirectory,
            CaptureOnFailure = _currentConfig.Screenshots.CaptureOnFailure,
            OrganizeByTest = _currentConfig.Screenshots.OrganizeByTest
        };
        _screenshotManager = new ScreenshotManager(page, screenshotOptions, _logger);
    }

    public void Configure(BrowserOptions settings)
    {
        if (settings == null) throw new ArgumentNullException(nameof(settings));
        lock (_lock)
        {
            _currentConfig = settings;
            if (_initialized)
            {
                CleanupResources();
                InitializeSync(_currentConfig);
            }
        }
    }

    private void CleanupResources()
    {
        SafeCloseAsync(_page, p => p.CloseAsync(), "page");
        _page = null;
        SafeCloseAsync(_context, c => c.CloseAsync(), "context");
        _context = null;
        SafeCloseAsync(_browser, b => b.CloseAsync(), "browser");
        _browser = null;
    }

    private void SafeCloseAsync<T>(T? obj, Func<T, Task> closeFunc, string resourceName) where T : class
    {
        if (obj == null) return;

        try
        {
            closeFunc(obj).GetAwaiter().GetResult();
        }
        catch (ObjectDisposedException)
        {
            // Already disposed - expected during parallel test cleanup
        }
        catch (Exception ex) when (ex.Message.Contains("Target page, context or browser has been closed") ||
                                    ex.Message.Contains("has been closed") ||
                                    ex.Message.Contains("Browser closed"))
        {
            // Resource already closed - suppress expected cleanup errors
        }
        catch (Exception ex)
        {
            _logger.Error($"Error during cleanup of {resourceName}: {ex.Message}");
        }
    }

    private void InitializeSync(BrowserOptions config)
    {
        InitializeAsync(config).GetAwaiter().GetResult();
    }

    private async Task EnsureInitializedAsync()
    {
        bool needsInit;
        lock (_lock) { needsInit = !_initialized; }
        if (needsInit) await InitializeAsync(_currentConfig).ConfigureAwait(false);
    }

    private async Task InitializeAsync(BrowserOptions config)
    {
        if (_playwright == null)
            _playwright = await Playwright.CreateAsync().ConfigureAwait(false);

        var options = config.ToBrowserTypeLaunchOptions();
        _browser = await LaunchBrowserAsync(config.BrowserType, options).ConfigureAwait(false);
        _context = await _browser.NewContextAsync(config.ToBrowserNewContextOptions()).ConfigureAwait(false);
        await _context.AddInitScriptAsync("Object.defineProperty(navigator, 'webdriver', { get: () => undefined })").ConfigureAwait(false);
        _initialized = true;
    }

    private async Task<IBrowser> LaunchBrowserAsync(BrowserOptions.BrowserTypeEnum browserType, BrowserTypeLaunchOptions options)
    {
        return browserType switch
        {
            BrowserOptions.BrowserTypeEnum.Chrome => await _playwright!.Chromium.LaunchAsync(options).ConfigureAwait(false),
            BrowserOptions.BrowserTypeEnum.Chromium => await _playwright!.Chromium.LaunchAsync(options).ConfigureAwait(false),
            BrowserOptions.BrowserTypeEnum.Firefox => await _playwright!.Firefox.LaunchAsync(options).ConfigureAwait(false),
            BrowserOptions.BrowserTypeEnum.Webkit => await _playwright!.Webkit.LaunchAsync(options).ConfigureAwait(false),
            BrowserOptions.BrowserTypeEnum.Edge => await _playwright!.Chromium.LaunchAsync(options).ConfigureAwait(false),
            _ => throw new ArgumentOutOfRangeException(nameof(browserType), browserType, "Unsupported browser type")
        };
    }

    public async Task<IPage> GetPageAsync()
    {
        if (_page != null && !_page.IsClosed) return _page;
        await EnsureInitializedAsync().ConfigureAwait(false);
        lock (_lock)
        {
            if (_page == null || _page.IsClosed)
            {
                _page = _context!.NewPageAsync().GetAwaiter().GetResult();
                NotifyPageChanged(_page);
            }
            return _page;
        }
    }

    public IScreenshotManager? GetScreenshotManager()
    {
        return _screenshotManager;
    }

    public IPage? GetCurrentTab()
    {
        lock (_lock) 
        { 
            return _page; 
        }
    }

    public async Task<IBrowser> GetBrowserAsync()
    {
        await EnsureInitializedAsync().ConfigureAwait(false);
        return _browser!;
    }

    public async Task<IBrowserContext> GetContextAsync()
    {
        await EnsureInitializedAsync().ConfigureAwait(false);
        return _context!;
    }

    public async Task NavigateAsync(string url, string? targetPageName = null, int timeout = 60000)
    {
        IPage page = await GetPageAsync().ConfigureAwait(false);
        // Normalize first so the page name and logged URL describe the REAL target, not an AWS-tracking
        // wrapper — an emailed deeplink arrives as awstrack.me/L0/<encoded-real-url>/<tracking-id>, whose
        // last segment is a meaningless tracking id and whose wrapper hides where we're actually heading.
        string destination = NormalizeUrl(url);
        string currentPageName = TryGetPageName(page.Url);
        targetPageName ??= TryGetPageName(destination);
        // Single-use token URLs (emailed deeplinks) must be navigated ONCE: each hit consumes/validates
        // the token server-side, so retrying a goto that timed out would re-spend it. Detect the token
        // on the real (unwrapped) destination and disable navigation retries for it.
        bool isSingleUse = IsSingleUseUrl(destination);
        // Record the destination up-front so the reader can see where a navigation is heading —
        // e.g. an email deeplink jump to the consumer/D2C site would otherwise be an opaque step.
        // The full URL (including any token) is logged intentionally: these are short-lived test
        // deeplinks and the complete string is needed to reproduce/debug the navigation.
        _logger.Info($"Navigating FROM [{currentPageName}] TO [{targetPageName}]: {destination}");
        await RetryNavigationAsync(page, url, timeout, currentPageName, targetPageName, allowRetry: !isSingleUse);
    }

    /// <summary>
    /// Whether <paramref name="url"/> is a single-use link whose navigation must not be retried — i.e.
    /// it carries a one-time <c>token=</c> credential (emailed quote deeplinks). Pass the unwrapped
    /// destination from <see cref="NormalizeUrl"/> so the token inside an AWS-tracking wrapper is seen.
    /// </summary>
    private static bool IsSingleUseUrl(string url) =>
        !string.IsNullOrEmpty(url) && url.Contains("token=", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Resolves the real navigation target for logging. AWS SES click-tracking wraps the true URL as
    /// <c>https://&lt;id&gt;.r.&lt;region&gt;.awstrack.me/L0/&lt;percent-encoded-real-url&gt;/&lt;tracking-id&gt;</c>;
    /// this unwraps the <c>/L0/</c> segment and URL-decodes it so the log shows the actual host/path
    /// (e.g. the D2C consumer site) rather than the opaque tracker. Non-tracking URLs are returned
    /// unchanged. Never throws — falls back to the original URL on any parse failure.
    /// </summary>
    private static string NormalizeUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return url;

        try
        {
            const string marker = "awstrack.me/L0/";
            var markerIdx = url.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (markerIdx < 0) return url;

            // Everything after /L0/ up to the trailing /<tracking-id> segment is the encoded real URL.
            var afterMarker = url[(markerIdx + marker.Length)..];
            var lastSlash = afterMarker.LastIndexOf('/');
            var encodedTarget = lastSlash > 0 ? afterMarker[..lastSlash] : afterMarker;

            var decoded = Uri.UnescapeDataString(encodedTarget);
            return decoded.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? decoded : url;
        }
        catch
        {
            return url;
        }
    }

    private async Task RetryNavigationAsync(IPage page, string url, int timeout, string currentPageName, string targetPageName, bool allowRetry = true)
    {
        // Single-use token URLs (deeplinks) must be hit once — each goto consumes the token — so cap
        // attempts at 1 for them; ordinary URLs keep the transient-failure resilience of 3 attempts.
        int maxAttempts = allowRetry ? 3 : 1;
        Exception? lastException = null;
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                if (attempt > 1) await Task.Delay(new Random().Next(100, 500)).ConfigureAwait(false);
                var gotoOptions = new PageGotoOptions { WaitUntil = WaitUntilState.Load, Timeout = timeout };
                await page.GotoAsync(url, gotoOptions).ConfigureAwait(false);
                return;
            }
            catch (TimeoutException ex)
            {
                lastException = ex;
                _logger.Warning($"Attempt {attempt}: Navigation FROM [{currentPageName}] TO [{targetPageName}] failed due to timeout: {ex.Message}");
                if (attempt < maxAttempts) await Task.Delay(1000 * attempt).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                lastException = ex;
                _logger.Error($"Attempt {attempt}: Unexpected error during navigation FROM [{currentPageName}] TO [{targetPageName}]: {ex.Message}");
                if ((ex.Message.Contains("net::ERR_ABORTED") || ex.Message.Contains("net::ERR_CONNECTION_RESET") || ex.Message.Contains("net::ERR_CONNECTION_REFUSED")) && attempt < maxAttempts)
                    await Task.Delay(1000 * attempt).ConfigureAwait(false);
                else throw;
            }
        }
        if (!allowRetry)
            _logger.Warning($"Single-use navigation TO [{targetPageName}] was not retried (token URL); surfacing the first failure.");
        else
            _logger.Error($"All {maxAttempts} navigation attempts failed for [{currentPageName}] -> [{targetPageName}].");
        throw new NavigationException(currentPageName, targetPageName, url, maxAttempts, lastException);
    }

    private string TryGetPageName(string url)
    {
        // Last path segment, query string dropped so the page name stays a clean identifier
        // (e.g. "automationpp", not "automationpp?token=..."). The full URL is logged separately.
        try { return url.Split('?', 2)[0].Split('/').Last(); } catch { return "Unknown"; }
    }

    public async Task TakeScreenshotAsync(string path)
    {
        if (_screenshotManager != null)
        {
            var directory = Path.GetDirectoryName(path) ?? ".";
            var name = Path.GetFileNameWithoutExtension(path);
            await _screenshotManager.CaptureScreenshotAsync(name, directory);
            _logger.Info($"Screenshot taken and saved to: {path}");
        }
        else
        {
            _logger.Warning("Screenshot manager not available");
        }
    }

    public async Task OpenNewTabAsync(string url = "")
    {
        var context = await GetContextAsync().ConfigureAwait(false);
        var newPage = await context.NewPageAsync().ConfigureAwait(false);
        _page = newPage;

        if (!string.IsNullOrEmpty(url))
        {
            await _page.GotoAsync(url).ConfigureAwait(false);
            _logger.Info($"Opened new tab and navigated to: {url}");
        }
        else
        {
            _logger.Info("Opened new tab");
        }

        // Notify subscribers about the page change
        NotifyPageChanged(_page);
    }

    public async Task OpenNewWindowAsync(string url = "")
    {
        var browser = await GetBrowserAsync().ConfigureAwait(false);

        // Close the old context to prevent resource leaks
        var oldContext = _context;
        var newContext = await browser.NewContextAsync(_currentConfig.ToBrowserNewContextOptions()).ConfigureAwait(false);
        var newPage = await newContext.NewPageAsync().ConfigureAwait(false);

        _context = newContext;
        _page = newPage;

        if (oldContext != null)
        {
            try { await oldContext.CloseAsync().ConfigureAwait(false); }
            catch (Exception ex) { _logger.Warning($"Error closing previous context during OpenNewWindow: {ex.Message}"); }
        }

        if (!string.IsNullOrEmpty(url))
        {
            await _page.GotoAsync(url).ConfigureAwait(false);
            _logger.Info($"Opened new window and navigated to: {url}");
        }
        else
        {
            _logger.Info("Opened new window");
        }

        // Notify subscribers about the page change
        NotifyPageChanged(_page);
    }

    public async Task SwitchToFirstTabAsync()
    {
        var context = await GetContextAsync().ConfigureAwait(false);
        var pages = context.Pages;
        if (pages.Count == 0) throw new TestSetupException("No pages available to switch.");

        var firstPage = pages[0];
        if (_page == firstPage)
        {
            _logger.Info($"Already on first tab: {_page.Url}");
            return;
        }

        _page = firstPage;

        await _page.BringToFrontAsync().ConfigureAwait(false);
        NotifyPageChanged(_page);

        _logger.Info($"Switched to tab first: {_page.Url}");
    }

    public async Task SwitchToLastTabAsync(
    int maxWaitMs = 5000,
    int pollIntervalMs = 100,
    int urlSettleTimeoutMs = 3000)
    {
        var context = await GetContextAsync().ConfigureAwait(false);
        var originalPage = _page;
        // Read before the wait, so a same-tab navigation can be told apart from a click
        // that never fired (see the failure path below).
        var originalUrlAtCall = SafeUrl(originalPage);

        int waited = 0;
        IPage? newPage = null;

        // Handle race condition: the new tab may have already opened before this method
        // was called (click → tab opens → SwitchToLastTabAsync called).
        // Instead of tracking pages by snapshot diff, look for any page that is not the
        // current one — this works whether the tab opened before or after the call.
        while (waited < maxWaitMs)
        {
            newPage = context.Pages.LastOrDefault(p => p != originalPage);
            if (newPage != null)
                break;
            await Task.Delay(pollIntervalMs).ConfigureAwait(false);
            waited += pollIntervalMs;
        }

        if (newPage == null)
        {
            // Three very different faults land here and the old message could not separate them:
            // the click never fired, the app navigated in the SAME tab instead of opening one, or
            // the originating form refused to submit. Capture the originating tab's URL before and
            // after the wait plus every open tab, so a CI-only failure is diagnosable from the TRX
            // alone. This runs only when we are already failing, so the extra reads cost nothing.
            var originalUrlNow = SafeUrl(originalPage);
            var openTabs = string.Join(", ", context.Pages.Select(SafeUrl));
            var verdict = originalUrlAtCall == originalUrlNow
                ? "unchanged - the click never fired, or the form did not submit"
                : "CHANGED - the app navigated in the same tab instead of opening a new one";

            var message =
                $"No new tab appeared within {maxWaitMs}ms. Current tab count: {context.Pages.Count}. " +
                $"Originating tab was '{originalUrlAtCall}' at call time and is '{originalUrlNow}' now ({verdict}). " +
                $"Open tabs: [{openTabs}].";

            _logger.Warning(message);
            throw new NavigationException(message);
        }

        // Set as current tab and notify
        _page = newPage;
        NotifyPageChanged(_page);

        // Wait for the URL to move away from about:blank so callers get the real navigation
        // URL rather than the initial empty state. We do NOT wait for load state because in
        // lower environments the target URL may be IP-blocked and the page will never finish
        // loading — we only need the URL to have updated.
        var urlDeadline = DateTime.UtcNow.AddMilliseconds(urlSettleTimeoutMs);
        while (_page.Url == "about:blank" && DateTime.UtcNow < urlDeadline)
            await Task.Delay(pollIntervalMs).ConfigureAwait(false);

        _logger.Info($"Switched to last tab: {_page.Url}");


    }

    /// <summary>Reads a page's URL without ever throwing - a page that was closed mid-flight
    /// must not mask the failure we are actually trying to report.</summary>
    private static string SafeUrl(IPage? page)
    {
        if (page == null) return "<none>";
        try
        {
            return page.IsClosed ? "<closed>" : page.Url;
        }
        catch
        {
            return "<unavailable>";
        }
    }

    public async Task CloseLastTabAsync()
    {
        var context = await GetContextAsync().ConfigureAwait(false);
        var pages = context.Pages;
        if (pages.Count > 1)
        {
            var lastPage = pages[^1];
            var previousPage = pages[^2];

            await lastPage.CloseAsync().ConfigureAwait(false);
            _page = previousPage;

            // Notify subscribers about the page change
            NotifyPageChanged(_page);

            _logger.Info("Closed last tab and switched to previous tab");
        }
        else
        {
            _logger.Info("Only one tab exists, not closing");
        }

    }

    public async Task CloseCurrentTabAsync()
    {
        var context = await GetContextAsync().ConfigureAwait(false);
        var pages = context.Pages;
        if (_page == null || _page.IsClosed)
        {
            _logger.Info("No active tab to close");
            return;
        }

        if (pages.Count <= 1)
        {
            _logger.Info("Only one tab exists, not closing");
            return;
        }

        var pagesList = pages.ToList();
        var currentPage = _page;
        var currentIndex = pagesList.IndexOf(currentPage);

        try
        {
            await currentPage.CloseAsync().ConfigureAwait(false);
        }
        catch (Exception ex) when (ex.Message.Contains("Target page, context or browser has been closed") ||
                                    ex.Message.Contains("has been closed") ||
                                    ex.Message.Contains("Browser closed"))
        {
            _logger.Warning("Current tab already closed");
        }
        catch (Exception ex)
        {
            _logger.Warning($"Error closing current tab: {ex.Message}");
        }

        var nextPage = currentIndex > 0
            ? pagesList[currentIndex - 1]
            : pagesList.Skip(1).FirstOrDefault();

        if (nextPage == null || nextPage.IsClosed)
            return;

        try
        {
            _page = nextPage;
            await _page.BringToFrontAsync().ConfigureAwait(false);
            NotifyPageChanged(_page);
            _logger.Info("Closed current tab and switched to another tab");
        }
        catch (Exception ex) when (ex.Message.Contains("Target page, context or browser has been closed") ||
                                    ex.Message.Contains("has been closed") ||
                                    ex.Message.Contains("Browser closed"))
        {
            _logger.Warning("Next tab already closed");
        }
    }

    public async Task<IReadOnlyList<IPage>> GetAllWindowsAsync()
    {
        var context = await GetContextAsync().ConfigureAwait(false);
        return context.Pages;
    }

    public IReadOnlyList<IPage> GetAllWindows()
    {
        if (_context == null) throw new TestSetupException("Browser context has not been initialized.");
        return _context.Pages;
    }

    public void Dispose()
    {
        if (_disposed) return;
        try
        {
            CleanupResources();
            _playwright?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.Error($"Error during dispose: {ex.Message}");
        }
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        try
        {
            await CleanupResourcesAsync().ConfigureAwait(false);
            _playwright?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.Error($"Error during async dispose: {ex.Message}");
        }
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private async Task CleanupResourcesAsync()
    {
        await SafeCloseResourceAsync(_page, p => p.CloseAsync(), "page").ConfigureAwait(false);
        _page = null;
        await SafeCloseResourceAsync(_context, c => c.CloseAsync(), "context").ConfigureAwait(false);
        _context = null;
        await SafeCloseResourceAsync(_browser, b => b.CloseAsync(), "browser").ConfigureAwait(false);
        _browser = null;
    }

    private async Task SafeCloseResourceAsync<T>(T? obj, Func<T, Task> closeFunc, string resourceName) where T : class
    {
        if (obj == null) return;

        try
        {
            await closeFunc(obj).ConfigureAwait(false);
        }
        catch (ObjectDisposedException)
        {
            // Already disposed - expected during parallel test cleanup
        }
        catch (Exception ex) when (ex.Message.Contains("Target page, context or browser has been closed") ||
                                    ex.Message.Contains("has been closed") ||
                                    ex.Message.Contains("Browser closed"))
        {
            // Resource already closed - suppress expected cleanup errors
        }
        catch (Exception ex)
        {
            _logger.Error($"Error during async cleanup of {resourceName}: {ex.Message}");
        }
    }
}

public interface IConfigurable<T>
{
    void Configure(T settings);
}