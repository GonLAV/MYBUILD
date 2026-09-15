using System.Diagnostics;
using Automation.Configuration.FrontEnds;
using Bolt.Automation.Common.Utils;
using Bolt.Automation.FrontEnds.Executor;
using Bolt.Automation.FrontEnds.PlaywrightBase;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Screenshot;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;
using NUnit.Framework;

namespace Bolt.Automation.InfraTests.TestExtension.Base
{
    public class UITestBase : TestBase, IAsyncDisposable
    {
        // Services
        protected IBrowserManager? _browserManager;
        protected IPageHelper? _pageHelper;
        protected IPage? _currentPage;
        protected PlaywrightExecutor? _executor;
        protected IPageFactory? _pageFactory;
        protected IServiceScope _uiTestScope;

        // Initialization flags
        private bool _nonBrowserComponentsInitialized = false;
        private bool _browserComponentsInitialized = false;

        // Context management
        protected Dictionary<string, (IBrowserContext Context, IPage Page, IPageHelper Helper)> _additionalContexts = new();
        private int _contextCounter = 0;
        private readonly string _testArtifactsPath;
        private bool _uiDisposed;

        // Cached values to avoid repeated calls
        private string? _cachedTestName;
        private IScreenshotManager? _cachedScreenshotManager;

        // Properties with proper lazy initialization
        protected PlaywrightExecutor Executor
        {
            get
            {
                EnsureBrowserComponentsInitialized(); 
                return _executor ?? throw new InvalidOperationException("Executor initialization failed");
            }
        }

        protected IPageFactory PageFactory
        {
            get
            {
                EnsureNonBrowserComponentsInitialized();
                return _pageFactory ?? throw new InvalidOperationException("PageFactory initialization failed");
            }
        }

        protected IBrowserManager BrowserManager
        {
            get
            {
                EnsureBrowserComponentsInitialized();
                return _browserManager ?? throw new InvalidOperationException("Browser initialization failed");
            }
        }

        protected UITestBase() : base()
        {
            _testArtifactsPath = GetTestArtifactsPath();
            EnsureNonBrowserComponentsInitialized();
        }

        private void EnsureNonBrowserComponentsInitialized()
        {
            if (_nonBrowserComponentsInitialized) return;

            try
            {
                _uiTestScope = ServiceProvider.CreateScope();
                var provider = _uiTestScope.ServiceProvider;

                // Initialize non-browser services
                _pageFactory = provider.GetRequiredService<IPageFactory>();
                _executor = provider.GetService<PlaywrightExecutor>();
                _nonBrowserComponentsInitialized = true;
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to initialize non-browser components: {ex.Message}");
                throw;
            }
        }

        private void EnsureBrowserComponentsInitialized()
        {
            EnsureNonBrowserComponentsInitialized();

            if (_browserComponentsInitialized) return;

            try
            {
                var provider = _uiTestScope.ServiceProvider;
                var browserOptions = TryGetService<BrowserOptions>(provider) ?? provider.GetRequiredService<IOptions<BrowserOptions>>().Value;

                // Initialize browser-specific services
                _browserManager = provider.GetRequiredService<IBrowserManager>();
                _browserManager.Configure(browserOptions.Clone());

                _currentPage = _browserManager.GetPageAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                if (_currentPage?.IsClosed != false) throw new InvalidOperationException("Failed to initialize the browser page.");

                var screenshotManager = GetScreenshotManager();
                _pageHelper = new PageHelper(_currentPage, _logger, screenshotManager, ScopeContext, _browserManager);
                _executor ??= PlaywrightExecutorFactory.CreateWithBrowserManagerFactory(provider, () => _browserManager!);
                _browserComponentsInitialized = true;
                
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to initialize browser components: {ex.Message}");
                throw;
            }
        }

        private static T? TryGetService<T>(IServiceProvider provider) where T : class
        {
            try { return provider.GetRequiredService<T>(); }
            catch { return null; }
        }

