using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Exceptions;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.ADBX.Pages.Quotes;
using Bolt.Automation.FrontEnds.Projects.Interview.Base;
using Bolt.Automation.FrontEnds.Projects.Interview.Popups;
using Microsoft.Playwright;
using static Bolt.Automation.FrontEnds.Projects.Interview.FormData.FieldNames;

namespace Bolt.Automation.FrontEnds.Projects.Interview.Pages
{
    public class Product_ResultsPage : InterviewBase
    {
        protected override string PageIdentifier => "Results";
        protected override string PageName => "Results Page";

        #region Locators
        private const string GettingTheBestQuoteLoaderLocator = "//app-loader[@class = 'ng-star-inserted']";
        private const string RatedCarrierPriceLocator = "app-carrier-logo-and-premium .amount";
        private const string ApplicationFormsButtonLocator = "//button[contains(@class,'Application Forms ')]";
        private const string RatingInProgressBannerLocator = "//*[contains(text(),'still in progress')]";
        private const string CarrierDetailPanelLocator = "//*[contains(text(),'Quote number')]";
        private const string NotesCasesButtonLocator = "//button[contains(normalize-space(.),'Notes') and contains(normalize-space(.),'Cases')] | //a[contains(normalize-space(.),'Notes') and contains(normalize-space(.),'Cases')]";
        private const string RequestAppButtonLocator = "//button[contains(@class,'Request Application') or contains(normalize-space(.),'Request Application')]";
        // Matches both admitted/non-admitted cards (app-carrier-card) and declined/failed
        // entries (app-not-quoted-carrier-list) - both render a carrier logo <img> from the
        // same Resource endpoint, but the declined-carrier image has alt="null" (literal string).
        private const string CarrierImagesInCurrentTabLocator = "div.tab-content img[src*='type=carrier']";
        // Declined/failed carrier entries - each holds its own logo image and a reason message.
        private const string DeclinedCarrierItemLocator = "app-not-quoted-carrier-list li.carrier";
        private const string CarrierCardLocator = "div.tab-content app-carrier-card";
        // The card's call to action. The class names the action ("Finalize", "Request Application"), and
        // the visible label lives in a nested span.title rather than being the button's own text.
        private const string CarrierActionButtonLocator =
            "app-button button[class*='Finalize'], app-button button[class*='Application']";
        private const string CarrierActionButtonLabelLocator = "span.title";
        // Scoped to the market-category tab bar ("Admitted (2)", "Declinations/Failures (2)", ...) —
        // the Overview/Table view-toggle control reuses the same "tab-titles" class.
        private const string TabTitlesLocator = "ul.tab-titles.left li";
        #endregion

        public Product_ResultsPage(IBrowserManager browserManager, IPageHelper pageHelper, IScopeContext scopeContext, bool validatePageReady = true, IAutomationLogger? logger = null)
        : base(browserManager, pageHelper, scopeContext, validatePageReady, logger)
        {
            var loaderLocator = Page.Locator(GettingTheBestQuoteLoaderLocator);
            PageHelper.WaitForElementToDisappearAsync(loaderLocator, 60000, initialRetries: 5, retryDelay: 1000).GetAwaiter().GetResult();
            PageHelper.ValidatePageReadyAsync(PageIdentifier, PageName).GetAwaiter().GetResult();
        }

        public override async Task ValidatePageReady()
        {
            // 2 min is the longest Bolt allows rating to take, and the interview rates between the
            // Applicant page and this one. Past that it is a product defect, not a slow test.
            await PageHelper.ValidatePageReadyAsync(PageIdentifier, PageName, timeout: 120000);
        }

        public async Task<bool> IsGetRates(int timeoutSeconds = 45)
        {
            _logger?.Info("Waiting for at least one carrier rate to appear on the results page...");
            // to layer to handle the dynamic loading in result page
            // Primary signal: a carrier card showing a premium/price. Fallback: the action button.
            var ratedCarriers = Page.Locator(RatedCarrierPriceLocator);
            try
            {
                await ratedCarriers.First.WaitForAsync(new Microsoft.Playwright.LocatorWaitForOptions
                {
                    State = Microsoft.Playwright.WaitForSelectorState.Attached,
                    Timeout = timeoutSeconds * 1000
                });

                var ratedCount = await Page.Locator(RatedCarrierPriceLocator).CountAsync();
                _logger?.LogBusinessRule("RatesDisplayed", true,
                    $"At least one rate is present ({ratedCount} carrier(s) with a premium displayed)");
                return true;
            }
            catch (TimeoutException)
            {
                _logger?.LogBusinessRule("RatesDisplayed", false,
                    $"No rates displayed within {timeoutSeconds}s");
                return false;
            }
        }

