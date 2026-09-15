using System.Text.RegularExpressions;
using Bolt.Automation.Common.Enums;
using Bolt.Automation.Common.Exceptions;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.TestDataProvider.TestData.CoverageModificationTestData; // test data access
using Microsoft.Playwright;

namespace Bolt.Automation.FrontEnds.Projects.HQXConsumer.Helpers;

public sealed class CoverageAssertionOptions
{
    public decimal? MinWindHailPercent { get; init; }

    /// <summary>
    /// When true, validate Wind/Hail as Not-Applicable (not-editable + mirrors the Standard
    /// deductible) rather than against the carrier/state option list.
    /// See kb partner:pgr-covmod-wind-hail-na.
    /// </summary>
    public bool WindHailIsNotApplicable { get; init; }
}

/// <summary>
/// Helper utilities specific to HQX Consumer coverage modification feature.
/// Contains only page-specific coverage interaction and formatting helpers (no cross-project abstractions).
/// </summary>
internal static class CoverageModificationHelper
{
    private const string CoverageDropdownSelectorTemplate = "app-rate-dropdown#{0} ng-select, app-rate-dropdown[id='{0}'] ng-select";
    private const string CoverageSelectedValueSelectorTemplate = "app-rate-dropdown#{0} ng-select .ng-select-container .ng-value-label, app-rate-dropdown[id='{0}'] ng-select .ng-select-container .ng-value-label";
    private static readonly Regex DollarRegex = new(@"\$([0-9,]+)", RegexOptions.Compiled);
    private static readonly Regex PercentTokenRegex = new(@"\((\d+(?:\.\d+)?)%\)", RegexOptions.Compiled);

    private sealed record CoverageFailure(CoverageEnums Coverage, string Expected, string Actual, string Reason);

    private sealed record DropdownFetchResult(string[] Options, string FailureReason = "");

    private sealed record CoverageValidationContext(
        CoverageEnums Coverage,
        CoverageValue[] ExpectedValues,
        string[] ActualOptions,
        decimal? CoverageA,
        decimal? SelectedAllPerilsDollar);

    #region Public API (Interaction)

    public static async Task<string[]> GetCoverageDropdownValuesAsync(IPage page, IAutomationLogger? logger, CoverageEnums coverage)
    {
        var result = await FetchNgSelectOptionsAsync(page, coverage.ToString(), logger);
        if (result.Options.Length == 0)
            logger?.Warning($"No options returned for coverage '{coverage}': {result.FailureReason}");
        return result.Options;
    }

    public static async Task<string?> GetSelectedDisplayValueAsync(IPage page, CoverageEnums coverage)
    {
        var selector = string.Format(CoverageSelectedValueSelectorTemplate, coverage);
        var label = page.Locator(selector);
        if (await label.CountAsync() == 0) return null;
        return (await label.First.TextContentAsync())?.Trim();
    }

    public static async Task<decimal?> GetSelectedDollarForCoverageAsync(IPage page, CoverageEnums coverage)
        => ExtractFirstDollarAmount(await GetSelectedDisplayValueAsync(page, coverage));

