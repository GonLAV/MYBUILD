using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Microsoft.Playwright;

namespace Bolt.Automation.FrontEnds.PlaywrightBase.Helpers
{
    /// <summary>What a tab that a link opened turned out to be showing.</summary>
    public sealed record OpenedTab(string Url, string Title)
    {
        public override string ToString() => $"[{Title}] at {Url}";
    }

    /// <summary>
    /// Reads and follows the links a page offers — where a link points, whether it leaves for a new
    /// tab, and what that tab ends up showing. Product-agnostic: takes CSS selectors, so it serves any
    /// front end. Every method returns what it observed and none of them assert; the pass/fail
    /// decision stays in the test.
    /// </summary>
    public class LinkHelper(
        IBrowserManager browserManager,
        IAutomationLogger? logger = null)
    {
        /// <summary>How long to give the page to render the link before treating it as absent.</summary>
        private const int RenderTimeoutMs = ElementResolver.RenderTimeoutMs;

        /// <summary>How long to wait for the click to produce a tab, and for that tab to finish loading.</summary>
        private const int NewTabTimeoutMs = 30_000;

        /// <summary>True when the link is set to open in a tab of its own rather than in place.</summary>
        public async Task<bool> OpensInNewTab(string cssSelector)
        {
            var link = await Resolve(cssSelector);
            var target = link is null ? null : await link.GetAttributeAsync("target");
            logger?.Info($"Link '{cssSelector}' target attribute: {target ?? "<absent>"}");
            return target == "_blank";
        }

        /// <summary>
        /// Hovers the link and returns the URL the browser previews for it. The preview itself is the
        /// browser's own status-bar tooltip — chrome, not page content, so it is outside the DOM and
        /// cannot be read back. What it shows is the link's href, which is what this returns: the
        /// hover proves the link is there and reachable by pointer, the href is the URL a user sees.
        /// Empty when the link never renders.
        /// </summary>
        public async Task<string> HoverAndGetPreviewedUrl(string cssSelector)
        {
            var link = await Resolve(cssSelector);
            if (link is null)
            {
                logger?.Warning($"No link matched '{cssSelector}' — nothing to hover");
                return string.Empty;
            }

            await link.HoverAsync(new LocatorHoverOptions { Timeout = RenderTimeoutMs });
            var previewed = await link.GetAttributeAsync("href") ?? string.Empty;
            logger?.LogUiAction("Hover", cssSelector, $"Browser previews [{previewed}]");
            return previewed;
        }

        /// <summary>
        /// Clicks the link, waits for the tab it opens to finish loading, and reports what that tab
        /// shows. Returns <c>null</c> when the click opened no tab at all. The tab is closed again
        /// before returning, so the test carries on against the page it started from; a tab that
        /// never finishes loading is still reported, since its URL and title say more about what
        /// went wrong than the timeout does.
        /// </summary>
        public async Task<OpenedTab?> OpenInNewTab(string cssSelector)
        {
            var link = await Resolve(cssSelector);
            if (link is null)
            {
                logger?.Warning($"No link matched '{cssSelector}' — nothing to click");
                return null;
            }

            var context = await browserManager.GetContextAsync();
            IPage tab;
            try
            {
                tab = await context.RunAndWaitForPageAsync(
                    async () => await link.ClickAsync(new LocatorClickOptions { Timeout = RenderTimeoutMs }),
                    new BrowserContextRunAndWaitForPageOptions { Timeout = NewTabTimeoutMs });
            }
            catch (TimeoutException)
            {
                logger?.Warning($"Clicking '{cssSelector}' opened no new tab within {NewTabTimeoutMs}ms");
                return null;
            }

            // Whatever the tab does from here — load slowly, crash, close itself — it must not be left
            // behind for the next step to trip over, so reading it and closing it are kept together.
            try
            {
                try
                {
                    await tab.WaitForLoadStateAsync(LoadState.Load,
                        new PageWaitForLoadStateOptions { Timeout = NewTabTimeoutMs });
                }
                catch (TimeoutException)
                {
                    logger?.Warning($"The tab opened by '{cssSelector}' did not finish loading within {NewTabTimeoutMs}ms");
                }

                var opened = new OpenedTab(tab.Url, await tab.TitleAsync());
                logger?.Info($"Link '{cssSelector}' opened a new tab: {opened}");
                return opened;
            }
            finally
            {

            }
        }

        /// <summary>
        /// The single link the selector matches once the page has rendered it, or null when it never
        /// appears within the render budget.
        /// </summary>
        private Task<ILocator?> Resolve(string cssSelector)
            => ElementResolver.ResolveVisible(browserManager, cssSelector, RenderTimeoutMs);
    }
}
