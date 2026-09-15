using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Exceptions;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.FormData.Common;
using Bolt.Automation.FrontEnds.FormData.Helpers;
using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using D2CBase = Bolt.Automation.FrontEnds.Projects.D2C.Base.D2CBase;

namespace Bolt.Automation.FrontEnds.Projects.D2C.Pages;

public class D2C_LobsPage(
    IBrowserManager browserManager,
    IPageHelper pageHelper,
    IScopeContext scopeContext,
    bool validatePageReady = true,
    IAutomationLogger? logger = null)
    : D2CBase(browserManager, pageHelper, scopeContext, validatePageReady, logger)
{
    #region Locators
    private const string RadioButtonsContainerLocator = "app-radio-buttons-multi-selection.online-lobs span.title";
    private const string SelectedLobsLocator = "button.radio-button-item.checked";
    #endregion

    protected override string PageIdentifier => "lobs";
    protected override string PageName => "LOBs Selection Page";

    public override async Task ValidatePageReady()
    {
        await PageHelper.ValidatePageReadyAsync(PageIdentifier, PageName, timeout: 30000);
        await CaptureApplicationIdsFromSessionStorageAsync();
    }

    public override async Task FillForm(Dictionary<string, string>? formData = null)
    {
        var (pageSpecificData, fields) = FormDataHelper.GetFieldsForPage(this, ScopeContext, formData);
        if (pageSpecificData.TryGetValue(FieldNames.Lob, out string? lobValue) && !string.IsNullOrEmpty(lobValue))
        {
            await SelectLob(lobValue);
        }
    }

    public async Task SelectLob(string lobValue)
    {
        var lobs = lobValue.Split(',', StringSplitOptions.RemoveEmptyEntries)
                          .Select(lob => lob.Trim())
                          .ToArray();
        
        foreach (var lob in lobs)
        {
            await SelectSingleLob(lob);
        }
    }
    public async Task SelectSingleLob(string lobValue)
    {
        try
        {
            var lobField = FieldWithValue(FieldNames.Lob, lobValue);
            lobField.InteractionOptions = new ElementInteractionOptions
            {
                ForceInteractionIfNotVisible = true,
                Value = lobValue
            };
            await PageHelper.InteractWithElement(lobField);
        }
        catch (Exception ex)
        {
            throw new PageElementException($"LOB '{lobValue}'", $"failed to select on page [{PageName}]: {ex.Message}");
        }
    }

    public async Task<List<string>> GetSelectedLobs()
    {
        var selectedElements = await Page.Locator(SelectedLobsLocator).AllAsync();
        var selectedLobs = new List<string>();
        foreach (var element in selectedElements)
        {
            if (await element.IsVisibleAsync())
            {
                string text = await element.TextContentAsync() ?? "";
                selectedLobs.Add(text.Trim());
            }
        }
        return selectedLobs;
    }
}