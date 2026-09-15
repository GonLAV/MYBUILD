using Automation.Configuration.FrontEnds;
using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;
using Microsoft.Playwright;
using static Automation.Configuration.FrontEnds.BrowserOptions;

namespace Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;

public class PlaywrightDriverInitializer : IPlaywrightDriverInitializer, IDisposable
{
    public const float DEFAULT_TIMEOUT = 30f;
    private IPlaywright? _playwright;
    private bool _disposed;

    public async Task<IBrowser> GetBrowserDriverAsync(BrowserTypeEnum browserType, BrowserOptions browserOptions)
    {
        if (_playwright == null)
        {
            _playwright = await Playwright.CreateAsync().ConfigureAwait(false);
        }

        // Use the adapter to convert BrowserOptions to Playwright options
        var options = browserOptions.ToBrowserTypeLaunchOptions();

        return await GetBrowserAsync(browserType, options);
    }

    private async Task<IBrowser> GetBrowserAsync(BrowserTypeEnum driverType, BrowserTypeLaunchOptions options)
    {
        if (_playwright == null)
        {
            _playwright = await Playwright.CreateAsync().ConfigureAwait(false);
        }

        var browserType = driverType.ToString().ToLower();
        return await _playwright[browserType].LaunchAsync(options);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _playwright?.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
