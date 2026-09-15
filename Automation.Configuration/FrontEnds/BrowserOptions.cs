using System.Diagnostics;
using System.Text.Json;

namespace Automation.Configuration.FrontEnds;

public class BrowserOptions
{
    public const string ConfigSection = "Browser";
    public enum BrowserTypeEnum
    {
        Chrome,
        Chromium,
        Firefox,
        Edge,
        Webkit
    }
    
    public ViewportSizeOptions ViewportSize { get; set; } = new ViewportSizeOptions();
    public float? SlowMo { get; set; }
    public float Timeout { get; set; }
    public BrowserTypeEnum BrowserType { get; set; } = BrowserTypeEnum.Chromium;
    public bool? Headless { get; set; }
    public string? ApplicationUrl { get; set; }
    public bool Maximize { get; set; }
    public string? Channel { get; set; }
    public WindowSizeOptions WindowSize { get; set; } = new WindowSizeOptions();
    public string[]? Args { get; set; }
    public Dictionary<string, object> TestContext { get; set; } = new Dictionary<string, object>();
    public bool IgnoreHTTPSErrors { get; set; }
    public bool JavaScriptEnabled { get; set; }
    public bool AcceptDownloads { get; set; }
    public string? UserAgent { get; set; }
    public bool IsDebugMode { get; set; }
    
    public ScreenshotConfig Screenshots { get; set; } = new ScreenshotConfig();

    public static bool IsDebuggerAttached => Debugger.IsAttached;

    public class ViewportSizeOptions
    {
        public int Width { get; set; }
        public int Height { get; set; }
    }

    public class WindowSizeOptions
    {
        public int Width { get; set; }
        public int Height { get; set; }
    }

    public class ScreenshotConfig
    {
        public string BaseDirectory { get; set; } = "Screenshots";

        public bool CaptureOnFailure { get; set; } = true;

        public bool OrganizeByTest { get; set; } = true;
    }

    public string GetBrowserChannel() => BrowserType switch
    {
        BrowserTypeEnum.Chrome => "chrome",
        BrowserTypeEnum.Edge => "msedge",
        BrowserTypeEnum.Firefox => "firefox",
        BrowserTypeEnum.Webkit => "webkit",
        BrowserTypeEnum.Chromium => "",  // Chromium is Playwright's default; no channel needed
        _ => string.Empty
    };

    public BrowserOptions Clone()
    {
        var json = JsonSerializer.Serialize(this);
        return JsonSerializer.Deserialize<BrowserOptions>(json) ?? throw new InvalidOperationException("Cloning failed");
    }
}