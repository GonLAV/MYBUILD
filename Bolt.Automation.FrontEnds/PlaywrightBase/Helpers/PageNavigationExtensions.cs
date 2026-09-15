using Bolt.Automation.Common.Utils;
using Microsoft.Playwright;

namespace Bolt.Automation.FrontEnds.PlaywrightBase.Helpers
{
    public static class PageNavigationExtensions
    {
        public static async Task WaitForPageReadyAsync(this IPage page)
        {
            if (page == null || page.IsClosed)
                return;

            try
            {
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                await Task.Delay(500); 
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error waiting for page ready: {ex.Message}");
            }
        }

        public static async Task NavigateToAsync(this IPage page, string url, int maxRetries = 3)
        {
            await RetryHelper.RunWithRetryAsync(
                async () =>
                {
                    await page.GotoAsync(url);
                    await page.WaitForPageReadyAsync();
                    Console.WriteLine($"Successfully navigated to: {url}");
                },
                maxRetries,
                1000,
                (ex, attempt) => Console.WriteLine($"Navigation attempt {attempt} failed: {ex.Message}")
            );
        }
    }
}
