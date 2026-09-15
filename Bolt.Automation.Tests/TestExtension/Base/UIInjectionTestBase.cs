using System.Diagnostics;
using Automation.Configuration.FrontEnds;
using Bolt.Automation.Common.Exceptions;
using Bolt.Automation.Common.Logging.Mongo;
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

namespace Bolt.Automation.Tests.TestExtension.Base
{
    /// <summary>
    /// UI-flavoured <see cref="InjectionTestBase"/> — adds Playwright browser, page factory,
    /// and helper construction. Mirrors <see cref="UITestBase"/>'s lifecycle (concurrent-launch
    /// throttling, artifact capture on failure, deterministic cleanup) but inherits the
    /// runtime-injected SanityTestConfig pipeline instead of tenant-keyed data stores.
    /// </summary>
    public class UIInjectionTestBase : InjectionTestBase
    {
        protected IBrowserManager? _browserManager;
        protected IPageHelper? _pageHelper;
        protected IPage? _currentPage;
        protected PlaywrightExecutor? _executor;
        protected IPageFactory? _pageFactory;
        protected IServiceScope _uiTestScope;

        private bool _nonBrowserComponentsInitialized = false;
        private bool _browserComponentsInitialized = false;

        private readonly string _testArtifactsPath;

        private string? _cachedTestName;
        private IScreenshotManager? _cachedScreenshotManager;

        // Limits concurrent browser launches to prevent thread pool starvation.
        // See UITestBase for the original rationale — same constraint applies here.
        private static readonly SemaphoreSlim _browserInitSemaphore = new(5, 5);

        protected PlaywrightExecutor Executor =>
            _executor ?? throw new TestSetupException("Executor not initialized — call base [SetUp] first.");

        protected IPageFactory PageFactory =>
            _pageFactory ?? throw new TestSetupException("PageFactory not initialized — call base [SetUp] first.");

        protected IBrowserManager BrowserManager =>
            _browserManager ?? throw new TestSetupException("Browser not initialized — call base [SetUp] first.");

        protected UIInjectionTestBase() : base()
        {
            _testArtifactsPath = GetTestArtifactsPath();
            // Initialize the non-browser DI components (which create _uiTestScope
            // and resolve _pageFactory / _executor) in the ctor so that subclass
            // ResolveServices() overrides — invoked from the base [SetUp] BEFORE
            // this class's own [SetUp] runs — can read _uiTestScope.ServiceProvider.
            // DI here resolves stable infra (IPageFactory, PlaywrightExecutor); if
            // it fails it's a wiring bug, not a user-input issue, so the loss of
            // run-page visibility on that narrow failure mode is acceptable.
            EnsureNonBrowserComponentsInitialized();
        }

        /// <summary>
        /// NUnit runs [SetUp] base-first: <see cref="InjectionTestBase.InitializeInjectionTestAsync"/>
        /// (logger + ResolveServices), then this method (browser + InitializeComponents).
        /// </summary>
        [SetUp]
        public async Task InitializeUiAsync()
        {
            await EnsureBrowserComponentsInitializedAsync();
            InitializeComponents();
        }

        /// <summary>
        /// Override to assemble helpers that need both DI services and browser components.
        /// </summary>
        protected virtual void InitializeComponents() { }

        private void EnsureNonBrowserComponentsInitialized()
        {
            if (_nonBrowserComponentsInitialized) return;

            _uiTestScope = ServiceProvider.CreateScope();
            var provider = _uiTestScope.ServiceProvider;

            _pageFactory = provider.GetRequiredService<IPageFactory>();
            _executor = provider.GetService<PlaywrightExecutor>();
            _nonBrowserComponentsInitialized = true;
        }

        private async Task EnsureBrowserComponentsInitializedAsync()
        {
            EnsureNonBrowserComponentsInitialized();

            if (_browserComponentsInitialized) return;

            if (!await _browserInitSemaphore.WaitAsync(TimeSpan.FromSeconds(180)).ConfigureAwait(false))
                throw new TimeoutException("Timed out waiting for a browser initialization slot after 180s — too many concurrent launches");
            try
            {
                var provider = _uiTestScope.ServiceProvider;
                var browserOptions = TryGetService<BrowserOptions>(provider) ?? provider.GetRequiredService<IOptions<BrowserOptions>>().Value;

                _browserManager = provider.GetRequiredService<IBrowserManager>();
                _browserManager.Configure(browserOptions.Clone());
                _browserManager.PageChanged += OnPageChanged;

                _currentPage = await _browserManager.GetPageAsync()
                    .WaitAsync(TimeSpan.FromSeconds(90))
                    .ConfigureAwait(false);
                if (_currentPage?.IsClosed != false) throw new InvalidOperationException("Failed to initialize the browser page.");

                var screenshotManager = GetScreenshotManager();
                _pageHelper = new PageHelper(_currentPage, _logger, screenshotManager, ScopeContext, _browserManager);
                _executor ??= PlaywrightExecutorFactory.CreateWithBrowserManagerFactory(provider, () => _browserManager!);
                _browserComponentsInitialized = true;
            }
            finally
            {
                _browserInitSemaphore.Release();
            }
        }

        private void OnPageChanged(IPage newPage)
        {
            _currentPage = newPage;
            _cachedScreenshotManager = null;
        }