        private string GetCachedTestName() => _cachedTestName ??=
            TestContext.CurrentContext?.Test?.Name ?? GetTestMethodName();

        private IScreenshotManager? GetScreenshotManager() => _cachedScreenshotManager ??= _browserManager?.GetScreenshotManager();

        private bool IsPageReady() => _currentPage?.IsClosed == false;

        protected async Task<(IPage Page, IPageHelper Helper)> CreateNewContextAsync(string? contextKey = null)
        {
            string key = contextKey ?? $"context_{++_contextCounter}";
            var browser = await BrowserManager.GetBrowserAsync();
            var context = await browser.NewContextAsync();
            var page = await context.NewPageAsync();
            var screenshotManager = _browserManager?.GetScreenshotManager();
            var pageHelper = new PageHelper(page,
                _logger,
                screenshotManager,
                browserManager: BrowserManager);
            _additionalContexts[key] = (context, page, pageHelper);
            return (page, pageHelper);
        }

        public override void Dispose()
        {
            if (_uiDisposed) return;

            var sw = Stopwatch.StartNew();
            _logger?.Info($"Disposing UI test - start");

            try
            {
                var swSync = Stopwatch.StartNew();
                DisposeSync();
                swSync.Stop();
                _logger?.Info($"DisposeSync completed in {swSync.ElapsedMilliseconds}ms");

                // Run the async disposal synchronously to avoid disposing the scope (and scoped services)
                // while async cleanup runs in background which can cause synchronous Dispose to be invoked
                // on scoped services leading to long blocking waits.
                try
                {
                    var swAsync = Stopwatch.StartNew();
                    DisposeAsyncCore().ConfigureAwait(false).GetAwaiter().GetResult();
                    swAsync.Stop();
                    _logger?.Info($"DisposeAsyncCore completed in {swAsync.ElapsedMilliseconds}ms");
                }
                catch (Exception ex)
                {
                    _logger?.Error($"Async disposal failed during Dispose: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                _logger?.Error($"Error during UI disposal: {ex.Message}");
            }
            finally
            {
                var swScope = Stopwatch.StartNew();
                try
                {
                    _uiTestScope?.Dispose();
                }
                catch (Exception ex)
                {
                    _logger?.Warning($"Error disposing UI service scope: {ex.Message}");
                }
                finally
                {
                    swScope.Stop();
                    _logger?.Info($"Service scope disposed in {swScope.ElapsedMilliseconds}ms");
                    _uiDisposed = true;
                    sw.Stop();
                    _logger?.Info($"Total UI disposal time: {sw.ElapsedMilliseconds}ms");
                    base.Dispose();
                }
            }
        }

        private void DisposeSync()
        {
            (_pageFactory as IDisposable)?.Dispose();
            (_pageHelper as IDisposable)?.Dispose();
            (_executor as IDisposable)?.Dispose();
        }

        public async ValueTask DisposeAsync()
        {
            if (_uiDisposed) return;
            await DisposeAsyncCore();
            _uiDisposed = true;
            GC.SuppressFinalize(this);
        }

        protected virtual async Task DisposeAsyncCore()
        {
            try
            {
                var swCap = Stopwatch.StartNew();
                await CaptureArtifactsAsync().ConfigureAwait(false);
                swCap.Stop();
                _logger?.Trace($"CaptureArtifactsAsync completed in {swCap.ElapsedMilliseconds}ms");

                var swClean = Stopwatch.StartNew();
                await CleanupResourcesAsync().ConfigureAwait(false);
                swClean.Stop();
                _logger?.Trace($"CleanupResourcesAsync completed in {swClean.ElapsedMilliseconds}ms");
            }
            catch (ObjectDisposedException)
            {
                _logger?.Debug("Resources already disposed during cleanup");
            }
            catch (Exception ex)
            {
                _logger?.Error($"Error during async disposal: {ex.Message}");
            }
        }

        private async Task CaptureArtifactsAsync()
        {
            if (!IsPageReady()) return;

            var tasks = new List<Task>();

            try { tasks.Add(CaptureScreenshotAsync()); }
            catch (Exception ex) { _logger?.Warning($"Screenshot capture failed: {ex.Message}"); }

            try { tasks.Add(SavePageSourceAsync()); }
            catch (Exception ex) { _logger?.Warning($"Page source capture failed: {ex.Message}"); }

            if (tasks.Count > 0)
                await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        private async Task CleanupResourcesAsync()
        {
            var tasks = new List<Task>();

            if (IsPageReady())
                tasks.Add(_currentPage!.CloseAsync());

            foreach (var (context, page, helper) in _additionalContexts.Values)
            {
                try
                {
                    (helper as IDisposable)?.Dispose();
                    if (page?.IsClosed == false) tasks.Add(page.CloseAsync());
                    tasks.Add(context.CloseAsync());
                }
                catch (Exception ex)
                {
                    _logger?.Warning($"Error disposing additional context: {ex.Message}");
                }
            }

            if (tasks.Count > 0)
                await Task.WhenAll(tasks).ConfigureAwait(false);

            _additionalContexts.Clear();

            try
            {
                if (_browserManager is IAsyncDisposable asyncDisposable)
                    await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                else
                    (_browserManager as IDisposable)?.Dispose();
            }
            catch (Exception ex)
            {
                _logger?.Warning($"Error disposing browser manager: {ex.Message}");
            }

            // Clear cached references
            _cachedScreenshotManager = null;
            _cachedTestName = null;
        }

        protected async Task CaptureScreenshotAsync(string? testName = null) =>
            await ExecuteArtifactCapture(async () =>
            {
                testName ??= GetCachedTestName();
                await EnsurePageReady();

                var screenshotManager = GetScreenshotManager();
                if (screenshotManager != null)
                {
                    var screenshotPath = await screenshotManager.CaptureTestScreenshotAsync(testName);
                    if (!string.IsNullOrEmpty(screenshotPath))
                        _logger?.Trace($"Test screenshot saved at: {screenshotPath}");
                    else
                    {
                        _logger?.Warning("Screenshot capture failed or was skipped");
                    }
                }
                else
                {
                    _logger?.Error("Screenshot manager not available - browser initialization may have failed");
                }
            }, "screenshot");

        private async Task EnsurePageReady()
        {
            if (!IsPageReady()) return;

            try
            {
                await _currentPage!.WaitForLoadStateAsync(LoadState.NetworkIdle, new() { Timeout = 3000 });
                await Task.Delay(500);
            }
            catch (Exception ex)
            {
                _logger?.Debug($"Page readiness check failed: {ex.Message}");
            }
        }

        protected async Task SavePageSourceAsync(string? testName = null) =>
            await ExecuteArtifactCapture(async () =>
            {
                testName ??= GetCachedTestName();
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var htmlDir = Path.Combine(_testArtifactsPath, FileNameUtils.SanitizeFileName(testName));
                Directory.CreateDirectory(htmlDir);
                var htmlPath = Path.Combine(htmlDir, $"page_source_{timestamp}.html");
                var pageContent = await _currentPage!.ContentAsync().ConfigureAwait(false);
                await File.WriteAllTextAsync(htmlPath, pageContent).ConfigureAwait(false);
                _logger?.Trace($"Page source saved at: {htmlPath}");
            }, "page source");

        private async Task ExecuteArtifactCapture(Func<Task> captureAction, string artifactType)
        {
            try
            {
                if (!IsPageReady())
                {
                    _logger?.Warning($"Cannot capture {artifactType} - page is not available");
                    return;
                }
                await captureAction();
            }
            catch (Exception ex)
            {
                _logger?.Error($"Unexpected error capturing {artifactType}: {ex.Message}");
            }
        }
    }
}