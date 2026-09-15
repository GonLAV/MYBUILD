using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Microsoft.Playwright;

namespace Bolt.Automation.FrontEnds.PlaywrightBase.Helpers
{
    /// <summary>
    /// Turns a CSS selector into the element it matches once the page has rendered it. Shared by the
    /// read-only page helpers, which all need the same thing: waiting for an element to be visible —
    /// rather than taking a count — is what keeps a read taken right after navigation from seeing an
    /// empty page.
    /// </summary>
    internal static class ElementResolver
    {
        /// <summary>
        /// How long to give the page to render an element before treating it as absent. The
        /// page-ready check passes on DOM-content-loaded plus URL, which a single-page app reaches
        /// before it has painted its content — so an element has to be waited for, not counted.
        /// </summary>
        internal const int RenderTimeoutMs = 10_000;

        /// <summary>
        /// The single element the selector matches once it is visible, or null when it never appears
        /// within the render budget.
        /// </summary>
        internal static async Task<ILocator?> ResolveVisible(
            IBrowserManager browserManager, string cssSelector, int timeoutMs = RenderTimeoutMs)
        {
            var page = await browserManager.GetPageAsync();
            var locator = page.Locator(cssSelector).First;
            try
            {
                await locator.WaitForAsync(new LocatorWaitForOptions
                {
                    State = WaitForSelectorState.Visible,
                    Timeout = timeoutMs
                });
                return locator;
            }
            catch (TimeoutException)
            {
                return null;
            }
        }
    }
}