    /// <summary>
    /// Opens the coverage dropdown and selects the first option whose value differs from the
    /// currently-selected one, WITHOUT triggering a rate refresh (no "update rate" click).
    /// Returns the newly-selected display label, or null when the dropdown is absent or offers
    /// no alternative value to switch to.
    /// </summary>
    public static async Task<string?> SelectDifferentValueForCoverageAsync(IPage page, IAutomationLogger? logger, CoverageEnums coverage)
    {
        var id = coverage.ToString();
        var ngSelect = GetDropdownLocator(page, id);

        try
        {
            await ngSelect.First.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 8000 });
        }
        catch
        {
            logger?.Warning($"Coverage dropdown '{id}' not visible — cannot change its value.");
            return null;
        }

        var originLabel = await GetSelectedDisplayValueAsync(page, coverage);
        var panel = page.Locator(".ng-dropdown-panel .ng-option");

        try
        {
            await ngSelect.First.ClickAsync();
            await panel.First.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 8000 });
            await page.WaitForTimeoutAsync(200);

            var count = await panel.CountAsync();
            for (int i = 0; i < count; i++)
            {
                var option = panel.Nth(i);
                var text = (await option.TextContentAsync())?.Trim() ?? string.Empty;
                if (string.IsNullOrEmpty(text)) continue;
                if (originLabel != null && Normalize(text).Equals(Normalize(originLabel), StringComparison.OrdinalIgnoreCase))
                    continue;

                await option.ClickAsync();
                await panel.First.WaitForAsync(new() { State = WaitForSelectorState.Hidden, Timeout = 5000 });
                await page.WaitForTimeoutAsync(150);

                var newLabel = await GetSelectedDisplayValueAsync(page, coverage);
                logger?.Info($"Coverage '{coverage}': changed selection from '{originLabel}' to '{newLabel}' (no rate refresh).");
                return newLabel;
            }

            // No alternative option — close the panel and leave the selection untouched.
            await TrySuppressEscapeAsync(page);
            logger?.Info($"Coverage '{coverage}': only one option ('{originLabel}') available — nothing to change.");
            return null;
        }
        catch (Exception ex)
        {
            await TrySuppressEscapeAsync(page);
            logger?.Warning($"Coverage '{coverage}': failed to change selection ({ex.Message.Split('\n')[0]}).");
            return null;
        }
    }

    /// <summary>
    /// For every coverage the carrier exposes, switches the dropdown to a value different from its
    /// origin (no rate refresh). Returns a map of coverage → newly-selected label for the dropdowns
    /// that were actually changed (dropdowns with a single option are skipped).
    /// </summary>
    public static async Task<IReadOnlyDictionary<CoverageEnums, string>> ChangeAllCoveragesToDifferentValuesAsync(
        IPage page, IAutomationLogger? logger, CarrierEnums carrier)
    {
        var changed = new Dictionary<CoverageEnums, string>();
        foreach (var coverage in CarrierCoverageData.GetCoverageTypesForCarrier(carrier))
        {
            var newLabel = await SelectDifferentValueForCoverageAsync(page, logger, coverage);
            if (!string.IsNullOrWhiteSpace(newLabel))
                changed[coverage] = newLabel!;
        }

        if (changed.Count == 0)
            logger?.Warning($"No coverage dropdowns were changed for carrier {carrier} — every dropdown had a single option or was unavailable.");
        else
            logger?.Info($"Changed {changed.Count} coverage dropdown(s) for {carrier}: " +
                         string.Join(", ", changed.Select(kv => $"{kv.Key}='{kv.Value}'")));

        // Let each change's /v1/action POST reach the server before the caller closes the quote.
        // NetworkIdle is unusable here — continuous Quantum Metric beacons mean the page never idles.
        await page.WaitForTimeoutAsync(2000);

        return changed;
    }

    /// <summary>
    /// Reads the currently-selected display label for each coverage the carrier exposes.
    /// Coverages whose dropdown/label is not present are omitted.
    /// </summary>
    public static async Task<IReadOnlyDictionary<CoverageEnums, string>> ReadSelectedValuesAsync(
        IPage page, CarrierEnums carrier)
    {
        var selected = new Dictionary<CoverageEnums, string>();
        foreach (var coverage in CarrierCoverageData.GetCoverageTypesForCarrier(carrier))
        {
            var label = await GetSelectedDisplayValueAsync(page, coverage);
            if (!string.IsNullOrWhiteSpace(label))
                selected[coverage] = label!;
        }
        return selected;
    }

    public static async Task<decimal?> GetCoverageAAmountAsync(IPage page)
    {
        var locator = page.Locator("xpath=//span[normalize-space()='Repair/rebuild dwelling']/ancestor::div[contains(@class,'info-title-container')]//p[contains(@class,'info-amount')]//span[1]");
        if (await locator.CountAsync() == 0) return null;
        var raw = (await locator.First.TextContentAsync())?.Trim();
        if (string.IsNullOrWhiteSpace(raw)) return null;
        raw = raw.Replace("$", "").Replace(",", "");
        return decimal.TryParse(raw, out var val) ? val : null;
    }

    #endregion

    #region Public API (Validation)

    /// <summary>
    /// Executes full coverage validation for a carrier/state combination. Throws on failure, logs success otherwise.
    /// </summary>
    public static async Task ValidateCoveragesForStateAsync(
        IPage page,
        IAutomationLogger? logger,
        CarrierEnums carrier,
        string state,
        CoverageAssertionOptions? options)
    {
        var coverageTypes = CarrierCoverageData.GetCoverageTypesForCarrier(carrier);
        decimal? coverageA = null;
        bool coverageAFetched = false;
        decimal? selectedAllPerilsDollar = null;
        var failures = new List<CoverageFailure>();

        // Wait for coverage dropdowns to be ready before starting validation
        if (coverageTypes.Length > 0)
        {
            var firstCoverageId = coverageTypes[0].ToString();
            var firstDropdownSelector = string.Format(CoverageDropdownSelectorTemplate, firstCoverageId);
            var firstDropdown = page.Locator(firstDropdownSelector);
            
            try
            {
                await firstDropdown.First.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15000 });
                logger?.Debug("First coverage dropdown detected, waiting for page to stabilize...");
                // Additional wait to ensure Angular has fully initialized all dropdowns
                await page.WaitForTimeoutAsync(500);
            }
            catch (Exception ex)
            {
                logger?.Warning($"First coverage dropdown did not become visible within timeout. Validation may fail: {ex.Message}");
            }
        }

        foreach (var coverage in coverageTypes)
        {
            var rawValues = CarrierCoverageData.GetCoverageValues(carrier, coverage, state);

            // Wind/Hail N/A: validate the not-editable + mirrors-Standard contract, not the option
            // list (see kb partner:pgr-covmod-wind-hail-na).
            if (coverage == CoverageEnums.WindHail && options?.WindHailIsNotApplicable == true)
            {
                await ValidateWindHailNotApplicableAsync(page, logger, failures);
                continue;
            }

            // Dynamic filtering (Wind/Hail minimum percent)
            if (coverage == CoverageEnums.WindHail && options?.MinWindHailPercent is { } minPct)
            {
                if (!coverageAFetched)
                {
                    coverageA = await GetCoverageAAmountAsync(page);
                    coverageAFetched = true;
                    if (coverageA == null)
                        logger?.Warning("WindHail filtering: Coverage A amount unavailable � dollar value filtering may be skipped.");
                }
                decimal? minDollar = coverageA.HasValue ? Math.Round(coverageA.Value * minPct / 100m, 0) : null;
                rawValues = rawValues.Where(v =>
                {
                    if (v.Type == CoverageValueType.PercentOfCovA)
                        return v.Value.HasValue && (decimal)v.Value.Value >= minPct;
                    if (v.Type == CoverageValueType.Dollar && v.Value.HasValue && minDollar.HasValue)
                        return (decimal)v.Value.Value >= minDollar.Value;
                    return v.Type != CoverageValueType.Dollar || !minDollar.HasValue;
                }).ToArray();
            }

            if (rawValues.Length == 0)
            {
                logger?.Info($"No expected values for {carrier}.{coverage} in {state} � skipping.");
                continue;
            }

            if (coverage == CoverageEnums.WindHail && selectedAllPerilsDollar == null)
            {
                selectedAllPerilsDollar = await GetSelectedDollarForCoverageAsync(page, CoverageEnums.AllPerils);
                if (selectedAllPerilsDollar.HasValue)
                    logger?.Info($"Detected selected All Perils deductible: ${selectedAllPerilsDollar.Value:N0}");
            }

            if (coverageA == null && !coverageAFetched && rawValues.Any(v => v.Type == CoverageValueType.PercentOfCovA))
            {
                coverageA = await GetCoverageAAmountAsync(page);
                coverageAFetched = true;
                if (coverageA == null)
                    logger?.Warning("Could not determine Coverage A base amount � percent calculations may fail.");
            }

            var dropdownResult = await FetchNgSelectOptionsAsync(page, coverage.ToString(), logger);
            if (dropdownResult.Options.Length == 0)
            {
                var expectedTypes = string.Join(",", rawValues.Select(r => r.Type.ToString()));
                logger?.LogDataValidation($"Coverage-{coverage}", false, expectedTypes, "<empty>", dropdownResult.FailureReason);
                failures.Add(new CoverageFailure(coverage, expectedTypes, "<empty>", dropdownResult.FailureReason));
                continue;
            }
            var actualOptions = dropdownResult.Options;

            ValidateCoverageList(logger, new CoverageValidationContext(coverage, rawValues, actualOptions, coverageA, selectedAllPerilsDollar), failures);
            
            // Small delay between coverage validations to allow page to stabilize in headless mode
            await page.WaitForTimeoutAsync(100);
        }

        if (failures.Count > 0)
        {
            var summaryLines = failures
                .GroupBy(f => f.Coverage)
                .Select(g => $"{g.Key}: " + string.Join("; ", g.Select(f => $"Expected '{f.Expected}' Actual '{f.Actual}' ({f.Reason})")));
            var summary = string.Join(Environment.NewLine, summaryLines);
            logger?.Error("Coverage validation failures:\n" + summary);
            throw new CoverageValidationException(summary);
        }
        logger?.Info($"All expected coverage values matched for carrier {carrier} state {state}.");
    }

    #endregion

    #region Core Validation Helpers

    /// <summary>
    /// Validates the Wind/Hail Not-Applicable contract in the open editor — not editable, and value
    /// mirrors the selected Standard (All Perils) deductible. Breaches are added to
    /// <paramref name="failures"/> to join the aggregated <see cref="CoverageValidationException"/>.
    /// See kb partner:pgr-covmod-wind-hail-na.
    /// </summary>
    private static async Task ValidateWindHailNotApplicableAsync(
        IPage page, IAutomationLogger? logger, List<CoverageFailure> failures)
    {
        // Poll for a STABLE editable state, not a point-in-time read — the editor renders
        // progressively and the W/H ng-select flickers under load (kb partner:pgr-covmod-wind-hail-na).
        var stablyEditable = await IsWindHailStablyEditableAsync(page);
        if (stablyEditable)
        {
            const string reason =
                "Wind/Hail is Not-Applicable for this quote but the Custom package still renders an " +
                "editable Wind/Hail dropdown; it should be non-editable and mirror the Standard deductible.";
            logger?.LogDataValidation("Coverage-WindHail-NA", false, "non-editable", "editable dropdown visible", reason);
            failures.Add(new CoverageFailure(CoverageEnums.WindHail, "non-editable (mirrors Standard)", "editable dropdown visible", reason));
        }
        else
        {
            logger?.Info("Wind/Hail is Not-Applicable and correctly renders no editable dropdown.");
        }

        // Mirrors-Standard check. The value may sit in a read-only ng-select label or a static row,
        // so the reader falls back between them.
        var standardDollar = await GetDeductibleDisplayDollarAsync(page, CoverageEnums.AllPerils, StandardDisplayTitles);
        var windHailDollar = await GetDeductibleDisplayDollarAsync(page, CoverageEnums.WindHail, WindHailDisplayTitles);

        if (standardDollar is null)
        {
            const string reason = "Could not read the selected Standard (All Perils) deductible to compare Wind/Hail against.";
            logger?.LogDataValidation("Coverage-WindHail-NA", false, "Standard deductible value", "<not found>", reason);
            failures.Add(new CoverageFailure(CoverageEnums.WindHail, "Standard deductible value", "<not found>", reason));
            return;
        }

        var expected = $"${standardDollar.Value:N0}";
        if (windHailDollar is null)
        {
            var reason = $"Wind/Hail displayed value not found; expected it to mirror the Standard deductible ({expected}).";
            logger?.LogDataValidation("Coverage-WindHail-NA", false, expected, "<not found>", reason);
            failures.Add(new CoverageFailure(CoverageEnums.WindHail, expected, "<not found>", reason));
            return;
        }

        var actual = $"${windHailDollar.Value:N0}";
        var mirrors = windHailDollar.Value == standardDollar.Value;
        logger?.LogDataValidation("Coverage-WindHail-NA", mirrors, expected, actual,
            mirrors ? "Wind/Hail mirrors the Standard deductible." : "Wind/Hail does not mirror the Standard deductible.");
        if (!mirrors)
        {
            failures.Add(new CoverageFailure(CoverageEnums.WindHail, expected, actual,
                "Wind/Hail is Not-Applicable and should mirror the selected Standard (All Perils) deductible."));
        }
    }

    private static void ValidateCoverageList(
        IAutomationLogger? logger,
        CoverageValidationContext ctx,
        List<CoverageFailure> failures)
    {
        var (coverage, expectedValues, actualOptions, coverageA, selectedAllPerilsDollar) = ctx;
        var expectedDisplayList = new List<string>();
        var normalizedAcceptableTokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var belowAopDisplayList = new List<string>();

        foreach (var expected in expectedValues)
        {
            var display = BuildExpectedDisplayString(expected, coverageA);
            if (string.IsNullOrWhiteSpace(display))
                continue;

            var skipped = ShouldSkipWindHailValue(coverage, expected, selectedAllPerilsDollar);

            // Always register as acceptable (so it's never flagged as Unexpected if the UI still shows it)
            normalizedAcceptableTokens.Add(Normalize(display));
            if (expected.Type == CoverageValueType.PercentOfCovA && expected.Value.HasValue)
            {
                var formattedPercent = FormatPercent(expected.Value.Value);
                normalizedAcceptableTokens.Add(Normalize($"{formattedPercent}%"));
                normalizedAcceptableTokens.Add(Normalize($"({formattedPercent}%)"));
            }

            if (skipped)
            {
                if (!belowAopDisplayList.Contains(display))
                    belowAopDisplayList.Add(display);
            }
            else if (!expectedDisplayList.Contains(display))
                expectedDisplayList.Add(display);
        }

        // Fail on below-AOP values that are still visible in the dropdown (known UI bug)
        if (belowAopDisplayList.Count > 0 && selectedAllPerilsDollar.HasValue)
        {
            var visibleBelowAop = belowAopDisplayList
                .Where(d => IsOptionPresent(actualOptions, d))
                .ToList();

            if (visibleBelowAop.Count > 0)
            {
                var belowAopDetails =
                    $"Wind/Hail options below the selected AOP deductible (${selectedAllPerilsDollar.Value:N0}) " +
                    $"are still visible in the dropdown: {string.Join(", ", visibleBelowAop)}. " +
                    $"These values should be filtered out once AOP is selected/re-selected.";

                logger?.Error($"[Wind/Hail] {belowAopDetails}");
                failures.Add(new CoverageFailure(coverage, string.Join(", ", expectedDisplayList), string.Join(", ", actualOptions), belowAopDetails));
            }
        }

        var missing = expectedDisplayList
            .Where(ed => !IsOptionPresent(actualOptions, ed))
            .ToList();

        var unexpected = actualOptions
            .Select(o => new { Raw = o, Norm = Normalize(o) })
            .Where(a => !normalizedAcceptableTokens.Contains(a.Norm))
            .Select(a => a.Raw)
            .Distinct()
            .ToList();

        var expectedJoined = expectedDisplayList.Count == 0 ? "<none>" : string.Join(", ", expectedDisplayList);
        var actualJoined = actualOptions.Length == 0 ? "<empty>" : string.Join(", ", actualOptions);
        var passed = missing.Count == 0 && (unexpected.Count == 0 || expectedDisplayList.Count == 0);

        var detailParts = new List<string>();
        if (missing.Count > 0) detailParts.Add($"Missing: {string.Join(", ", missing)}");
        if (unexpected.Count > 0 && expectedDisplayList.Count > 0) detailParts.Add($"Unexpected: {string.Join(", ", unexpected)}");
        var details = detailParts.Count == 0 ? "All expected coverage values present" : string.Join(" | ", detailParts);

        logger?.LogDataValidation($"Coverage-{coverage}", passed, expectedJoined, actualJoined, details);
        if (!passed)
        {
            failures.Add(new CoverageFailure(coverage, expectedJoined, actualJoined, details));
        }
    }

    #endregion

    #region Formatting & Rules

    public static bool ShouldSkipWindHailValue(CoverageEnums coverage, CoverageValue expected, decimal? selectedAllPerilsDollar) =>
        coverage == CoverageEnums.WindHail &&
        expected.Type == CoverageValueType.Dollar &&
        expected.Value.HasValue &&
        selectedAllPerilsDollar.HasValue &&
        (decimal)expected.Value.Value < selectedAllPerilsDollar.Value;

    public static string BuildExpectedDisplayString(CoverageValue value, decimal? coverageA) =>
        value.Type switch
        {
            CoverageValueType.Dollar => value.Value.HasValue ? $"${value.Value.Value:N0}" : string.Empty,
            CoverageValueType.PercentOfCovA => (coverageA.HasValue && value.Value.HasValue)
                ? $"${Math.Round(coverageA.Value * (decimal)value.Value.Value / 100m, 0):N0} ({FormatPercent(value.Value.Value)}%)"
                : (value.Value.HasValue ? $"{FormatPercent(value.Value.Value)}%" : string.Empty),
            CoverageValueType.None => "None",
            _ => string.Empty
        };

    private static string FormatPercent(double percent)
    {
        // Format percent value - remove trailing zeros and unnecessary decimal point
        // e.g., 0.5 -> "0.5", 1.0 -> "1", 2.5 -> "2.5"
        return percent % 1 == 0 ? percent.ToString("0") : percent.ToString("0.#");
    }

    public static bool IsOptionPresent(string[] actualOptions, string expectedDisplay)
    {
        if (actualOptions.Any(a => Normalize(a).Equals(Normalize(expectedDisplay), StringComparison.OrdinalIgnoreCase)))
            return true;
        if (!expectedDisplay.Contains('%')) return false;
        var percentTokenMatch = PercentTokenRegex.Match(expectedDisplay);
        if (!percentTokenMatch.Success) return false;
        var token = percentTokenMatch.Groups[1].Value + "%"; // e.g. 2%
        return actualOptions.Any(a => Normalize(a).Contains(Normalize(token)));
    }

    public static string Normalize(string s) => s.Replace(" ", string.Empty).Replace(",", string.Empty).Trim();

    #endregion

    #region Internal Helpers

    private static ILocator GetDropdownLocator(IPage page, string id) =>
        page.Locator(string.Format(CoverageDropdownSelectorTemplate, id));

    /// <summary>
    /// Point-in-time read: is the Wind/Hail dropdown an editable ng-select right now (present, visible,
    /// not disabled)? Absent/hidden/disabled all read as false.
    /// </summary>
    private static async Task<bool> IsWindHailEditableNowAsync(IPage page)
    {
        var ngSelect = GetDropdownLocator(page, CoverageEnums.WindHail.ToString());
        if (await ngSelect.CountAsync() == 0) return false;
        if (!await ngSelect.First.IsVisibleAsync()) return false;
        var disabled = await ngSelect.First.EvaluateAsync<bool>("el => el.classList.contains('ng-select-disabled')");
        return !disabled;
    }

    /// <summary>
    /// Whether Wind/Hail is editable, read as a STABLE value to tolerate the editor's progressive
    /// render: polls <see cref="IsWindHailEditableNowAsync"/> until identical across
    /// <c>requiredStableSamples</c> consecutive reads, or the timeout elapses (last read wins).
    /// </summary>
    private static async Task<bool> IsWindHailStablyEditableAsync(
        IPage page, int requiredStableSamples = 3, int sampleIntervalMs = 250, int timeoutMs = 6000)
    {
        var deadline = timeoutMs / Math.Max(1, sampleIntervalMs);
        bool last = await IsWindHailEditableNowAsync(page);
        int stable = 1;
        for (int i = 0; i < deadline && stable < requiredStableSamples; i++)
        {
            await page.WaitForTimeoutAsync(sampleIntervalMs);
            var current = await IsWindHailEditableNowAsync(page);
            stable = current == last ? stable + 1 : 1;
            last = current;
        }
        return last;
    }

    // Deductible titles a Not-Applicable Wind/Hail row can carry (default plus the FL/NY overrides
    // from DeductibleDefinitions), matched case-insensitively when reading its static display value.
    private static readonly string[] WindHailDisplayTitles =
        ["Wind/Hail deductible", "Hurricane deductible", "Wind deductible"];

    // Standard / All Perils deductible titles (default plus the FL non-hurricane override).
    private static readonly string[] StandardDisplayTitles =
        ["Standard deductible", "Non-Hurricane deductible"];

    /// <summary>
    /// Reads a deductible's displayed dollar value robustly: the read-only ng-select label first (in
    /// case the value is shown inside a dropdown), then a static display row matched by any of the
    /// given <paramref name="displayTitles"/> (title → sibling <c>info-amount</c>). Used for both the
    /// Standard and the Not-Applicable Wind/Hail deductibles, whose N/A layout renders as static rows.
    /// </summary>
    private static async Task<decimal?> GetDeductibleDisplayDollarAsync(
        IPage page, CoverageEnums coverage, string[] displayTitles)
    {
        var fromLabel = await GetSelectedDollarForCoverageAsync(page, coverage);
        if (fromLabel is not null) return fromLabel;

        foreach (var title in displayTitles)
        {
            var amount = await GetDeductibleDisplayDollarByTitleAsync(page, title);
            if (amount is not null) return amount;
        }
        return null;
    }

    /// <summary>
    /// Reads the displayed dollar amount of a deductible display row identified by its title span
    /// (the value lives in the row's <c>info-amount</c>). Returns null when the row or amount is absent.
    /// </summary>
    private static async Task<decimal?> GetDeductibleDisplayDollarByTitleAsync(IPage page, string title)
    {
        var titleSpan = page.Locator("p.info-title span")
            .Filter(new LocatorFilterOptions { HasTextRegex = new Regex(@"^\s*" + Regex.Escape(title) + @"\s*$", RegexOptions.IgnoreCase) });
        if (await titleSpan.CountAsync() == 0) return null;

        var row = titleSpan.First.Locator(
            "xpath=ancestor::*[contains(@class,'info-title-container') or contains(@class,'info-container')][1]");
        var amount = row.Locator("p.info-amount");
        if (await amount.CountAsync() == 0) return null;

        return ExtractFirstDollarAmount((await amount.First.TextContentAsync())?.Trim());
    }


    private static async Task<DropdownFetchResult> FetchNgSelectOptionsAsync(IPage page, string id, IAutomationLogger? logger)
    {
        var ngSelect = GetDropdownLocator(page, id);

        try
        {
            await ngSelect.First.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 8000 });
        }
        catch
        {
            var reason = $"Coverage dropdown '{id}' did not appear within the timeout � " +
                         $"the coverage section may not have fully rendered yet. " +
                         $"This is likely a timing or rendering issue, not a data bug.";
            logger?.Warning(reason);
            return new DropdownFetchResult([], reason);
        }

        var panel = page.Locator(".ng-dropdown-panel .ng-option");

        for (int attempt = 1; attempt <= 2; attempt++)
        {
            try
            {
                await ngSelect.First.ClickAsync();
                await panel.First.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = attempt == 1 ? 5000 : 8000 });
                await page.WaitForTimeoutAsync(200);

                var texts = await panel.EvaluateAllAsync<string[]>("opts => opts.map(o => (o.textContent ?? '').trim())");
                await TrySuppressEscapeAsync(page);

                var options = texts.Where(t => !string.IsNullOrEmpty(t)).ToArray();
                if (options.Length > 0)
                    return new DropdownFetchResult(options);

                if (attempt < 2)
                {
                    logger?.Debug($"Dropdown '{id}' opened but had no visible options on attempt {attempt}, retrying after delay...");
                    await page.WaitForTimeoutAsync(1500);
                    continue;
                }

                return new DropdownFetchResult([],
                    $"Dropdown '{id}' opened but contained no options � " +
                    $"this coverage may have no available values for the selected carrier/state combination, " +
                    $"or the coverage data has not yet loaded.");
            }
            catch (Exception ex)
            {
                await TrySuppressEscapeAsync(page);
                if (attempt < 2)
                {
                    logger?.Debug($"Dropdown options did not load for '{id}' on attempt {attempt} ({ex.Message.Split('\n')[0]}), retrying after delay...");
                    await page.WaitForTimeoutAsync(1500);
                    continue;
                }

                return new DropdownFetchResult([],
                    $"Dropdown '{id}' exists but options did not load after {attempt} attempt(s) � " +
                    $"the coverage data may not have been populated yet. " +
                    $"This is likely a timing or rendering issue, not necessarily a data bug.");
            }
        }

        return new DropdownFetchResult([], $"Dropdown '{id}' could not be read after multiple attempts.");
    }

    private static async Task TrySuppressEscapeAsync(IPage page)
    {
        try { await page.Keyboard.PressAsync("Escape"); } catch { /* best effort close */ }
    }

    private static decimal? ExtractFirstDollarAmount(string? display)
    {
        if (string.IsNullOrWhiteSpace(display)) return null;
        var match = DollarRegex.Match(display);
        if (!match.Success) return null;
        return decimal.TryParse(match.Groups[1].Value.Replace(",", ""), out var val) ? val : null;
    }

    #endregion
}