        private static T? TryGetService<T>(IServiceProvider provider) where T : class
            => provider.GetService<T>();

        private string GetCachedTestName() => _cachedTestName ??=
            TestContext.CurrentContext?.Test?.Name ?? GetTestMethodName();

        private IScreenshotManager? GetScreenshotManager() => _cachedScreenshotManager ??= _browserManager?.GetScreenshotManager();

        private bool IsPageReady() => _currentPage?.IsClosed == false;

        [TearDown]
        public async Task UiTearDownAsync()
        {
            var swTotal = Stopwatch.StartNew();

            try
            {
                (_pageFactory as IDisposable)?.Dispose();
                (_pageHelper as IDisposable)?.Dispose();
                (_executor as IDisposable)?.Dispose();

                await CaptureAndCleanupAsync()
                    .WaitAsync(TimeSpan.FromSeconds(30))
                    .ConfigureAwait(false);
            }
            catch (TimeoutException)
            {
                _logger?.Warning($"UI teardown timed out after 30s");
            }
            catch (Exception ex)
            {
                _logger?.Warning($"Error during UI teardown: {ex.Message}");
            }
            finally
            {
                try { _uiTestScope?.Dispose(); }
                catch (Exception ex) { _logger?.Warning($"Error disposing UI service scope: {ex.Message}"); }

                _logger?.Trace($"UI teardown completed in {swTotal.ElapsedMilliseconds}ms");
            }
        }

        protected virtual async Task CaptureAndCleanupAsync()
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
                _logger?.Trace("Resources already disposed during cleanup");
            }
            catch (Exception ex)
            {
                _logger?.Warning($"Error during async disposal: {ex.Message}");
            }
        }

        private async Task CaptureArtifactsAsync()
        {
            if (!IsPageReady()) return;
            if (!HasTestFailed) return;

            var tasks = new List<Task>();

            try { tasks.Add(CaptureFinalScreenshotAsync()); }
            catch (Exception ex) { _logger?.Warning($"Screenshot capture failed: {ex.Message}"); }

            try { tasks.Add(SavePageSourceAsync()); }
            catch (Exception ex) { _logger?.Warning($"Page source capture failed: {ex.Message}"); }

            try { tasks.Add(CaptureDomSnapshotAsync()); }
            catch (Exception ex) { _logger?.Warning($"DOM snapshot capture failed: {ex.Message}"); }

            if (tasks.Count > 0)
                await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        protected async Task CaptureFinalScreenshotAsync(string? testName = null) =>
            await ExecuteArtifactCapture(async () =>
            {
                testName ??= GetCachedTestName();
                await EnsurePageReady();
                var screenshotManager = GetScreenshotManager();
                if (screenshotManager != null)
                {
                    var finalName = testName + "_final";
                    var screenshotBytes = await screenshotManager.CaptureScreenshotBytesAsync(finalName);
                    if (screenshotBytes.Length > 0)
                    {
                        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                        var fileName = $"{finalName}_{timestamp}.png";
                        await UploadArtifactBytesAsync(screenshotBytes, fileName, "image/png", ArtifactType.Screenshot);
                    }
                    else
                        _logger?.Debug("Screenshot capture failed or was skipped");
                }
                else
                {
                    _logger?.Error("Screenshot manager not available - browser initialization may have failed");
                }
            }, "screenshot");

        protected async Task CaptureDomSnapshotAsync(string? testName = null) =>
            await ExecuteArtifactCapture(async () =>
            {
                testName ??= GetCachedTestName();
                var screenshotManager = GetScreenshotManager();
                if (screenshotManager != null)
                {
                    var domBytes = await screenshotManager.CaptureDomSnapshotBytesAsync(testName + "_dom");
                    if (domBytes.Length > 0)
                    {
                        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                        var fileName = $"{testName}_dom_{timestamp}.html";
                        await UploadArtifactBytesAsync(domBytes, fileName, "text/html", ArtifactType.DomSnapshot);
                    }
                }
            }, "DOM snapshot");

        private async Task CleanupResourcesAsync()
        {
            try
            {
                if (_browserManager != null)
                    _browserManager.PageChanged -= OnPageChanged;

                if (_currentPage != null && !_currentPage.IsClosed)
                    await _currentPage.CloseAsync().ConfigureAwait(false);
            }
            catch (Exception ex) when (ex.Message.Contains("Target page, context or browser has been closed"))
            {
                _logger?.Trace($"Page already closed during cleanup");
            }
            catch (Exception ex)
            {
                _logger?.Warning($"Error closing page: {ex.Message}");
            }

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

            _cachedScreenshotManager = null;
            _cachedTestName = null;
        }

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
                _logger?.Trace($"Page readiness check failed: {ex.Message}");
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
                    _logger?.Debug($"Cannot capture {artifactType} - page is not available");
                    return;
                }
                await captureAction();
            }
            catch (Exception ex)
            {
                _logger?.Warning($"Unexpected error capturing {artifactType}: {ex.Message}");
            }
        }

        protected string GetCurrentBaseUrl()
        {
            var currentTab = BrowserManager.GetCurrentTab();
            var currentUrl = currentTab?.Url ?? throw new InvalidOperationException("No active page available");
            var uri = new Uri(currentUrl);
            return $"{uri.Scheme}://{uri.Host}/";
        }
    }
}
