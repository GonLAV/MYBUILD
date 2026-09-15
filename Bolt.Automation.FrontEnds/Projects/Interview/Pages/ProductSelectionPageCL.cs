using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.Interview.Base;
using Bolt.Automation.FrontEnds.FormData.Common;
using Bolt.Automation.FrontEnds.FormData.Helpers;
using Microsoft.Playwright;

namespace Bolt.Automation.FrontEnds.Projects.Interview.Pages;
    public class ProductSelectionPageCL : InterviewBase
{
    public ProductSelectionPageCL(
        IBrowserManager browserManager,
        IPageHelper pageHelper,
        IScopeContext scopeContext,
        bool validatePageReady = true,
        IAutomationLogger? logger = null)
        : base(browserManager, pageHelper, scopeContext, validatePageReady, logger) { }

    protected override string PageIdentifier => "CL_ProductSelection";
    protected override string PageName => "Product Selection Page";

    public override async Task FillForm(Dictionary<string, string>? formData = null)
    {
        var (pageSpecificData, fields) = FormDataHelper.GetFieldsForPage(this, ScopeContext, formData);
        if (pageSpecificData.TryGetValue(FieldNames.Lob, out string? lobValue) && !string.IsNullOrEmpty(lobValue))
        {
            await SelectLob(lobValue);
        }
    }

    public async Task SelectLob(string lobName)
    {
        var normalizedLob = lobName.Replace(" ", "").Trim();
        var cards = Page.Locator("app-product-card");
        var count = await cards.CountAsync();

        // A quote started from a brand-new account (no prior Case Manager data to warm the page)
        // renders the LOB cards a beat later than an existing-account quote - the immediate
        // CountAsync() above races that render. Only retry after a bounded wait when it comes back
        // empty, so an existing-account quote (cards already attached) pays no extra round trip.
        if (count == 0)
        {
            try
            {
                await cards.First.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Attached, Timeout = 10000 });
                count = await cards.CountAsync();
            }
            catch (TimeoutException)
            {
                _logger?.Warning("No product cards attached on Product Selection Page after 10s wait");
            }
        }

        for (int i = 0; i < count; i++)
        {
            var card = cards.Nth(i);
            var titleLocator = card.Locator("[class*='product-title']").First;
            if (await titleLocator.CountAsync() == 0)
                continue;

            var titleText = (await titleLocator.TextContentAsync() ?? "").Replace(" ", "").Trim();
            if (titleText.Contains(normalizedLob, StringComparison.OrdinalIgnoreCase))
            {
                var checkbox = card.Locator("input[type='checkbox'], input[type='radio']").First;
                if (await checkbox.CountAsync() > 0 && await checkbox.IsCheckedAsync())
                {
                    _logger?.Info($"LOB '{lobName}' already selected; skipping click");
                    return;
                }

                await card.Locator("label").First.ClickAsync();
                _logger?.Info($"Selected LOB: {lobName}");
                return;
            }
        }

        _logger?.Warning($"LOB '{lobName}' not found on Product Selection Page");
    }
}
