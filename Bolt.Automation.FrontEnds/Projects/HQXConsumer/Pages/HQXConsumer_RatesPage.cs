using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Enums;
using Bolt.Automation.Common.Exceptions;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.HQXConsumer.Base;
using Bolt.Automation.FrontEnds.Projects.HQXConsumer.Helpers;
using Bolt.Automation.FrontEnds.Projects.HQXConsumer.Services.CarrierSelection;
using Bolt.Automation.FrontEnds.Projects.HQXConsumer.Services.Tooltips;
using Bolt.Automation.TestDataProvider.TestData.CoverageModificationTestData;
using Microsoft.Playwright;

namespace Bolt.Automation.FrontEnds.Projects.HQXConsumer.Pages
{
    public class HQXConsumer_RatesPage : HQXConsumerBase
    {
        private readonly Lazy<CarrierSelectionService> _carrierService;
        private readonly Lazy<TooltipValidationService> _tooltipService;

        private const string EditCoverageButtonLocator = ".desktop-view .edit-btn";
        private const string UndoEditsButtonLocator = ".desktop-view .undo-edits-btn";
        private const string OnlineBuyButtonLocator = "#onlineBuyBtn";
        private const int BuildYourPolicyAjaxTimeout = 10000;


        public HQXConsumer_RatesPage(
            IBrowserManager browserManager,
            IPageHelper pageHelper,
            IScopeContext scopeContext,
            bool validatePageReady = true,
            IAutomationLogger? logger = null)
            : base(browserManager, pageHelper, scopeContext, validatePageReady, logger)
        {
            // Lazy initialization to avoid constructor complexity
            _carrierService = new Lazy<CarrierSelectionService>(
                () => new CarrierSelectionService(Page, _logger));
            
            _tooltipService = new Lazy<TooltipValidationService>(
                () => new TooltipValidationService(Page, PageHelper, _logger));
        }

        protected override string PageIdentifier => "rates";
        protected override string PageName => "Rates Page";
        protected override int PageTimeout => 120000;

        #region Carrier Operations (Delegated to Service)
        
        /// <summary>
        /// Gets the currently selected carrier name
        /// </summary>
        public Task<string?> GetSelectedCarrierNameAsync() => _carrierService.Value.GetSelectedCarrierAsync();

        /// <summary>
        /// Ensures the specified carrier is selected
        /// </summary>
        public Task<bool> EnsureCarrierSelectedAsync(string expectedCarrier) => 
            _carrierService.Value.EnsureCarrierSelectedAsync(expectedCarrier);

        /// <summary>
        /// Gets all available carriers in the comparison table
        /// </summary>
        public Task<IReadOnlyList<string>> GetAvailableCarriersAsync() =>
            _carrierService.Value.GetAvailableCarriersAsync();

        /// <summary>
        /// Reads the selected carrier's information statement text (the underwriting-carrier
        /// paragraph plus bullet points) with normalized whitespace.
        /// </summary>
        public Task<string> GetCarrierInformationStatementAsync() =>
            _carrierService.Value.GetCarrierInformationStatementAsync();

        /// <summary>
        /// Reads the premium displayed for the currently selected carrier on the main rate card.
        /// </summary>
        public Task<decimal> GetSelectedCarrierPremiumAsync() =>
            _carrierService.Value.GetSelectedCarrierPremiumAsync();

        /// <summary>
        /// Switches to a different carrier and returns the premium shown for the new selection.
        /// </summary>
        public Task<decimal> SwitchCarrierAndGetPremiumAsync(string targetCarrier) =>
            _carrierService.Value.SwitchCarrierAndGetPremiumAsync(targetCarrier);

        /// <summary>
        /// Switches to any carrier other than the current one and returns (carrier, premium).
        /// Expands the "See other rates" table internally.
        /// </summary>
        public Task<(string Carrier, decimal Premium)> SwitchToAnyOtherCarrierAndGetPremiumAsync() =>
            _carrierService.Value.SwitchToAnyOtherCarrierAndGetPremiumAsync();

