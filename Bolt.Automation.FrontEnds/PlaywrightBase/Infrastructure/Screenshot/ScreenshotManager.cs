using System.Runtime.InteropServices;
using SkiaSharp;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.Common.Utils;
using Microsoft.Playwright;

namespace Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Screenshot;

public class ScreenshotManager : IScreenshotManager
{
    private readonly IPage _page;
    private readonly IAutomationLogger? _logger;
    private readonly ScreenshotOptions _options;
    private static int _failureScreenshotsCapturedGlobal;
    private static readonly object _failureLock = new();
    private static readonly string[] FailureScreenshotIgnorePatterns =
    {
        "Unsupported dropdown type",
        "Failed to select dropdown option",
        "Failed to perform Select action",
        "Failed to interact with locator"
    };

    public ScreenshotManager(IPage page, ScreenshotOptions? options = null, IAutomationLogger? logger = null)
    {
        _page = page ?? throw new ArgumentNullException(nameof(page));
        _options = options ?? new ScreenshotOptions();
        _logger = logger;
    }

    public async Task<string> CaptureScreenshotAsync(string? name = null, string? directory = null)
    {
        try
        {
            if (_page?.IsClosed != false)
            {
                _logger?.Warning("Cannot capture screenshot - page is not available");
                return string.Empty;
            }
            name ??= "screenshot";
            var timestamp = DateTime.Now.ToString(_options.TimestampFormat);
            var fileName = $"{name}_{timestamp}.png";
            var targetDirectory = directory ?? _options.BaseDirectory;
            Directory.CreateDirectory(targetDirectory);
            var fullPath = Path.Combine(targetDirectory, fileName);
            await WaitForPageStable();
            var screenshotBytes = await CaptureFullPageWithScrolling();
            if (screenshotBytes.Length ==0)
            {
                _logger?.Warning($"Screenshot appears to be empty, skipping save for {fileName}");
                return string.Empty;
            }
            // Optional URL overlay
            if (_options.IncludeUrlOverlay)
                screenshotBytes = AddUrlOverlay(screenshotBytes, _page.Url);
            await File.WriteAllBytesAsync(fullPath, screenshotBytes);
            _logger?.Trace($"Screenshot captured: {fullPath}");
            _logger?.Trace($"Page: {await _page.TitleAsync()} | URL: {_page.Url}");
            return fullPath;
        }
        catch (Exception ex)
        {
            _logger?.LogException(ex, "Failed to capture screenshot");
            return string.Empty;
        }
    }

