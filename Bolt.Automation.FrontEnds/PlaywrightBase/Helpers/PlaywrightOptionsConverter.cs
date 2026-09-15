using Automation.Configuration.FrontEnds;
using Microsoft.Playwright;

namespace Bolt.Automation.FrontEnds.PlaywrightBase.Helpers
{
    public static class PlaywrightOptionsAdapter
    {
        /// <summary>
        /// Converts BrowserOptions to Playwright's BrowserTypeLaunchOptions
        /// </summary>
        public static BrowserTypeLaunchOptions ToBrowserTypeLaunchOptions(this BrowserOptions options)
        {
            var launchArgs = options.Args?.ToList() ?? [];
            bool effectiveHeadless = !BrowserOptions.IsDebuggerAttached && (options.Headless ?? true);

            if (options.Maximize && !effectiveHeadless)
            {
                // --start-maximized only works in headed mode
                launchArgs.Add("--start-maximized");
            }
            else if (options.WindowSize is { Width: > 0, Height: > 0 })
            {
                launchArgs.Add($"--window-size={options.WindowSize.Width},{options.WindowSize.Height}");
            }

            // Add cache-busting args for consistent testing behavior
            if (options.IsDebugMode)
            {
                launchArgs.Add("--disable-web-security");
                launchArgs.Add("--disable-features=VizDisplayCompositor");
                launchArgs.Add("--no-sandbox");
                launchArgs.Add("--disable-http-cache");
                launchArgs.Add("--disable-cache");
                launchArgs.Add("--disable-application-cache");
            }

            if (effectiveHeadless || BrowserOptions.IsDebuggerAttached)
            {
                launchArgs.Add("--disable-blink-features=AutomationControlled");
            }

            return new BrowserTypeLaunchOptions
            {
                Headless = effectiveHeadless,
                SlowMo = options.SlowMo,
                Timeout = options.Timeout,
                Args = launchArgs.Count > 0 ? launchArgs.ToArray() : null,
                Channel = options.Channel ?? options.GetBrowserChannel()
            };
        }

        /// <summary>
        /// Converts BrowserOptions to Playwright's BrowserNewContextOptions
        /// </summary>
        public static BrowserNewContextOptions ToBrowserNewContextOptions(this BrowserOptions options)
        {
            var contextOptions = new BrowserNewContextOptions
            {
                IgnoreHTTPSErrors = options.IgnoreHTTPSErrors,
                JavaScriptEnabled = options.JavaScriptEnabled,
                AcceptDownloads = options.AcceptDownloads,
                UserAgent = options.UserAgent
            };

            // Add extra HTTP headers to control caching behavior
            if (options.IsDebugMode)
            {
                contextOptions.ExtraHTTPHeaders = new Dictionary<string, string>
                {
                    ["Cache-Control"] = "no-cache, no-store, must-revalidate",
                    ["Pragma"] = "no-cache",
                    ["Expires"] = "0"
                };
            }

            bool effectiveHeadless = !BrowserOptions.IsDebuggerAttached && (options.Headless ?? true);

            if (options.Maximize && !effectiveHeadless)
            {
                // Headed + maximize: use NoViewport so the viewport fills the maximized window
                contextOptions.ViewportSize = ViewportSize.NoViewport;
            }
            else if (options.ViewportSize.Width > 0 && options.ViewportSize.Height > 0)
            {
                // Explicit viewport dimensions provided
                contextOptions.ViewportSize = new ViewportSize
                {
                    Width = options.ViewportSize.Width,
                    Height = options.ViewportSize.Height
                };
            }
            else
            {
                // Headless + maximize, or no viewport specified: use a standard full-HD viewport
                contextOptions.ViewportSize = new ViewportSize
                {
                    Width = 1920,
                    Height = 1080
                };
            }

            if (effectiveHeadless && string.IsNullOrEmpty(options.UserAgent))
            {
                contextOptions.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36";
            }

            return contextOptions;
        }
    }
}