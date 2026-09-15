using Automation.Configuration.FrontEnds;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Screenshot;
using Microsoft.Playwright;

namespace Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;

public interface IBrowserManager
{
    event Action<IPage>? PageChanged;
    Task<IPage> GetPageAsync();
    Task<IBrowser> GetBrowserAsync();
    Task<IBrowserContext> GetContextAsync();
    Task NavigateAsync(string url, string? targetPageName = null,int timeout = 30000);
    Task TakeScreenshotAsync(string path);
    Task OpenNewTabAsync(string url = "");
    Task OpenNewWindowAsync(string url = "");
    Task SwitchToFirstTabAsync();
    //Task SwitchToLastTabAsync();
    Task SwitchToLastTabAsync(int maxWaitMs = 5000, int pollIntervalMs = 100, int urlSettleTimeoutMs = 3000);
    Task CloseLastTabAsync();
    Task CloseCurrentTabAsync();
    IReadOnlyList<IPage> GetAllWindows();
    Task<IReadOnlyList<IPage>> GetAllWindowsAsync();
    IPage? GetCurrentTab();
    void Configure(BrowserOptions settings);    
    IScreenshotManager? GetScreenshotManager();
}