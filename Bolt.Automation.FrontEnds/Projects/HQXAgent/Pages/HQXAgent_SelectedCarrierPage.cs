using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Exceptions;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.FormData.Helpers;
using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.HQXAgent.Base;
using Bolt.Automation.FrontEnds.Projects.HQXAgent.FormData.FieldsRegistry;
using Microsoft.Playwright;
using static Bolt.Automation.FrontEnds.Projects.HQXAgent.FormData.FieldNamesHQXAgent;
namespace Bolt.Automation.FrontEnds.Projects.HQXAgent.Pages
{
    public class HQXAgent_SelectedCarrierPage(IBrowserManager browserManager,
        IPageHelper pageHelper,
        IScopeContext scopeContext,
        bool validatePageReady = true,
        IAutomationLogger? logger = null)
        : HQXAgentBase(browserManager, pageHelper, scopeContext, validatePageReady, logger)
    {

        protected override string PageIdentifier => "SelectedCarrier";
        protected override string PageName => "PGR Selected Carrier Page";
        protected override int PageTimeout => 120000;

        private const string ViewRatesLocator = "//button[contains(@class,'toggle-view-rates')]";

        // The "View rates" toggle expands #rates-comparison-panel, which holds the coverages.
        private const string RatesComparisonPanelLocator = "#rates-comparison-panel";

        // Inside the panel each coverage name is a <span class="coverage-title">. The panel
        // renders two responsive layouts (comparison-table mobile-tablet + comparison-table
        // desktop) so every title appears twice — scoping to the panel and selecting the span
        // directly matches both layouts; the reader dedupes. Confirmed against the live QA E&S
        // DOM (ADO TC 246475).
        private const string CoverageTitleLocator = "#rates-comparison-panel span.coverage-title";

        // The E&S "key points" box (<app-key-points>) renders section headings as <p> and each
        // note as an <li>. The selected carrier's name is read from the selected rate-head cell's
        // logo title inside the rates panel (th.rate-head.selected svg[title], e.g. "Progressive by
        // BambooSurplus Logo") — the same source HQXConsumer's CarrierSelectionService uses. The
        // page's header logo (.carrier-selected-logo-container) shows a placeholder carrier until
        // the E&S rate resolves, so it is NOT used here.
        private const string KeyPointsTextLocator = "app-key-points .info-box p, app-key-points .info-box li";
        private const string CarrierLogoLocator = "#rates-comparison-panel th.rate-head.selected svg[title]";

        // The "Important Notes on E&S Homeowners" heading renders only once the E&S offer has
        // loaded — the definitive signal that "Get E&S rates" took effect.
        private const string EsLoadedSignalLocator = "app-key-points .info-box p:has-text(\"Important Notes on E&S\")";

        // The "Get E&S HO rates" button lives in the Selected Carrier footer while the page shows
        // the standard admitted-carrier results. Clicking it computes the E&S result asynchronously;
        // the button stays present until that computation completes, so its detachment is the
        // definitive "E&S processed" signal (regardless of whether E&S quoted or declined). Button
        // presence is otherwise checked via the GetESRates / GetDFRates field-registry entries.
        private const string GetESRatesButtonLocator = "button.get-es-rates-btn";

        // The "Not quoted" (<rate-unquoted-carriers>) block lists each declined carrier's logo plus
        // its reason in <ul class="carrier-messages"><li>. Reconciled against the live QA decline
        // DOM (ADO TC 246473).
        private const string NotQuotedSectionLocator = "rate-unquoted-carriers";
        private const string NotQuotedReasonLocator = "rate-unquoted-carriers .carrier-messages li";

        public override async Task FillForm(Dictionary<string, string> formData = null)
        {
            var (pageSpecificData, fields) = FormDataHelper.GetFieldsForPage(this, ScopeContext, formData);
            await Page.FillRelevantFields(pageSpecificData, fields, ScopeContext);
        }

        private async Task ClickOnViewCoverageButton()
        {
            await PageHelper.InteractWithElement(
                LocatorType.XPath,
                ViewRatesLocator,
                ElementAction.Click,
                new ElementInteractionOptions { IgnoreIfNotFound = true }
            );
        }

        /// <summary>
        /// Expands the "View rates" panel and waits for it to become visible with the selected
        /// carrier's rate rendered. The panel starts <c>hidden</c> and the toggle carries
        /// <c>aria-expanded</c>; this clicks only when collapsed (clicking an open toggle would
        /// collapse it), then waits for the panel to be visible — the reliable "E&amp;S offer is
        /// ready" signal that gates reading the carrier name / coverages.
        /// </summary>
        public async Task ClickViewRatesAsync()
        {
            var toggle = Page.Locator(ViewRatesLocator);
            var expanded = await toggle.GetAttributeAsync("aria-expanded");
            if (string.Equals(expanded, "true", StringComparison.OrdinalIgnoreCase))
            {
                _logger?.Info("'View rates' panel is already expanded — leaving it open.");
            }
            else
            {
                _logger?.Info("Clicking the 'View rates' toggle to expand the coverages panel.");
                await ClickOnViewCoverageButton();
            }

            await Page.Locator(RatesComparisonPanelLocator).WaitForAsync(
                new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = PageTimeout });
        }

