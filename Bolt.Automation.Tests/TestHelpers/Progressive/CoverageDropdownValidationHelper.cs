using Bolt.Automation.Common.Enums;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.Common.Logging.Extentions;
using Bolt.Automation.FrontEnds.Projects.HQXConsumer.Helpers;
using Bolt.Automation.FrontEnds.Projects.HQXConsumer.Pages;
using Bolt.Automation.InternalServices.Database.Queries.Main;

namespace Bolt.Automation.Tests.TestHelpers.Progressive;

/// <summary>
/// Helper for coverage modification dropdown validation.
/// Uses <see cref="ProgressiveTestHelper"/> for generic flow steps
/// and adds coverage-specific operations on top.
/// </summary>
public class CoverageDropdownValidationHelper(
    IAutomationLogger logger,
    IMainQueries mainQueries)
{
    /// <summary>
    /// Fetches the Wind/Hail percentage threshold from the database for the given carrier and quote.
    /// </summary>
    public async Task<decimal?> GetWindHailThresholdAsync(string externalId, CarrierEnums carrier)
    {
        var minWindHailPercent = await mainQueries.ResultDataLogic.GetPercentFromFreeTextAsync(
            externalId, carrier, CoverageEnums.WindHail);

        logger.Info(minWindHailPercent.HasValue
            ? $"Carrier Wind/Hail percent retrieved: {minWindHailPercent}% (filtering expected values below this)."
            : "No Wind/Hail percent returned for carrier/result.");

        return minWindHailPercent;
    }

    /// <summary>
    /// Whether Wind/Hail is Not-Applicable for this quote (Homesite-only per <c>ba-homesite-wind-hail-na</c>;
    /// other carriers always return false). Unknown (no/unparseable attachment) is treated as normal
    /// (editable). See kb partner:pgr-covmod-wind-hail-na.
    /// </summary>
    public async Task<bool> IsWindHailNotApplicableAsync(string externalId, CarrierEnums carrier)
    {
        if (carrier != CarrierEnums.Homesite)
        {
            logger.Info($"Wind/Hail N/A check is Homesite-only; carrier {carrier} uses the normal Wind/Hail dropdown validation.");
            return false;
        }

        var notApplicable = await mainQueries.ResultDataLogic.IsWindHailNotApplicableAsync(externalId, carrier);

        logger.Info(notApplicable switch
        {
            true => "Wind/Hail is Not-Applicable for this Homesite quote (XWindFlag=false and no WindHailDeductible returned); " +
                    "expecting it to mirror the Standard deductible and to be non-editable in the Custom package.",
            false => "Homesite returned an editable Wind/Hail offer (WindHailDeductible present); expecting the normal Wind/Hail dropdown.",
            null => "Wind/Hail N/A determination unavailable for this Homesite quote; defaulting to the normal Wind/Hail dropdown validation."
        });

        return notApplicable ?? false;
    }

    /// <summary>
    /// Opens the coverage editor on the Rates page.
    /// </summary>
    public async Task OpenCoverageEditorAsync(HQXConsumer_RatesPage ratesPage)
    {
        await ratesPage.ClickEditCoverageAsync();
    }

    /// <summary>
    /// In the open coverage editor, changes every coverage dropdown to a value different from its
    /// origin without submitting ("update rate"). Returns the map of coverage → newly-selected label.
    /// Fails the test if no dropdown could be changed (nothing to verify persistence on).
    /// </summary>
    public async Task<IReadOnlyDictionary<CoverageEnums, string>> ChangeAllCoveragesToNonOriginAsync(
        HQXConsumer_RatesPage ratesPage, CarrierEnums carrier)
    {
        var changed = await ratesPage.ChangeAllCoveragesToDifferentValuesAsync(carrier);
        if (changed.Count == 0)
        {
            throw new InvalidOperationException(
                $"No coverage dropdown could be changed to a non-origin value for carrier {carrier}; " +
                "the Custom package may not have rendered editable dropdowns.");
        }
        logger.Info($"Custom package now shows changed (unsubmitted) selections: " +
                    string.Join(", ", changed.Select(kv => $"{kv.Key}='{kv.Value}'")));
        return changed;
    }

    /// <summary>
    /// Reads the currently-selected label of every coverage dropdown the carrier exposes in the
    /// open coverage editor.
    /// </summary>
    public Task<IReadOnlyDictionary<CoverageEnums, string>> ReadSelectedCoverageValuesAsync(
        HQXConsumer_RatesPage ratesPage, CarrierEnums carrier) =>
        ratesPage.ReadSelectedCoverageValuesAsync(carrier);

    /// <summary>
    /// Validates that the coverage dropdown values match expectations for the given carrier and state.
    /// </summary>
    public async Task ValidateCoverageDropdownsAsync(
        HQXConsumer_RatesPage ratesPage,
        CarrierEnums carrier,
        string stateCd,
        CoverageAssertionOptions options)
    {
        await ratesPage.AssertCoveragesForStateAsync(carrier, stateCd, options);
    }

}