    private byte[] AddUrlOverlay(byte[] originalPngBytes, string url)
    {
        try
        {
            using var originalBitmap = SKBitmap.Decode(originalPngBytes);
            if (originalBitmap == null)
                return originalPngBytes;

            const int barHeight = 42;
            var info = new SKImageInfo(originalBitmap.Width, originalBitmap.Height + barHeight);
            using var surface = SKSurface.Create(info);
            var canvas = surface.Canvas;

            // Draw light gray bar background
            canvas.Clear(new SKColor(245, 245, 245));

            // Draw bottom border line
            using var linePaint = new SKPaint { Color = SKColors.LightGray, StrokeWidth = 1 };
            canvas.DrawLine(0, barHeight - 1, originalBitmap.Width, barHeight - 1, linePaint);

            // Draw original screenshot below bar
            canvas.DrawBitmap(originalBitmap, 0, barHeight);

            // Draw "URL:" label in bold accent color
            using var accentPaint = new SKPaint { Color = new SKColor(0, 90, 160), IsAntialias = true };
            using var boldFont = new SKFont(SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Bold), 14);
            canvas.DrawText("URL:", 8, 28, boldFont, accentPaint);

            // Draw URL text
            using var textPaint = new SKPaint { Color = SKColors.Black, IsAntialias = true };
            using var font = new SKFont(SKTypeface.FromFamilyName("Segoe UI"), 14);
            var displayUrl = url.Length > 120 ? url.Substring(0, 117) + "..." : url;
            canvas.DrawText(displayUrl, 60, 28, font, textPaint);

            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            return data.ToArray();
        }
        catch (Exception ex)
        {
            _logger?.Debug($"URL overlay failed: {ex.Message}");
            return originalPngBytes; // fallback
        }
    }

    private async Task WaitForPageStable()
    {
        try
        {
            // Wait for network to be idle
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle, new() { Timeout =5000 });
            
            // Additional wait for any remaining animations or rendering
            await Task.Delay(1000);
            
            // Scroll to top to ensure we capture from the beginning
            await _page.EvaluateAsync("window.scrollTo(0,0)");
            await Task.Delay(500);
            
            // Wait for any lazy-loaded content
            await _page.WaitForLoadStateAsync(LoadState.DOMContentLoaded, new() { Timeout =2000 });
        }
        catch (Exception ex)
        {
            _logger?.Debug($"Page stability wait issue: {ex.Message}");
        }
    }

    private async Task<byte[]> CaptureFullPageWithScrolling()
    {
        try
        {
            // Get page dimensions
            var pageInfo = await _page.EvaluateAsync<dynamic>(@"() => { return { scrollHeight: Math.max(document.body.scrollHeight||0,document.documentElement.scrollHeight||0,document.body.offsetHeight||0,document.documentElement.offsetHeight||0,document.body.clientHeight||0,document.documentElement.clientHeight||0), clientHeight: window.innerHeight }; }");

            // If page is longer than viewport, use alternative method
                if (pageInfo.scrollHeight > pageInfo.clientHeight *1.2)
                return await CaptureWithManualScrolling(pageInfo);

            // Use standard full page screenshot
            return await _page.ScreenshotAsync(new PageScreenshotOptions { FullPage = true, Type = ScreenshotType.Png, Animations = ScreenshotAnimations.Disabled, Caret = ScreenshotCaret.Hide });
        }
        catch (Exception ex)
        {
            _logger?.Debug($"Advanced capture failed: {ex.Message}");
            return await _page.ScreenshotAsync(new PageScreenshotOptions { FullPage = true, Type = ScreenshotType.Png });
        }
    }

    private async Task<byte[]> CaptureWithManualScrolling(dynamic pageInfo)
    {
        // Save current scroll position
        var originalScroll = await _page.EvaluateAsync<dynamic>("() => ({ x: window.scrollX, y: window.scrollY })");
        
        try
        {
            // Scroll to top and wait
            await _page.EvaluateAsync("window.scrollTo(0,0)");
            await Task.Delay(400);
            
            // Force page to render all content by scrolling to bottom and back
            await _page.EvaluateAsync($"window.scrollTo(0,{pageInfo.scrollHeight})");
            await Task.Delay(800);
            
            // Scroll back to top
            await _page.EvaluateAsync("window.scrollTo(0,0)");
            await Task.Delay(400);
            
            // Take the full page screenshot
            return await _page.ScreenshotAsync(new PageScreenshotOptions { FullPage = true, Type = ScreenshotType.Png, Animations = ScreenshotAnimations.Disabled, Caret = ScreenshotCaret.Hide });
        }
        finally
        {
            // Restore original scroll position
            await _page.EvaluateAsync($"window.scrollTo({originalScroll.x},{originalScroll.y})");
        }
    }

    public async Task<string> CaptureFailureScreenshotAsync(string context, Exception? exception = null)
    {
        if (!_options.CaptureOnFailure) return string.Empty;

        // Skip low-signal or recoverable interaction errors to reduce spam
        if (exception != null && FailureScreenshotIgnorePatterns.Any(p => exception.Message?.Contains(p, StringComparison.OrdinalIgnoreCase) == true))
        {
            _logger?.Debug($"Skipping failure screenshot (recoverable). Context: {context}");
            return string.Empty;
        }

        // Throttle: only one failure screenshot per test run (global)
        lock (_failureLock)
        {
            if (_failureScreenshotsCapturedGlobal >= 1)
            {
                _logger?.Debug($"Skipping failure screenshot for context '{context}' - throttle limit reached.");
                return string.Empty;
            }
        }

        // Derive a stable test name for directory organization
        var testName = DeriveTestName(context);
        var name = $"failure_{SanitizeFileName(testName)}"; // simplified name (avoid duplicate suffixes)
        var directory = _options.BaseDirectory;
        if (_options.OrganizeByTest && !string.IsNullOrEmpty(testName))
            directory = Path.Combine(directory, FileNameUtils.SanitizeFileName(testName));
        var screenshot = await CaptureScreenshotAsync(name, directory);
        
        if (!string.IsNullOrEmpty(screenshot))
        {
            lock (_failureLock) { _failureScreenshotsCapturedGlobal++; }
            _logger?.LogUiAction("ScreenshotCapture", "Failure", $"Failure screenshot: {screenshot} - Context: {context}");
        }
        
        return screenshot;
    }

    private static string DeriveTestName(string context)
    {
        if (string.IsNullOrWhiteSpace(context)) return "unknown-test";
        // Remove leading/trailing markers that we added (failure / final) and timestamp fragments if any.
        var c = context.Trim();
        c = c.Replace("_failure", "", StringComparison.OrdinalIgnoreCase)
             .Replace("failure_", "", StringComparison.OrdinalIgnoreCase)
             .Replace("_final", "", StringComparison.OrdinalIgnoreCase)
             .Replace("final_", "", StringComparison.OrdinalIgnoreCase);
        // If underscores remain used for segments, keep as is.
        return string.IsNullOrWhiteSpace(c) ? "unknown-test" : c;
    }

    public async Task<string> CaptureTestScreenshotAsync(string? testName = null)
    {
        testName ??= "test";

        var directory = _options.OrganizeByTest && !string.IsNullOrEmpty(testName) ? Path.Combine(_options.BaseDirectory, FileNameUtils.SanitizeFileName(testName)) : _options.BaseDirectory;

        // Always capture screenshots - this method is called during test cleanup
        return await CaptureScreenshotAsync(testName, directory);
    }

    public async Task<byte[]> CaptureScreenshotBytesAsync(string? name = null)
    {
        try
        {
            if (_page?.IsClosed != false)
            {
                _logger?.Warning("Cannot capture screenshot bytes - page is not available");
                return [];
            }
            await WaitForPageStable();
            var screenshotBytes = await CaptureFullPageWithScrolling();
            if (screenshotBytes.Length == 0)
            {
                _logger?.Warning("Screenshot appears to be empty");
                return [];
            }
            if (_options.IncludeUrlOverlay)
                screenshotBytes = AddUrlOverlay(screenshotBytes, _page.Url);
            _logger?.Trace($"Screenshot captured in memory ({screenshotBytes.Length} bytes)");
            return screenshotBytes;
        }
        catch (Exception ex)
        {
            _logger?.LogException(ex, "Failed to capture screenshot bytes");
            return [];
        }
    }

    public async Task<byte[]> CaptureDomSnapshotBytesAsync(string? name = null)
    {
        try
        {
            if (_page?.IsClosed != false)
            {
                _logger?.Warning("Cannot capture DOM snapshot bytes - page is not available");
                return [];
            }
            var html = await _page.ContentAsync();
            if (string.IsNullOrEmpty(html))
            {
                _logger?.Warning("DOM snapshot is empty");
                return [];
            }
            var bytes = System.Text.Encoding.UTF8.GetBytes(html);
            _logger?.Trace($"DOM snapshot captured in memory ({bytes.Length} bytes)");
            return bytes;
        }
        catch (Exception ex)
        {
            _logger?.LogException(ex, "Failed to capture DOM snapshot bytes");
            return [];
        }
    }

    public async Task<string> CaptureDomSnapshotAsync(string? name = null, string? directory = null)
    {
        try
        {
            if (_page?.IsClosed != false)
            {
                _logger?.Warning("Cannot capture DOM snapshot - page is not available");
                return string.Empty;
            }

            var html = await _page.ContentAsync();
            if (string.IsNullOrEmpty(html))
            {
                _logger?.Warning("DOM snapshot is empty, skipping save");
                return string.Empty;
            }

            name ??= "dom_snapshot";
            var timestamp = DateTime.Now.ToString(_options.TimestampFormat);
            var fileName = $"{name}_{timestamp}.html";
            var targetDirectory = directory ?? _options.BaseDirectory;
            Directory.CreateDirectory(targetDirectory);
            var fullPath = Path.Combine(targetDirectory, fileName);

            await File.WriteAllTextAsync(fullPath, html);
            _logger?.Trace($"DOM snapshot captured: {fullPath}");
            return fullPath;
        }
        catch (Exception ex)
        {
            _logger?.LogException(ex, "Failed to capture DOM snapshot");
            return string.Empty;
        }
    }

    public void OpenScreenshotDirectory(string? testName = null)
    {
        try
        {
            var directory = _options.OrganizeByTest && !string.IsNullOrEmpty(testName)
                ? Path.Combine(_options.BaseDirectory, FileNameUtils.SanitizeFileName(testName))
                : _options.BaseDirectory;

            if (!Directory.Exists(directory)) return;

            // Avoid trying to open directory in headless CI environments unless explicitly requested via env var
            var allowUi = System.Environment.GetEnvironmentVariable("ALLOW_SCREENSHOT_DIRECTORY_OPEN");
            if (string.IsNullOrEmpty(allowUi))
            {
                _logger?.Debug("Skipping directory open (headless or CI environment detected)");
                return;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = directory, UseShellExecute = true });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                System.Diagnostics.Process.Start("xdg-open", directory);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                System.Diagnostics.Process.Start("open", directory);
            }
            else
            {
                _logger?.Debug("Unsupported OS for auto-open; skipping");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogException(ex, "Failed to open screenshot directory");
        }
    }

    public List<string> GetAllScreenshots(string? testName = null)
    {
        try
        {
            var directory = _options.OrganizeByTest && !string.IsNullOrEmpty(testName) ? Path.Combine(_options.BaseDirectory, FileNameUtils.SanitizeFileName(testName)) : _options.BaseDirectory;
            if (Directory.Exists(directory))
                return Directory.GetFiles(directory, "*.png", SearchOption.AllDirectories).ToList();
        }
        catch (Exception ex)
        {
            _logger?.LogException(ex, "Failed to get screenshot files");
        }
        return new List<string>();
    }

    private static string SanitizeFileName(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "unknown";
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(input.Where(c => !invalidChars.Contains(c)).ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? "unknown" : sanitized;
    }
}