        /// <summary>
        /// Returns the package tiers (Basic / Popular / Deluxe) offered by the selected carrier.
        /// </summary>
        public Task<IReadOnlyList<string>> GetAvailablePackageTiersAsync() =>
            _carrierService.Value.GetAvailablePackageTiersAsync();

        /// <summary>
        /// Selects a specific package tier on the current carrier and returns its premium.
        /// </summary>
        public Task<decimal> SelectPackageTierAndGetPremiumAsync(string tier) =>
            _carrierService.Value.SelectPackageTierAndGetPremiumAsync(tier);

        /// <summary>
        /// Selects any package tier other than the current one and returns its premium.
        /// </summary>
        public Task<decimal> SelectDifferentPackageTierAndGetPremiumAsync() =>
            _carrierService.Value.SelectDifferentPackageTierAndGetPremiumAsync();

        /// <summary>
        /// Returns true when the currently selected carrier supports online buy.
        /// </summary>
        public Task<bool> IsOnlineBuyAvailableAsync(int timeoutMs = 2000) =>
            _carrierService.Value.IsOnlineBuyAvailableAsync(timeoutMs);

        /// <summary>
        /// Ensures a carrier that supports online buy is selected, switching via the
        /// "See other rates" comparison table if needed. Returns the carrier name that exposed the link.
        /// </summary>
        public Task<string> EnsureOnlineBuyCarrierSelectedAsync(IEnumerable<string> preferredOrder) =>
            _carrierService.Value.EnsureOnlineBuyCarrierSelectedAsync(preferredOrder);

        /// <summary>
        /// Clicks the online "Finish &amp; Buy" / "Customize and buy" button which bridges the user to the carrier site.
        /// </summary>
        public async Task ClickFinishAndBuyAsync()
        {
            _logger?.Info("Clicking Finish & Buy (online buy) button to bridge to carrier site");
            var button = Page.Locator(OnlineBuyButtonLocator);
            await PageHelper.WaitForElementAsync(button, BuildYourPolicyAjaxTimeout, true);
            await button.ClickAsync();
        }

        #endregion

        #region Coverage Display Operations

        public async Task ClickEditCoverageAsync()
        {
            _logger?.Debug("Clicking 'Edit coverages' to enter edit mode");

            var editButton = Page.Locator(EditCoverageButtonLocator);

            try
            {
                await PageHelper.WaitForElementAsync(editButton, BuildYourPolicyAjaxTimeout, false, 1);
            }
            catch (Exception ex)
            {
                throw new PageElementException(
                    "Edit Coverage button",
                    $"The 'Build your policy' section may not have rendered. Locator: '{EditCoverageButtonLocator}' | Page: {Page.Url}", ex);
            }

            await editButton.ClickAsync();
            _logger?.Debug("Clicked 'Edit coverages' — waiting for 'Undo edits' button to confirm edit mode is active");

            try
            {
                await PageHelper.WaitForElementAsync(Page.Locator(UndoEditsButtonLocator), 10000, true);
                _logger?.Debug("Edit mode confirmed — 'Undo edits' button is visible");
            }
            catch (Exception ex)
            {
                throw new PageElementException(
                    "'Undo edits' button",
                    $"'Edit coverages' was clicked but edit mode was not confirmed within the timeout. Page: {Page.Url}", ex);
            }
        }

        private async Task<IReadOnlyCollection<string>> GetDisplayNamesAsync(string rootSelector)
        {
            var nodes = Page.Locator($"{rootSelector} p.info-title span");
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            
            for (int i = 0, count = await nodes.CountAsync(); i < count; i++)
            {
                var text = (await nodes.Nth(i).InnerTextAsync())?.Trim(); 
                if (!string.IsNullOrWhiteSpace(text)) set.Add(text);
            }
            
            return set.ToList();
        }

        public Task<IReadOnlyCollection<string>> GetCoverageDisplayNamesAsync() => 
            GetDisplayNamesAsync("#coverages");
        
