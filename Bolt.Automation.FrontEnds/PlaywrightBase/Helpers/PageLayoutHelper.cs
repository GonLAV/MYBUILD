using System.Diagnostics;
using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.FormData.Base;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Microsoft.Playwright;

namespace Bolt.Automation.FrontEnds.PlaywrightBase.Helpers
{
    /// <summary>
    /// An element's on-screen rectangle, in CSS pixels relative to the top of the document.
    /// </summary>
    public sealed record ElementBox(double X, double Y, double Width, double Height)
    {
        public double Right => X + Width;
        public double Bottom => Y + Height;

        /// <summary>True when this box starts below the other box ends — i.e. it renders underneath it.</summary>
        public bool IsBelow(ElementBox other) => Y >= other.Bottom;

        /// <summary>True when this box starts right of where the other box ends.</summary>
        public bool IsRightOf(ElementBox other) => X >= other.Right;

        public override string ToString() => $"x={X:0} y={Y:0} w={Width:0} h={Height:0}";
    }

    /// <summary>
    /// Reads what a page actually renders — visible text, element geometry and the resulting
    /// top-to-bottom order — so tests can check page composition and component placement rather
    /// than only field behaviour. Product-agnostic: takes CSS selectors, and resolves field names
    /// through the registry for the current front end. Every method returns what it observed and
    /// none of them assert; the pass/fail decision stays in the test.
    /// </summary>
    public class PageLayoutHelper(
        IBrowserManager browserManager,
        IScopeContext? scopeContext = null,
        IAutomationLogger? logger = null)
    {
        /// <summary>How long to give the page to render an element before treating it as absent.</summary>
        private const int RenderTimeoutMs = ElementResolver.RenderTimeoutMs;

        /// <summary>
        /// Budget for a single candidate locator of a registry field. Short, because several are
        /// tried in turn and only one is expected to apply to this front end.
        /// </summary>
        private const int CandidateLocatorTimeoutMs = 1_000;

        /// <summary>Trimmed visible text of the element, or empty when it is not on the page.</summary>
        public async Task<string> GetText(string cssSelector)
        {
            var locator = await Resolve(cssSelector);
            if (locator is null)
            {
                logger?.Debug($"No element matched '{cssSelector}' — no text to read");
                return string.Empty;
            }

            var text = (await locator.TextContentAsync() ?? string.Empty).Trim();
            logger?.Debug($"Text of '{cssSelector}': [{text}]");
            return text;
        }

        /// <summary>
        /// Every element that does not carry the text it should, described so the message reads on
        /// its own. Empty means the page says exactly what was expected of it. An element that never
        /// renders reads as empty, so it is reported here too rather than passing silently.
        /// </summary>
        public async Task<IReadOnlyList<string>> GetTextMismatches(IReadOnlyDictionary<string, string> expectedBySelector)
        {
            var mismatches = new List<string>();
            foreach (var (selector, expected) in expectedBySelector)
            {
                var actual = await GetText(selector);
                if (actual != expected)
                    mismatches.Add($"'{selector}' should read [{expected}] but reads [{actual}]");
            }

            logger?.Info(mismatches.Count == 0
                ? $"All {expectedBySelector.Count} expected texts matched"
                : $"Text mismatches: {string.Join("; ", mismatches)}");
            return mismatches;
        }

        /// <summary>
        /// The element's rectangle, or <c>null</c> when it is absent or not rendered. Playwright
        /// reports no box for a hidden element, so a non-null box also means "visible to the user".
        /// </summary>
        public async Task<ElementBox?> GetBox(string cssSelector)
        {
            var locator = await Resolve(cssSelector);
            if (locator is null)
            {
                logger?.Debug($"No element matched '{cssSelector}' — no box to measure");
                return null;
            }

            var bounds = await locator.BoundingBoxAsync();
            if (bounds is null)
            {
                logger?.Debug($"Element '{cssSelector}' stopped being rendered while it was measured");
                return null;
            }

            var box = new ElementBox(bounds.X, bounds.Y, bounds.Width, bounds.Height);
            logger?.Debug($"Box of '{cssSelector}': {box}");
            return box;
        }

        /// <summary>The rectangle of a registry field, resolved by field name.</summary>
        public async Task<ElementBox?> GetFieldBox(string fieldName)
        {
            var selector = await FieldSelector(fieldName);
            return selector is null ? null : await GetBox(selector);
        }

        /// <summary>
        /// The given fields ordered the way the user reads them, top of the page down. Fields that
        /// are not rendered are left out, so a short list is itself the signal that one is missing.
        /// </summary>
        public async Task<IReadOnlyList<string>> GetFieldsTopToBottom(params string[] fieldNames)
        {
            var measured = new List<(string Name, double Top)>();
            foreach (var fieldName in fieldNames)
            {
                var box = await GetFieldBox(fieldName);
                if (box is null)
                {
                    logger?.Warning($"Field '{fieldName}' is not rendered — leaving it out of the reading order");
                    continue;
                }
                measured.Add((fieldName, box.Y));
            }

            var order = measured.OrderBy(m => m.Top).Select(m => m.Name).ToList();
            logger?.Info($"Fields top-to-bottom: {string.Join(" -> ", order)}");
            return order;
        }

        /// <summary>
        /// Every place the page fails to stack the given selectors in the order they were listed,
        /// each described so the message reads on its own. An empty list means the page reads
        /// top-to-bottom exactly as asked; an element that is not rendered is a violation in its own
        /// right, so one call covers both "is it there" and "is it in the right place".
        /// </summary>
        public async Task<IReadOnlyList<string>> GetStackViolations(params string[] cssSelectors)
        {
            var violations = new List<string>();
            ElementBox? above = null;
            var aboveSelector = string.Empty;

            foreach (var selector in cssSelectors)
            {
                var box = await GetBox(selector);
                if (box is null)
                {
                    violations.Add($"'{selector}' is not rendered");
                    continue;
                }

                if (above is not null && !box.IsBelow(above))
                    violations.Add($"'{selector}' ({box}) does not render below '{aboveSelector}' ({above})");

                // The last element that was actually measured anchors the next comparison, so a
                // missing one costs a single violation instead of cascading into the rest.
                above = box;
                aboveSelector = selector;
            }

            logger?.Info(violations.Count == 0
                ? $"Stacked top-to-bottom as asked: {string.Join(" -> ", cssSelectors)}"
                : $"Stacking violations: {string.Join("; ", violations)}");
            return violations;
        }

        /// <summary>
        /// First locator of the registry field that is actually on the page, or null when none match.
        /// A field may declare several locators for the same control across front ends, so the live
        /// page decides which one applies here.
        /// </summary>
        private async Task<string?> FieldSelector(string fieldName)
        {
            var registry = ProjectContextManager.GetFieldRegistry(scopeContext);
            if (!registry.TryGetValue(fieldName, out var field))
                throw new KeyNotFoundException($"Field '{fieldName}' is not in the field registry for the current front end.");

            var page = await browserManager.GetPageAsync();

            // Each candidate only gets a short wait, so the whole set is retried until the overall
            // render budget runs out — otherwise a field that renders late is reported as missing
            // just because it was not there while its own locator was being tried.
            var elapsed = Stopwatch.StartNew();
            do
            {
                foreach (var selector in field.Locators)
                {
                    if (await IsAttached(page, selector, CandidateLocatorTimeoutMs))
                        return selector;
                }
            }
            while (elapsed.ElapsedMilliseconds < RenderTimeoutMs);

            // LocatorSet converts implicitly to string, so the IEnumerable overload needs to be spelled out.
            logger?.Debug($"None of field '{fieldName}' locators [{string.Join(", ", (IEnumerable<string>)field.Locators)}] matched the page");
            return null;
        }

        /// <summary>
        /// The single element the selector matches once the page has rendered it, or null when it
        /// never appears within the render budget.
        /// </summary>
        private Task<ILocator?> Resolve(string cssSelector)
            => ElementResolver.ResolveVisible(browserManager, cssSelector, RenderTimeoutMs);

        /// <summary>True when the selector is on the page within the given budget.</summary>
        private static async Task<bool> IsAttached(IPage page, string selector, int timeoutMs)
        {
            try
            {
                await page.Locator(selector).First.WaitForAsync(new LocatorWaitForOptions
                {
                    State = WaitForSelectorState.Attached,
                    Timeout = timeoutMs
                });
                return true;
            }
            catch (TimeoutException)
            {
                return false;
            }
        }
    }
}
