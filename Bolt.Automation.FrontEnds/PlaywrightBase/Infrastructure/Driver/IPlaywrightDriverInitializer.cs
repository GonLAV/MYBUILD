using Automation.Configuration.FrontEnds;
using Microsoft.Playwright;
using static Automation.Configuration.FrontEnds.BrowserOptions;

namespace Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;

public interface IPlaywrightDriverInitializer : IDisposable
{
    Task<IBrowser> GetBrowserDriverAsync(BrowserTypeEnum browserType, BrowserOptions browserOptions);
}