        /// <summary>
        /// Reads the coverage titles rendered in the expanded rate panel (after
        /// <see cref="ClickViewRatesAsync"/>). Returns the distinct, trimmed display titles so the
        /// caller can validate the displayed coverages against an expected set.
        /// </summary>
        public async Task<List<string>> GetDisplayedCoverageTitlesAsync()
        {
            var nodes = Page.Locator(CoverageTitleLocator);
            var titles = new List<string>();

            for (int i = 0, count = await nodes.CountAsync(); i < count; i++)
            {
                var text = (await nodes.Nth(i).InnerTextAsync())?.Trim();
                if (!string.IsNullOrWhiteSpace(text))
                    titles.Add(text);
            }

            var distinct = titles.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            _logger?.Info($"Displayed coverage titles ({distinct.Count}): {string.Join(", ", distinct)}");
            return distinct;
        }

        /// <summary>
        /// Reads the text lines shown in the E&amp;S "key points" box — the section headings and
        /// every note bullet, in document order, trimmed with inner whitespace collapsed. Lets the
        /// caller validate the informational copy displayed for the E&amp;S offering.
        /// </summary>
        public async Task<List<string>> GetKeyPointsTextLinesAsync()
        {
            var nodes = Page.Locator(KeyPointsTextLocator);
            var lines = new List<string>();

            for (int i = 0, count = await nodes.CountAsync(); i < count; i++)
            {
                var text = (await nodes.Nth(i).InnerTextAsync())?.Trim();
                if (string.IsNullOrWhiteSpace(text))
                    continue;
                // Collapse runs of internal whitespace/newlines so the copy compares cleanly.
                text = string.Join(" ", text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
                lines.Add(text);
            }

            _logger?.Info($"Key-points text lines ({lines.Count}): {string.Join(" | ", lines)}");
            return lines;
        }

        /// <summary>
        /// Returns the selected carrier's display name taken from the co-branded logo's title/alt
        /// (e.g. "Progressive by BambooSurplus Logo"), trimmed of the trailing " Logo" suffix.
        /// </summary>
        public async Task<string> GetSelectedCarrierNameAsync()
        {
            var title = await Page.Locator(CarrierLogoLocator).First.GetAttributeAsync("title");
            var name = (title ?? string.Empty).Replace(" Logo", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
            _logger?.Info($"Selected carrier name: '{name}'");
            return name;
        }

        /// <summary>
        /// Reads the displayed coverage titles and returns the symmetric difference against
        /// <paramref name="expectedTitles"/> — an empty (Missing, Extra) means the displayed
        /// coverages match. The caller supplies the LOB-specific expected set and asserts.
        /// </summary>
        public async Task<(List<string> Missing, List<string> Extra)> GetCoverageTitlesDiffAsync(IEnumerable<string> expectedTitles)
        {
            var actual = await GetDisplayedCoverageTitlesAsync();
            return Diff("Coverages", expectedTitles, actual, ", ");
        }

        /// <summary>
        /// Reads the key-points text lines and returns the symmetric difference against
        /// <paramref name="expectedLines"/> — an empty (Missing, Extra) means the displayed copy
        /// matches. The caller supplies the carrier-specific expected set and asserts.
        /// </summary>
        public async Task<(List<string> Missing, List<string> Extra)> GetKeyPointsDiffAsync(IEnumerable<string> expectedLines)
        {
            var actual = await GetKeyPointsTextLinesAsync();
            return Diff("Key points", expectedLines, actual, " | ");
        }

        private (List<string> Missing, List<string> Extra) Diff(
            string label, IEnumerable<string> expected, IEnumerable<string> actual, string sep)
        {
            var exp = expected.ToList();
            var act = actual.ToList();
            var missing = exp.Except(act, StringComparer.OrdinalIgnoreCase).ToList();
            var extra = act.Except(exp, StringComparer.OrdinalIgnoreCase).ToList();
            _logger?.Info($"{label} diff — Missing: {string.Join(sep, missing)} | Extra: {string.Join(sep, extra)}");
            return (missing, extra);
        }

        /// <summary>
        /// Clicks the "Get E&amp;S HO rates" button, which surfaces the Excess &amp; Surplus
        /// (Bamboo Surplus) offering. Unlike the standard admitted carriers, the E&amp;S carrier
        /// is not selected from the carrier table — it is reached via this dedicated button.
        /// </summary>
        public async Task ClickGetESRatesButton()
        {
            _logger?.Info("Clicking the 'Get E&S HO rates' button to surface the Excess & Surplus offering.");
            var getESRates = SelectedCarrierFields.Fields[GetESRates];
            await PageHelper.InteractWithElement(getESRates);

            // The E&S rate is computed asynchronously; proceeding before it renders leaves the page
            // on the standard admitted-carrier rates. Wait for the E&S "Important Notes" heading —
            // the definitive signal the offer has loaded — before the test reads carrier/coverages.
            _logger?.Info("Waiting for the E&S offering to render (Important Notes on E&S Homeowners).");
            await Page.Locator(EsLoadedSignalLocator).WaitForAsync(
                new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = PageTimeout });
        }

        /// <summary>
        /// Clicks the "Get E&amp;S HO rates" button when the E&amp;S carrier is expected to decline
        /// (UUDS / "Not quoted"): instead of a rate the offer renders a decline block with the
        /// reason copy and a "Get DF rates" link. Waits for that decline block to appear — the
        /// definitive "the carrier responded" signal for the decline path — rather than the
        /// rate-loaded "Important Notes" heading used by <see cref="ClickGetESRatesButton"/>.
        /// </summary>
        public async Task ClickGetESRatesExpectingDeclineAsync()
        {
            _logger?.Info("Clicking the 'Get E&S HO rates' button, expecting a 'Not quoted' decline.");
            var getESRates = SelectedCarrierFields.Fields[GetESRates];
            await PageHelper.InteractWithElement(getESRates);

            // The E&S result is computed asynchronously; the "Get E&S rates" button stays present
            // until it completes (the standard-page "Not quoted" list is already on screen, so it is
            // not a reliable signal). Wait for the button to detach — that means E&S processed.
            _logger?.Info("Waiting for the 'Get E&S rates' button to detach (E&S result processed).");
            await Page.Locator(GetESRatesButtonLocator).WaitForAsync(
                new LocatorWaitForOptions { State = WaitForSelectorState.Detached, Timeout = PageTimeout });
        }

        /// <summary>
        /// Reads the "Not quoted" (<c>rate-unquoted-carriers</c>) decline reasons — one per declined
        /// carrier — in document order, trimmed with inner whitespace collapsed.
        /// </summary>
        public async Task<List<string>> GetNotQuotedReasonsAsync()
        {
            var nodes = Page.Locator(NotQuotedReasonLocator);
            var lines = new List<string>();

            for (int i = 0, count = await nodes.CountAsync(); i < count; i++)
            {
                var text = (await nodes.Nth(i).InnerTextAsync())?.Trim();
                if (string.IsNullOrWhiteSpace(text))
                    continue;
                text = string.Join(" ", text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
                lines.Add(text);
            }

            _logger?.Info($"Not-quoted decline reasons ({lines.Count}): {string.Join(" | ", lines)}");
            return lines;
        }

        /// <summary>Returns whether the E&amp;S carrier's logo appears in the "Not quoted" block.</summary>
        public async Task<bool> IsCarrierInNotQuotedBlockAsync(string carrierName)
        {
            var locator = $"{NotQuotedSectionLocator} svg[title*=\"{carrierName}\" i]";
            var count = await Page.Locator(locator).CountAsync();
            _logger?.Info($"Carrier '{carrierName}' in Not-quoted block: {count > 0}");
            return count > 0;
        }

        public async Task ClickOnSpecificCarrier(string carrier)
        {
            await ClickOnViewCoverageButton();

            var carrierElement = SelectedCarrierFields.Fields[SelectedCarrier];
            var carrierLocator = carrierElement.GetLocators(carrier);
            var isCarrierFound = await PageHelper.ElementExists(carrierElement.Strategy, carrierLocator, timeout: 100);

            if (!isCarrierFound)
            {
                throw new CarrierNotFoundException(carrier, "Selected Carrier page");
            }

            await PageHelper.InteractWithElement(
               LocatorType.XPath,
               carrierLocator,
               ElementAction.Click
           );
        }
    }
}