        public async Task<Interview_EmailQuotesPopup> ClickOnEmailQuotes()
        {
            await PageHelper.InteractWithField(EmailQuoteButton);

            return new Interview_EmailQuotesPopup(BrowserManager, PageHelper, ScopeContext, true, _logger);
        }

        public async Task ClickMarketCategoryTab(string categoryName)
        {
            var locator = $"//ul[contains(@class,'tab-titles')]//li[contains(normalize-space(.), '{categoryName}')]";
            await PageHelper.InteractWithElement(LocatorType.XPath, locator, ElementAction.Click);
            _logger?.Info($"Clicked on market category tab: {categoryName}");
            await Task.Delay(1000);
        }

        /// <summary>
        /// Returns the names of all carriers rendered in the currently active market-category
        /// tab, regardless of whether each one has been rated yet. When <paramref name="expectedCount"/>
        /// is greater than zero, polls up to <paramref name="timeoutSeconds"/> for that many
        /// carriers to render before reading.
        /// </summary>
        public async Task<List<string>> GetCarriersInCurrentTab(int expectedCount = 0, int timeoutSeconds = 20)
        {
            var images = Page.Locator(CarrierImagesInCurrentTabLocator);
            if (expectedCount > 0)
            {
                await WaitForLocatorCount(images, expectedCount, timeoutSeconds);
            }

            var carrierNames = await ReadCarrierNames(images);
            _logger?.Info($"Found {carrierNames.Count} carriers in current tab: {string.Join(", ", carrierNames)}");
            return carrierNames;
        }

        /// <summary>
        /// Returns the action-button label on each rated carrier card in the currently active tab, in
        /// card order, so a test can assert every carrier offers the same call to action (on the CL
        /// results page that reads "Finalize with carrier", not "Request Application").
        /// </summary>
        /// <remarks>
        /// A card with no action button comes back as an empty string rather than being skipped, so a
        /// missing button fails the caller's assertion instead of silently shrinking the list.
        /// <para>
        /// Waits for the first card only, not a count: the caller does not know how many carriers rated,
        /// and <see cref="WaitForLocatorCount"/> matches an <em>exact</em> count, so asking it for 1 here
        /// would burn the whole budget on every multi-carrier tab and could return on a partial set while
        /// cards were still arriving. Call this after <see cref="WaitForRatingToComplete"/>, which is what
        /// establishes that the set is settled.
        /// </para>
        /// </remarks>
        public async Task<List<string>> GetCarrierActionButtonTexts(int timeoutSeconds = 20)
        {
            var cards = Page.Locator(CarrierCardLocator);
            await WaitForAtLeastOne(cards, timeoutSeconds);

            var count = await cards.CountAsync();
            var labels = new List<string>(count);
            for (int i = 0; i < count; i++)
            {
                var button = cards.Nth(i).Locator(CarrierActionButtonLocator).First;
                if (await button.CountAsync() == 0)
                {
                    labels.Add(string.Empty);
                    continue;
                }

                var label = button.Locator(CarrierActionButtonLabelLocator).First;
                var target = await label.CountAsync() > 0 ? label : button;
                labels.Add((await target.InnerTextAsync()).Trim());
            }

            _logger?.Info($"Carrier action buttons in current tab: {string.Join(" | ", labels.Select(l => l.Length == 0 ? "<none>" : l))}");
            return labels;
        }

