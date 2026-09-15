using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.FormData.Base;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.Interview.Base;
using Microsoft.Playwright;
using static Bolt.Automation.FrontEnds.FormData.Common.FieldNames;
using static Bolt.Automation.FrontEnds.Projects.Interview.FormData.FieldNames;

namespace Bolt.Automation.FrontEnds.Projects.Interview.Pages;

public class ProductBusinessProfilePageCL : InterviewBase
{
    public ProductBusinessProfilePageCL(
        IBrowserManager browserManager,
        IPageHelper pageHelper,
        IScopeContext scopeContext,
        bool validatePageReady = true,
        IAutomationLogger? logger = null)
        : base(browserManager, pageHelper, scopeContext, validatePageReady, logger) { }

    protected override string PageIdentifier => "CL_BusinessProfile";
    protected override string PageName => "Business Profile Page";
    private const string LobContainerLocator = "//*[contains(@class,'PolicyData.Lobs[]')]";
    private const string LobItemLocator = "//*[contains(@class,'PolicyData.Lobs[]')]//li";
    private const string IndustryControlLocator = "//*[contains(@class,'PolicyData.EposNaicDescription')]";
    // Group headers render as role=option too, but ng-select marks them disabled.
    private const string IndustryOptionLocator = "div[role='option']:not(.ng-option-disabled)";

    public override async Task FillForm(Dictionary<string, string>? formData = null)
    {
        if (formData == null)
        {
            await base.FillForm(null);
            return;
        }

        // Extract fields we handle specially on the consumer CL start page
        formData.TryGetValue(Lob, out var lobValue);
        formData.TryGetValue(EposNaicDescription, out var industryValue);

        // This page is untagged for EposNaicDescription in the registry (the generic fill pass would
        // raw-Fill the ng-select), so the registry DefaultValue has to be resolved here instead.
        if (string.IsNullOrEmpty(industryValue))
            industryValue = ResolveRegistryDefault(EposNaicDescription);

        // Fill industry FIRST - the ng-select typeahead needs focus to move away to commit the value
        if (!string.IsNullOrEmpty(industryValue))
        {
            await FillIndustryTypeahead(industryValue);
        }

        // base.FillForm will click on other fields, committing the industry selection
        await base.FillForm(formData);

        // Consumer CL flow: LOB selection is on the Start Page (not a separate LobsPage)
        if (!string.IsNullOrEmpty(lobValue) && await IsLobSelectionPresent())
        {
            await SelectLob(lobValue);
        }
    }

    private string? ResolveRegistryDefault(string fieldName) =>
        ProjectContextManager.GetFieldRegistry(ScopeContext).TryGetValue(fieldName, out var field)
            ? field.DefaultValue
            : null;


    private async Task<bool> IsLobSelectionPresent()
    {
        var lobContainer = Page.Locator(LobContainerLocator);
        return await lobContainer.CountAsync() > 0;
    }

    public async Task SelectLob(string lobName)
    {
        var lobItems = Page.Locator(LobItemLocator);
        var count = await lobItems.CountAsync();

        for (int i = 0; i < count; i++)
        {
            var item = lobItems.Nth(i);
            var text = await item.TextContentAsync() ?? "";

            // Match by partial text (e.g. "WorkersCompensation" matches "Workers Compensation")
            var normalizedText = text.Replace(" ", "").Trim();
            var normalizedLob = lobName.Replace(" ", "").Trim();

            if (normalizedText.Contains(normalizedLob, StringComparison.OrdinalIgnoreCase))
            {
                var checkbox = item.Locator("input[type='checkbox']");
                if (!await checkbox.IsCheckedAsync())
                {
                    await item.Locator("label").ClickAsync();
                    _logger?.Info($"Selected LOB: {text.Trim()}");
                }
                return;
            }
        }

        _logger?.Warning($"LOB '{lobName}' not found on Start Page");
    }

    public async Task FillIndustryTypeahead(string searchText)
    {
        var control = Page.Locator(IndustryControlLocator).First;
        var committed = control.Locator($".ng-value-label:has-text({Quote(searchText)})");
        if (await committed.CountAsync() > 0) return;

        // A quote started from an existing account arrives with a different industry prefilled, and
        // ng-select ignores a new search term until the current value is cleared.
        var clear = control.Locator(".ng-clear-wrapper");
        if (await clear.CountAsync() > 0)
            await clear.First.ClickAsync();

        var input = control.Locator("input[role='combobox']").First;
        await input.ClickAsync();
        await input.PressSequentiallyAsync(searchText, new LocatorPressSequentiallyOptions { Delay = 80 });

        await control.Locator($"{IndustryOptionLocator}:has-text({Quote(searchText)})").First
            .ClickAsync(new LocatorClickOptions { Timeout = 20000 });

        await committed.First.WaitForAsync(
            new LocatorWaitForOptions { State = WaitForSelectorState.Attached, Timeout = 20000 });
        // A new (never-prefilled) account can render two loader elements at once (page-level +
        // control-level); waiting on the raw comma-separated Loader selector (or `.First`) is a
        // strict-mode multi-match (troubleshooting:strict-mode-multimatch), so each match is waited
        // on individually instead.
        foreach (var loader in await Page.Locator(Loader).AllAsync())
        {
            await loader.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden, Timeout = 30000 });
        }
        _logger?.Info($"Industry classification set to '{searchText}'");

        await DismissAlertModalAsync();
    }

    // :has-text() takes a quoted string; escape quotes in the term (NAICS titles contain apostrophes).
    private static string Quote(string value) => $"\"{value.Replace("\"", "\\\"")}\"";
}