using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Exceptions;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.Common.Logging.Extentions;
using Bolt.Automation.Common.Utils;
using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.Interview.Pages;
using Bolt.Automation.FrontEnds.Projects.Interview.Popups;
using NUnit.Framework;

namespace Bolt.Automation.Tests.TestHelpers.Interview
{
    public class InterviewTestHelper(
    IAutomationLogger _logger,
    IBrowserManager _browserManager,
    IPageFactory _pageFactory,
    IPageHelper _pageHelper,
    IScopeContext _scopeContext)
    {

        public async Task<bool> IsCarrierVisible(string carrierName, string? altTextContains = null)
        {
            if (string.IsNullOrWhiteSpace(carrierName))
                throw new ArgumentException("Carrier name cannot be null or empty", nameof(carrierName));

            var rawText = altTextContains ?? carrierName;

            // Normalize whitespace: trim edges and collapse internal multiple spaces to one
            var searchAltText = System.Text.RegularExpressions.Regex.Replace(rawText.Trim(), @"\s+", " ").ToLower();

            // Use normalize-space() in XPath so alt attribute spacing differences are ignored
            var carrierImageLocator = $"//img[translate(normalize-space(@alt), 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz')='{searchAltText}']";

            _logger?.Info($"Searching for carrier logo with locator: {carrierImageLocator}");


            var exists = await _pageHelper.ElementExists(LocatorType.XPath, carrierImageLocator, 2000);
            if (exists)
            {
                _logger?.Info($"Carrier '{carrierName}' found and is visible.");
                return true;
            }

            _logger?.Warning($"Carrier '{carrierName}' not found on the page");
            return false;
        }

        public async Task ClickProgressBarStep(string stepName)
        {
            var progressBarStepLocator = $"//div[contains(@class,'progress-meter-wrapper')]//li[contains(normalize-space(.), '{stepName}')]";

            _logger?.Info($"Clicking on progress bar step: {stepName}");

            var exists = await _pageHelper.ElementExists(LocatorType.XPath, progressBarStepLocator, 5000);
            if (!exists)
            {
                _logger?.Warning($"Progress bar step '{stepName}' not found");
                throw new PageElementException($"Progress bar step '{stepName}'", "not found on the page");
            }

            await _pageHelper.InteractWithElement(LocatorType.XPath, progressBarStepLocator, ElementAction.Click);
            _logger?.Info($"Successfully clicked on progress bar step: {stepName}");
            await Task.Delay(1000);
        }

        /// <summary>
        /// Fetches all rates from the Results page and logs a "RatesDisplayed" business rule
        /// summarizing every market category. Returns the result so the caller can assert on it.
        /// Only categories whose tab actually rendered are included in the message - a category
        /// with no tab on this page (e.g. no "Offline" tab on a Personal Auto quote) is omitted
        /// rather than logged as a misleading "0 []", which reads identically to a tab that did
        /// render with zero carriers.
        /// </summary>
        public async Task<MarketRatesResult> LogRatesBusinessRule(Product_ResultsPage resultsPage)
        {
            var rates = await resultsPage.GetAllRates();

            var parts = new List<string>();
            if (rates.AdmittedTabPresent)
                parts.Add($"Admitted: {rates.Admitted.Count} [{string.Join(", ", rates.Admitted)}]");
            if (rates.DeclinationsFailuresTabPresent)
            {
                var declinationsFailures = rates.DeclinationsFailures.Select(d => $"{d.Carrier} ({d.Reason})");
                parts.Add($"Declinations/Failures: {rates.DeclinationsFailures.Count} [{string.Join(", ", declinationsFailures)}]");
            }
            if (rates.NonAdmittedTabPresent)
                parts.Add($"Non-Admitted: {rates.NonAdmitted.Count} [{string.Join(", ", rates.NonAdmitted)}]");
            if (rates.OfflineTabPresent)
                parts.Add($"Offline: {rates.Offline.Count} [{string.Join(", ", rates.Offline)}]");

            _logger.LogBusinessRule("RatesDisplayed", rates.Admitted.Count > 0, string.Join(", ", parts));

            return rates;
        }

        /// <summary>
        /// Drives an open Offline Request popup end to end: request text, attachment, Submit Request, then
        /// Confirm to dismiss the "your request was submitted" question. Returns the same popup so the
        /// caller can hand it straight to <see cref="VerifyOfflineRequestSubmittedAsync"/>.
        /// Deliberately not wrapped in a step: one caller asserts the paper-application card inside the
        /// same step, so the step boundary belongs to the test.
        /// </summary>
        public async Task<Interview_OfflineRequestPopup> SubmitOfflineRequestAsync(Interview_OfflineRequestPopup popup)
        {
            var offlineText = "AutoTest " + RandomManager.GetRandomString(8);
            _logger.Info($"Offline request text: {offlineText}");

            await popup.AddText(offlineText);
            await popup.AddFileAsync();
            await popup.ClickContinue();
            await popup.ClickPopupConfirm();
            return popup;
        }

        /// <summary>
        /// Opens the Offline Request from the results page's "Additional Carriers" tab, fills it in, and
        /// asserts it came back submitted.
        /// </summary>
        /// <remarks>
        /// Re-creates the results page rather than taking one: two callers reach this after a Block Bind
        /// detour, so the page object the test was holding is stale by now.
        /// </remarks>
        public async Task SubmitAdditionalCarriersOfflineRequestAsync()
        {
            var popup = await _logger.ExecuteStepAsync("Open offline request popup and fill form", async () =>
            {
                var resultsPage = _pageFactory.CreatePage<Product_ResultsPage>();
                await resultsPage.ClickMarketCategoryTab("Additional Carriers");
                await resultsPage.ClickOfflineRequest();

                return await SubmitOfflineRequestAsync(_pageFactory.CreatePage<Interview_OfflineRequestPopup>());
            });

            await VerifyOfflineRequestSubmittedAsync(popup);
        }

        /// <summary>
        /// Asserts a submitted Offline Request left the button in the disabled "Request Submitted" state.
        /// Pass <paramref name="expectedResult"/> when a test needs wording other than the default in the
        /// step report.
        /// Note: this is typed to <see cref="Interview_OfflineRequestPopup"/> on purpose —
        /// <c>Interview_RequestApplicationPopup</c> hides <c>IsRequestSubmittedButtonExists</c> with
        /// <c>new</c>, so accepting the shared base would silently call the wrong implementation.
        /// </summary>
        public async Task VerifyOfflineRequestSubmittedAsync(
            Interview_OfflineRequestPopup popup,
            string expectedResult = "Expected result: Case created, popup closed, button shows 'Request Submitted' (grayed out)")
        {
            await _logger.ExecuteStepAsync("Verify offline request submission", async () =>
            {
                var isSubmitted = await popup.IsRequestSubmittedButtonExists();
                _logger.LogDataValidation("Request Submitted Button", isSubmitted, "true", isSubmitted.ToString(),
                    "Offline request button should change to 'Request Submitted' and be grayed out");
                Assert.That(isSubmitted, Is.True, "Offline request button did not change to 'Request Submitted'");
            }, expectedResult);
        }
    }
}