        /// <summary>
        /// Walks every market-category tab (Admitted, Declinations/Failures, Non-admitted,
        /// Offline, and any other tab the page renders - e.g. CL's "Additional Carriers" -
        /// collected into <see cref="MarketRatesResult.Other"/>), clicking into a tab only when
        /// its declared count is greater than zero, then restores whichever tab was originally
        /// active.
        /// Waits for the "still in progress" rating banner to clear first - tab counts (e.g.
        /// "Admitted (0)") are only accurate once rating settles, otherwise a carrier still
        /// rating reads as absent instead of pending. The wait is best-effort: some quotes
        /// (e.g. Home with many carriers) can outlast it, so a timeout is logged and the
        /// method still reads whatever tab state currently exists, same as before this wait
        /// existed, rather than failing the whole call.
        /// </summary>
        public async Task<MarketRatesResult> GetAllRates(int perTabTimeoutSeconds = 20, int ratingTimeoutSeconds = 180)
        {
            try
            {
                await WaitForRatingToComplete(ratingTimeoutSeconds);
            }
            catch (PageElementException ex)
            {
                _logger?.Warning($"Carrier rating banner did not clear within {ratingTimeoutSeconds}s before reading rates - reading current tab state anyway: {ex.Message}");
            }

            var tabs = Page.Locator(TabTitlesLocator);
            var tabCount = await tabs.CountAsync();

            var admitted = new List<string>();
            var declinationsFailures = new List<CarrierDeclination>();
            var nonAdmitted = new List<string>();
            var offline = new List<string>();
            var other = new Dictionary<string, List<string>>();
            var admittedTabPresent = false;
            var declinationsFailuresTabPresent = false;
            var nonAdmittedTabPresent = false;
            var offlineTabPresent = false;

            var originallyActiveIndex = await GetActiveTabIndex(tabs, tabCount);

            for (int i = 0; i < tabCount; i++)
            {
                var tab = tabs.Nth(i);
                var label = (await tab.InnerTextAsync()).Trim();
                if (string.IsNullOrEmpty(label))
                {
                    continue;
                }

                var expectedCount = ParseTabCarrierCount(label) ?? 0;
                var isDeclinationsTab = label.Contains("Declin", StringComparison.OrdinalIgnoreCase);

                var carrierNames = new List<string>();
                var declinations = new List<CarrierDeclination>();

                if (expectedCount > 0)
                {
                    await EnsureTabActive(tab);

                    if (isDeclinationsTab)
                    {
                        declinations = await WaitForDeclinedCarriersInCurrentTab(expectedCount, perTabTimeoutSeconds);
                        carrierNames = declinations.Select(d => $"{d.Carrier} ({d.Reason})").ToList();
                    }
                    else
                    {
                        carrierNames = await GetCarriersInCurrentTab(expectedCount, perTabTimeoutSeconds);
                    }
                }

                if (label.StartsWith("Admitted", StringComparison.OrdinalIgnoreCase))
                {
                    admitted = carrierNames;
                    admittedTabPresent = true;
                }
                else if (isDeclinationsTab)
                {
                    declinationsFailures = declinations;
                    declinationsFailuresTabPresent = true;
                }
                else if (label.Contains("Non-admitted", StringComparison.OrdinalIgnoreCase))
                {
                    nonAdmitted = carrierNames;
                    nonAdmittedTabPresent = true;
                }
                else if (label.StartsWith("Offline", StringComparison.OrdinalIgnoreCase))
                {
                    offline = carrierNames;
                    offlineTabPresent = true;
                }
                else
                {
                    other[StripCarrierCount(label)] = carrierNames;
                }

                var actualCount = isDeclinationsTab ? declinations.Count : carrierNames.Count;
                if (actualCount != expectedCount)
                {
                    _logger?.Warning($"'{label}' tab declared {expectedCount} carrier(s) but only {actualCount} rendered within {perTabTimeoutSeconds}s");
                }
            }

            await RestoreActiveTab(tabs, originallyActiveIndex);

            return new MarketRatesResult(
                admitted, declinationsFailures, nonAdmitted, offline, other,
                admittedTabPresent, declinationsFailuresTabPresent, nonAdmittedTabPresent, offlineTabPresent);
        }

        /// <summary>Extracts the carrier count from a tab label such as "Admitted (2)".</summary>
        private static int? ParseTabCarrierCount(string tabLabel)
        {
            var match = System.Text.RegularExpressions.Regex.Match(tabLabel, @"\((\d+)\)");
            return match.Success ? int.Parse(match.Groups[1].Value) : null;
        }

        /// <summary>Strips the trailing " (N)" carrier count from a tab label, e.g. "Additional Carriers (1)" → "Additional Carriers".</summary>
        private static string StripCarrierCount(string tabLabel) =>
            System.Text.RegularExpressions.Regex.Replace(tabLabel, @"\s*\(\d+\)\s*$", "").Trim();

