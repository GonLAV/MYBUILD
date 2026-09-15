using Microsoft.Playwright;

namespace Bolt.Automation.FrontEnds.FormData.Helpers
{
    public static class ScreenshotHelper
    {
        private const string DefaultDirectory = "Screenshots";

        public static async Task<string> TakeScreenshotAsync(
            IPage page,
            string? directory = null,
            string? name = null)
        {
            if (page == null) throw new ArgumentNullException(nameof(page));
            directory ??= DefaultDirectory;
            name ??= "Test";
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            Directory.CreateDirectory(directory);
            string screenshotPath = Path.Combine(directory, $"{name}_screenshot_{timestamp}.png");
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = screenshotPath, FullPage = true });
            return screenshotPath;
        }
    }
}
