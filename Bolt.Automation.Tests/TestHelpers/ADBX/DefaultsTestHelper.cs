using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.Common.Logging.Extentions;
using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.ADBX.FormData;
using Bolt.Automation.FrontEnds.Projects.ADBX.Popups;
using NUnit.Framework;
using static Bolt.Automation.FrontEnds.Projects.ADBX.FormData.ADBX_FieldNames;

namespace Bolt.Automation.Tests.TestHelpers.ADBX;

/// <summary>
/// Reusable helper for Defaults Management UI test steps:
/// grid search, record creation/update, and verification.
/// </summary>
public class DefaultsTestHelper(
    IAutomationLogger logger,
    IPageFactory pageFactory,
    IPageHelper pageHelper)
{
    /// <summary>
    /// Applies the given search criteria to the defaults grid and returns the first row's data.
    /// Returns null when no matching records are found.
    /// </summary>
    public async Task<Dictionary<string, string>?> SearchDefaultsAsync(Dictionary<string, string> criteria)
    {
        foreach (var kvp in criteria)
        {
            FieldRegistryADBX.Fields[kvp.Key].DefaultValue = kvp.Value;
            await pageHelper.InteractWithField(kvp.Key, new ElementInteractionOptions { Timeout = 5000 });
        }
        await pageHelper.InteractWithField(DefaultsGridSearchButton);

        if (!await pageHelper.IsTableDisplayed() || await pageHelper.GetRowCount() == 0)
        {
            logger.Info("No matching defaults records found");
            return null;
        }

        var rowData = await pageHelper.GetRowData(1);
        logger.Info($"Found record: {string.Join(", ", rowData.Select(kvp => $"{kvp.Key}={kvp.Value}"))}");
        return rowData;
    }

    /// <summary>Creates a new defaults record using the provided field values.</summary>
    public async Task CreateDefaultsRecordAsync(Dictionary<string, string> dataSet)
    {
        await pageHelper.InteractWithField(DefaultsGridAddButton);
        var popup = pageFactory.CreatePage<ADBX_AddDefaultsRecordPopup>();
        logger.Info($"Creating record with: {string.Join(", ", dataSet.Select(kvp => $"{kvp.Key}={kvp.Value}"))}");
        await popup.FillForm(dataSet);
        await popup.ClickPopupConfirm();
    }

    /// <summary>
    /// Finds the row identified by <paramref name="currentValue"/> in the Value column,
    /// opens its edit popup, and updates only the specified editable fields.
    /// </summary>
    public async Task UpdateDefaultsRecordAsync(string currentValue, Dictionary<string, string> editableFields)
    {
        await pageHelper.ClickTableCellButtonAsync("value", currentValue, "edit");
        var popup = pageFactory.CreatePage<ADBX_AddDefaultsRecordPopup>();
        await popup.ClearEditableFileds();
        logger.Info($"Updating fields: {string.Join(", ", editableFields.Select(kvp => $"{kvp.Key}={kvp.Value}"))}");
        foreach (var kvp in editableFields)
            await popup.FillForm(new Dictionary<string, string> { [kvp.Key] = kvp.Value });
        await popup.ClickPopupConfirm();
    }

    /// <summary>
    /// Re-searches the defaults grid and asserts that the first record contains the expected field values.
    /// </summary>
    public async Task VerifyDefaultsRecordAsync(Dictionary<string, string> searchCriteria, Dictionary<string, string> expectedFields)
    {
        foreach (var kvp in searchCriteria)
        {
            FieldRegistryADBX.Fields[kvp.Key].DefaultValue = kvp.Value;
            await pageHelper.InteractWithField(kvp.Key);
        }
        await pageHelper.InteractWithField(DefaultsGridSearchButton);

        Assert.That(await pageHelper.GetRowCount(), Is.GreaterThan(0), "No rows found after operation");

        var rowData = await pageHelper.GetRowData(1);
        logger.Info($"Final row: {string.Join(", ", rowData.Select(kvp => $"{kvp.Key}={kvp.Value}"))}");

        foreach (var expected in expectedFields)
        {
            var actual = rowData.GetValueOrDefault(expected.Key, "");
            Assert.That(actual, Does.Contain(expected.Value),
                $"{expected.Key} mismatch. Expected to contain: '{expected.Value}', Actual: '{actual}'");
        }

        logger.Info($"Verified: {string.Join(", ", expectedFields.Select(kvp => $"{kvp.Key}={rowData.GetValueOrDefault(kvp.Key, "")}"))}");
    }
}