        /// <summary>Returns the index of the currently active tab, or -1 if none is active.</summary>
        private static async Task<int> GetActiveTabIndex(ILocator tabs, int tabCount)
        {
            for (int i = 0; i < tabCount; i++)
            {
                if (await IsTabActive(tabs.Nth(i)))
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>Checks whether a tab element carries the "active" CSS class.</summary>
        private static async Task<bool> IsTabActive(ILocator tab)
        {
            var classAttr = await tab.GetAttributeAsync("class");
            return classAttr != null && classAttr.Contains("active");
        }

        /// <summary>Clicks the tab if it isn't already active, then waits for the tab-switch to render.</summary>
        private static async Task EnsureTabActive(ILocator tab)
        {
            if (await IsTabActive(tab))
            {
                return;
            }

            await tab.ClickAsync();
            await Task.Delay(1000);
        }

        /// <summary>Re-activates whichever tab was active before <see cref="GetAllRates"/> started switching between them.</summary>
        private static async Task RestoreActiveTab(ILocator tabs, int originallyActiveIndex)
        {
            if (originallyActiveIndex >= 0)
            {
                await EnsureTabActive(tabs.Nth(originallyActiveIndex));
            }
        }

        /// <summary>Polls the Declinations/Failures tab until the declared count renders, then reads each carrier's name and decline reason.</summary>
        private async Task<List<CarrierDeclination>> WaitForDeclinedCarriersInCurrentTab(int expectedCount, int timeoutSeconds)
        {
            var items = Page.Locator(DeclinedCarrierItemLocator);
            await WaitForLocatorCount(items, expectedCount, timeoutSeconds);

            var count = await items.CountAsync();
            var declinations = new List<CarrierDeclination>();

            for (int i = 0; i < count; i++)
            {
                var item = items.Nth(i);
                var carrier = await GetCarrierName(item.Locator("img").First);
                var reason = (await item.Locator(".info").First.InnerTextAsync()).Trim();

                if (!string.IsNullOrWhiteSpace(carrier))
                {
                    declinations.Add(new CarrierDeclination(carrier, reason));
                }
            }

            return declinations;
        }

        /// <summary>
        /// Waits for a locator's match count to reach <paramref name="expectedCount"/>, using
        /// Playwright's own retrying assertion instead of a manual poll loop. Swallows a timeout -
        /// the caller re-reads the actual count afterward and logs a Warning on mismatch.
        /// </summary>
        private static async Task WaitForLocatorCount(ILocator locator, int expectedCount, int timeoutSeconds)
        {
            try
            {
                await Assertions.Expect(locator).ToHaveCountAsync(expectedCount, new LocatorAssertionsToHaveCountOptions
                {
                    Timeout = timeoutSeconds * 1000
                });
            }
            catch (PlaywrightException)
            {
            }
        }

        /// <summary>
        /// Waits for a locator to match at least one element, for callers that do not know the expected
        /// count. Swallows the timeout on the same contract as <see cref="WaitForLocatorCount"/> - the
        /// caller re-reads the count and asserts on it, which reads better than a raw Playwright timeout.
        /// </summary>
        private static async Task WaitForAtLeastOne(ILocator locator, int timeoutSeconds)
        {
            try
            {
                await locator.First.WaitForAsync(new LocatorWaitForOptions
                {
                    State = WaitForSelectorState.Visible,
                    Timeout = timeoutSeconds * 1000
                });
            }
            catch (PlaywrightException)
            {
            }
        }

        /// <summary>Reads the carrier name from every matched image.</summary>
        private static async Task<List<string>> ReadCarrierNames(ILocator images)
        {
            var carrierNames = new List<string>();
            var count = await images.CountAsync();

            for (int i = 0; i < count; i++)
            {
                var name = await GetCarrierName(images.Nth(i));
                if (!string.IsNullOrWhiteSpace(name))
                {
                    carrierNames.Add(name);
                }
            }

            return carrierNames;
        }

        /// <summary>
        /// Reads a carrier's name from its logo image's alt text, falling back to the "name"
        /// query parameter of the image src (declined/failed carrier images render alt="null"
        /// as a literal string rather than omitting it).
        /// </summary>
        private static async Task<string?> GetCarrierName(ILocator image)
        {
            var altText = await image.GetAttributeAsync("alt");
            if (!string.IsNullOrWhiteSpace(altText) && !altText.Equals("null", StringComparison.OrdinalIgnoreCase))
            {
                return altText;
            }

            // Declined/failed carrier images render alt="null" (literal) - recover the name
            // from the Resource endpoint's "name" query parameter instead.
            var src = await image.GetAttributeAsync("src");
            var match = string.IsNullOrEmpty(src) ? null : System.Text.RegularExpressions.Regex.Match(src, @"name=([^&]+)");
            return match is { Success: true } ? Uri.UnescapeDataString(match.Groups[1].Value) : null;
        }

        public async Task WaitForRatingToComplete(int timeoutSeconds = 180)
        {
            _logger?.Info("Waiting for all carrier ratings to complete...");
            var inProgressLocator = Page.Locator(RatingInProgressBannerLocator);
            await PageHelper.WaitForElementToDisappearAsync(inProgressLocator, timeoutSeconds * 1000, initialRetries: 5, retryDelay: 2000);
            _logger?.Info("Carrier rating complete");
        }

        public async Task<bool> IsMessageVisible(string message, int timeoutMs = 8000)
        {
            var locator = $"//*[contains(normalize-space(text()), \"{message}\")]";
            var exists = await PageHelper.ElementExists(LocatorType.XPath, locator, timeoutMs);
            _logger?.Info($"Message '{message}' visible on results page: {exists}");
            return exists;
        }

        public async Task<bool> WaitForCarrierToAppear(string carrierName, int timeoutSeconds = 120, int pollIntervalMs = 2000)
        {
            _logger?.Info($"Waiting for carrier '{carrierName}'...");

            var endTime = DateTime.Now.AddSeconds(timeoutSeconds);

            while (DateTime.Now < endTime)
            {
                var carriers = await GetCarriersInCurrentTab();
                if (carriers.Any(c => c.Contains(carrierName, StringComparison.OrdinalIgnoreCase)))
                {
                    _logger?.Info($"Carrier '{carrierName}' found");
                    return true;
                }
                await Task.Delay(pollIntervalMs);
            }

            _logger?.Warning($"Carrier '{carrierName}' not found within {timeoutSeconds}s");
            return false;
        }

        public async Task<Interview_ApplicationFormsPopup> ClickApplicationFormsButton()
        {
            await PageHelper.InteractWithElement(LocatorType.XPath, ApplicationFormsButtonLocator, ElementAction.Click);
            _logger?.Info("Clicked Application Forms button");
            return new Interview_ApplicationFormsPopup(BrowserManager, PageHelper, ScopeContext, true, _logger);
        }

        public async Task<List<string>> GetAcordFormsList()
        {
            var items = Page.Locator("//input[@type='checkbox'][contains(@id,'ACORD')]");
            var count = await items.CountAsync();
            var formNames = new List<string>();

            for (int i = 0; i < count; i++)
            {
                var id = await items.Nth(i).GetAttributeAsync("id");
                if (!string.IsNullOrWhiteSpace(id))
                    formNames.Add(id.Trim());
            }

            _logger?.Info($"Found {count} ACORD forms: {string.Join(", ", formNames)}");
            return formNames;
        }

        /// <summary>
        /// Leaves the interview for this quote's timeline in ADBX, and switches the scope to ADBX so the
        /// caller can carry straight on with the dashboard page this returns.
        /// </summary>
        /// <remarks>
        /// Deliberately no tab switch: Notes/Cases navigates the current tab to the quote timeline.
        /// Re-selecting the last tab here picks up whichever ADBX tab the quote was opened from - the
        /// account page, or the ADBX root - and the returned page then fails its own URL validation
        /// (seen on TC 253185, which sat on .../accounts/&lt;id&gt;/timeline instead of .../quotes/...).
        /// </remarks>
        public async Task<ADBX_QuoteSummaryPage> ClickNotesAndCases()
        {
            await PageHelper.InteractWithElement(LocatorType.XPath, NotesCasesButtonLocator, ElementAction.Click);
            ScopeContext.Set(ctx => ctx.FrontEnd, Bolt.Automation.Common.Enums.FrontEndType.ADBX);
            _logger?.Info("Clicked Notes/Cases - leaving the interview for the quote timeline in ADBX");
            return new ADBX_QuoteSummaryPage(BrowserManager, PageHelper, ScopeContext, logger: _logger);
        }

        /// <summary>
        /// Clicks "Request Application" on the carrier card, waits for the carrier detail panel
        /// to open, then clicks "Request Application" again inside that panel to reach the
        /// final Request Application popup.
        /// </summary>
        public async Task<Interview_RequestApplicationPopup> ClickRequestApplication()
        {
            // First click: opens the carrier detail side panel
            await PageHelper.InteractWithElement(LocatorType.XPath, RequestAppButtonLocator, ElementAction.Click);
            _logger?.Info("Clicked Request Application on carrier card");

            // Wait for the carrier detail panel to appear (it shows a Quote number)
            var panelLocator = Page.Locator(CarrierDetailPanelLocator);
            await PageHelper.WaitForElementAsync(panelLocator, timeout: 10000, waitForVisibility: true);

            // Second click: the panel's Request Application button is the last one in the DOM
            var panelButton = Page.Locator(RequestAppButtonLocator).Last;
            await panelButton.ClickAsync(new Microsoft.Playwright.LocatorClickOptions { Timeout = 10000 });
            _logger?.Info("Clicked Request Application in carrier detail panel");

            return new Interview_RequestApplicationPopup(BrowserManager, PageHelper, ScopeContext, true, _logger);
        }
    }
}