        public Task<IReadOnlyCollection<string>> GetDeductibleDisplayNamesAsync() => 
            GetDisplayNamesAsync("#deductibles");

        #endregion

        #region Title Comparison

        public record CoverageComparisonResult(IReadOnlyCollection<string> Missing, IReadOnlyCollection<string> Extra);

        private async Task<CoverageComparisonResult> CompareTitlesAsync(
            IEnumerable<string> expected,
            Func<Task<IReadOnlyCollection<string>>> actualFactory)
        {
            var expList = expected
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var actList = (await actualFactory())
                .Select(s => s.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var expSet = expList.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var actSet = actList.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var missing = expSet.Except(actSet, StringComparer.OrdinalIgnoreCase).ToList();
            var extra = actSet.Except(expSet, StringComparer.OrdinalIgnoreCase).ToList();

            return new CoverageComparisonResult(missing, extra);
        }

        public Task<CoverageComparisonResult> CompareCoverageTitlesAsync(IEnumerable<string> expected) =>
            CompareTitlesAsync(expected, GetCoverageDisplayNamesAsync);

        public Task<CoverageComparisonResult> CompareDeductibleTitlesAsync(IEnumerable<string> expected) =>
            CompareTitlesAsync(expected, GetDeductibleDisplayNamesAsync);

        #endregion

        #region Coverage Validation

        public record CoverageValidationSummary(
            CoverageComparisonResult CoverageTitles,
            CoverageComparisonResult DeductibleTitles,
            IReadOnlyCollection<Services.Tooltips.TooltipValidationIssue> TooltipIssues,
            IReadOnlyList<string> ExpectedCoverageTitles,
            IReadOnlyList<string> ExpectedDeductibleTitles);

        /// <summary>
        /// End-to-end validation: builds expected coverage & deductible titles (state-aware),
        /// compares with UI, then validates tooltips. Logs all validation results.
        /// </summary>
        public async Task<CoverageValidationSummary> ValidateCoverageDisplayAsync(LOBEnums lob, string? state, bool log = true)
        {
            state = state?.Trim().ToUpperInvariant() ?? string.Empty;
            _logger?.Info($"Validating coverage display for LOB: {lob}, State: {state}");

            var coverageDefs = CoverageDataProvider.GetCoverages(lob);
            var deductibleDefs = CoverageDataProvider.GetDeductibles(lob);

            var expectedCoverageTitles = coverageDefs
                .Select(c => CoverageDataProvider.GetCoverageVerbiage(lob, c.Coverage, state)?.DisplayName ?? c.DefaultVerbiage.DisplayName)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var expectedDeductibleTitles = deductibleDefs
                .Select(d => CoverageDataProvider.GetDeductibleVerbiage(lob, d.Coverage, state)?.DisplayName ?? d.DefaultVerbiage.DisplayName)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var coverageResult = await CompareCoverageTitlesAsync(expectedCoverageTitles);
            var deductibleResult = await CompareDeductibleTitlesAsync(expectedDeductibleTitles);

            var coverageTooltipPairs = coverageDefs
                .Select(c => new
                {
                    Title = CoverageDataProvider.GetCoverageVerbiage(lob, c.Coverage, state)?.DisplayName ?? c.DefaultVerbiage.DisplayName,
                    Tooltip = CoverageDataProvider.GetCoverageVerbiage(lob, c.Coverage, state)?.Tooltip ?? c.DefaultVerbiage.Tooltip
                })
                .Where(x => !string.IsNullOrWhiteSpace(x.Title));

            var deductibleTooltipPairs = deductibleDefs
                .Select(d => new
                {
                    Title = CoverageDataProvider.GetDeductibleVerbiage(lob, d.Coverage, state)?.DisplayName ?? d.DefaultVerbiage.DisplayName,
                    Tooltip = CoverageDataProvider.GetDeductibleVerbiage(lob, d.Coverage, state)?.Tooltip ?? d.DefaultVerbiage.Tooltip
                })
                .Where(x => !string.IsNullOrWhiteSpace(x.Title));

            var expectedTooltips = coverageTooltipPairs
                .Concat(deductibleTooltipPairs)
                .GroupBy(x => x.Title, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToDictionary(k => k.Title, v => v.Tooltip, StringComparer.OrdinalIgnoreCase);

            var tooltipIssues = await _tooltipService.Value.ValidateTooltipsAsync(expectedTooltips);

            if (log)
            {
                LogValidationResults(coverageResult, deductibleResult, tooltipIssues, expectedCoverageTitles, expectedDeductibleTitles);
            }

            return new CoverageValidationSummary(
                coverageResult,
                deductibleResult,
                tooltipIssues,
                expectedCoverageTitles,
                expectedDeductibleTitles);
        }

        private void LogValidationResults(
            CoverageComparisonResult coverageResult,
            CoverageComparisonResult deductibleResult,
            IReadOnlyCollection<Services.Tooltips.TooltipValidationIssue> tooltipIssues,
            IReadOnlyList<string> expectedCoverageTitles,
            IReadOnlyList<string> expectedDeductibleTitles)
        {
            LogTitleValidation("CoverageTitles", expectedCoverageTitles, coverageResult.Missing);
            LogTitleValidation("DeductibleTitles", expectedDeductibleTitles, deductibleResult.Missing);

            if (tooltipIssues.Count == 0)
            {
                _logger?.Info($"All tooltips validated successfully");
            }
            else
            {
                _logger?.Info($"Tooltip validation found {tooltipIssues.Count} issue(s)");
                foreach (var issue in tooltipIssues)
                {
                    _logger?.Info($"Tooltip Issue [{issue.Reason}] Title='{issue.Title}' Expected='{issue.ExpectedFragment}' Actual='{issue.Actual}'");
                }
            }
        }

        private void LogTitleValidation(string label, IReadOnlyCollection<string> expected, IReadOnlyCollection<string> missing)
        {
            if (missing.Count == 0)
            {
                _logger?.Info($"{label}: All {expected.Count} expected titles found - {string.Join(" | ", expected)}");
            }
            else
            {
                var found = expected.Except(missing, StringComparer.OrdinalIgnoreCase);
                _logger?.Info($"{label}: Found {found.Count()}/{expected.Count} - Missing: {string.Join(", ", missing)}");
            }
        }

        /// <summary>
        /// Validates coverage dropdown values for a specific carrier and state
        /// </summary>
        public Task AssertCoveragesForStateAsync(CarrierEnums carrier, string state, CoverageAssertionOptions? options = null) =>
            CoverageModificationHelper.ValidateCoveragesForStateAsync(Page, _logger, carrier, state, options);

        /// <summary>
        /// In the open coverage editor, changes every coverage dropdown the carrier exposes to a
        /// value different from its origin, WITHOUT clicking "update rate". Returns a map of
        /// coverage → newly-selected label for the dropdowns that were changed.
        /// </summary>
        public Task<IReadOnlyDictionary<CoverageEnums, string>> ChangeAllCoveragesToDifferentValuesAsync(CarrierEnums carrier) =>
            CoverageModificationHelper.ChangeAllCoveragesToDifferentValuesAsync(Page, _logger, carrier);

        /// <summary>
        /// Reads the currently-selected label of every coverage dropdown the carrier exposes.
        /// </summary>
        public Task<IReadOnlyDictionary<CoverageEnums, string>> ReadSelectedCoverageValuesAsync(CarrierEnums carrier) =>
            CoverageModificationHelper.ReadSelectedValuesAsync(Page, carrier);

        /// <summary>
        /// Normalizes a coverage dropdown label for comparison (strips spaces/commas), so
        /// e.g. "$1,000" and "$1000" compare equal. Matches the normalization used when the
        /// dropdown options are read/selected.
        /// </summary>
        public static string NormalizeCoverageLabel(string label) =>
            CoverageModificationHelper.Normalize(label);

        #endregion
    }
}