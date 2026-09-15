using Bolt.Automation.Common;
using Bolt.Automation.Common.Enums;
using Bolt.Automation.Common.Logging.Extentions;
using Bolt.Automation.FrontEnds.Projects.HQXConsumer.Helpers;
using Bolt.Automation.FrontEnds.Projects.HQXConsumer.Pages;
using Bolt.Automation.InternalServices.Database.Queries.Main;
using Bolt.Automation.TestDataProvider.TestData.PlatformAPITestData;
using Bolt.Automation.Tests.TestData.Progressive;
using Bolt.Automation.Tests.TestExtension.Attributes;
using Bolt.Automation.Tests.TestExtension.Base;
using Bolt.Automation.Tests.TestHelpers.Progressive;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using AuthorAttribute = Bolt.Automation.Tests.TestExtension.Attributes.AuthorAttribute;

namespace Bolt.Automation.Tests.Tests.Progressive.CovMod;

/// <summary>
/// Coverage modification (Custom package) tests: dropdown-value validation across carriers
/// (a State-Carrier permutation per case) and unsubmitted-selection persistence across a
/// close/reopen.
/// </summary>
public class CovModDropdownTests : ProgressiveUITestBase
{
    // Meadville, PA — the persistence test's CovMod address. On QA only Homesite (of the CovMod
    // carriers) rates here, so that test drives the Homesite Custom package.
    private const AddressKey CovModPersistenceAddress = AddressKey.PA_Meadville;
    private const CarrierEnums CovModPersistenceCarrier = CarrierEnums.Homesite;

    private CoverageDropdownValidationHelper _coverageHelper = null!;

    protected override void ResolveServices()
    {
        base.ResolveServices();
        _coverageHelper = new CoverageDropdownValidationHelper(
        _logger,
        _uiTestScope.ServiceProvider.GetService<IMainQueries>()!);
    }

    [Test]
    [Tenant(Tenant.PROGRESSIVEPL)]
    [Author(Author.Helen)]
    [TestCaseSource(typeof(CovModTestCases), nameof(CovModTestCases.DropdownCases))]
    [Category("CoverageModification")]
    [Category("Progressive")]
    [Description("Validates custom coverage package dropdown values against defined package rules in HQX 2.0")]
    public async Task PGR_HQX2_CovMod_Dropdown_Values(AddressKey addressKey, CarrierEnums carrier)
    {
        var address = Addresses.GetAddress(addressKey);
        ScopeContext.Set(ctx => ctx.CurrentUser, TestContextAccessor.CurrentUserCollection.Consumer);

        var (externalId, _) = await _progressiveHelper.CallQuoteStartAsync(address);
        await _progressiveHelper.NavigateToRatesPageAsync();
        var ratesPage = await _progressiveHelper.EnsureCarrierSelectedAsync(carrier);

        var (minWindHailPercent, windHailNotApplicable) = await _logger.ExecuteStepAsync("Fetch Wind/Hail expectations from database", async () =>
        {
            var threshold = await _coverageHelper.GetWindHailThresholdAsync(externalId!, carrier);
            var notApplicable = await _coverageHelper.IsWindHailNotApplicableAsync(externalId!, carrier);
            return (threshold, notApplicable);
        });

        await _logger.ExecuteStepAsync("Open coverage editor and validate dropdown values", async () =>
        {
            await _coverageHelper.OpenCoverageEditorAsync(ratesPage);
            await _coverageHelper.ValidateCoverageDropdownsAsync(ratesPage, carrier, address.StateProvCd, new CoverageAssertionOptions
            {
                MinWindHailPercent = minWindHailPercent,
                WindHailIsNotApplicable = windHailNotApplicable
            });
        });
    }

    /// <summary>
    /// Custom (coverage-modification) package retains unsubmitted coverage selections after a
    /// close/reopen as consumer (HQX 2.0). Fails red by design — this is a live QA defect: the
    /// changes POST to /v1/action on each change but reopen restores ORIGIN values, not the changes.
    /// Homesite only (the sole CovMod carrier in appetite at PA_Meadville).
    /// </summary>
    [Test]
    [Tenant(Tenant.PROGRESSIVEPL)]
    [Author(Author.Helen)]
    [Category("CoverageModification")]
    [Category("Progressive")]
    [Category("Regression")]
    [TestCaseId(252130)]
    [Description("Custom package retains unsubmitted coverage selections after closing and reopening the quote as consumer (HQX 2.0). Lands red on bug 250237.")]
    public async Task PGR_HQX2_CovMod_Custom_Package_Retains_Unsubmitted_Selections()
    {
        var carrier = CovModPersistenceCarrier;
        var address = Addresses.GetAddress(CovModPersistenceAddress);
        ScopeContext.Set(ctx => ctx.CurrentUser, TestContextAccessor.CurrentUserCollection.Consumer);

        var quote = await _progressiveHelper.CreateConsumerQuoteAndSettleAsync(address);
        var ratesPage = await _progressiveHelper.SubmitToRatesPageAsync(quote.OverviewPage);

        await _progressiveHelper.EnsureCarrierSelectedAsync(carrier);
        await _coverageHelper.OpenCoverageEditorAsync(ratesPage);

        var changedSelections = await _logger.ExecuteStepAsync(
            "Change every Custom-package coverage dropdown to a non-origin value (no submit)",
            async () => await _coverageHelper.ChangeAllCoveragesToNonOriginAsync(ratesPage, carrier));

        // Fresh browser = a true close: consumer flow has no close control, and a same-page nav
        // would keep the SPA's in-memory state alive and not exercise a real reopen.
        await _logger.ExecuteStepAsync("Close the quote (open a fresh browser)",
            async () => await BrowserManager.OpenNewWindowAsync());

        var reopenedRatesPage = await _progressiveHelper.CallQuoteRetrievalAsync<HQXConsumer_RatesPage>(
            quote.FriendlyId, quote.LastName, quote.ZipCode);

        var actualSelections = await _logger.ExecuteStepAsync("Reopen the Custom package and read the coverage selections", async () =>
        {
            await _progressiveHelper.EnsureCarrierSelectedAsync(carrier);
            await _coverageHelper.OpenCoverageEditorAsync(reopenedRatesPage);
            return await _coverageHelper.ReadSelectedCoverageValuesAsync(reopenedRatesPage, carrier);
        });

        // Fails red on current QA: selections are not retained across close/reopen (see summary).
        Assert.Multiple(() =>
        {
            foreach (var (coverage, expectedLabel) in changedSelections)
            {
                actualSelections.TryGetValue(coverage, out var actualLabel);
                Assert.That(actualLabel, Is.Not.Null,
                    $"Coverage '{coverage}' dropdown was absent after reopen (expected the unsubmitted selection '{expectedLabel}')");
                Assert.That(HQXConsumer_RatesPage.NormalizeCoverageLabel(actualLabel!), Is.EqualTo(HQXConsumer_RatesPage.NormalizeCoverageLabel(expectedLabel)).IgnoreCase,
                    $"DEFECT: Custom package did not retain the unsubmitted selection for '{coverage}' after close/reopen — " +
                    $"expected '{expectedLabel}' but reopened as '{actualLabel}' (origin value).");
            }
        });
    }
}
