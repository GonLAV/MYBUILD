using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.Interview.Base;
using Microsoft.Playwright;

namespace Bolt.Automation.FrontEnds.Projects.Interview.Pages;

public class ProductMarketSelectionsPageCL : InterviewBase
{
    public ProductMarketSelectionsPageCL(
        IBrowserManager browserManager,
        IPageHelper pageHelper,
        IScopeContext scopeContext,
        bool validatePageReady = true,
        IAutomationLogger? logger = null)
        : base(browserManager, pageHelper, scopeContext, validatePageReady, logger) { }

    protected override string PageIdentifier => "CL_MarketSelections";
    protected override string PageName => "Market Selections Page";

    /// <summary>
    /// Ticks "Quote all eligible" and confirms it stuck, returning false when the control is absent
    /// (no carrier is eligible online, so there is nothing to select and the page advances as-is).
    /// The appetite block is re-created when eligibility data resolves, which silently discards a tick
    /// applied a moment earlier - Playwright reports the check as succeeded and the box is pristine again -
    /// so the state is re-read and re-applied rather than trusted.
    /// </summary>
    public async Task<bool> EnsureQuoteAllEligibleSelectedAsync(int attempts = 4, int settleMs = 700)
    {
        var label = Page.Locator("label.checkbox-control:has-text(\"Quote all eligible\")");

        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            if (await label.CountAsync() == 0)
            {
                _logger?.Info("'Quote all eligible' is not on the page - no carrier is eligible online, nothing to select.");
                return false;
            }

            if (await label.First.Locator("input[type=checkbox]").IsCheckedAsync())
            {
                _logger?.Info($"'Quote all eligible' is selected (confirmed on attempt {attempt}).");
                return true;
            }

            _logger?.Info($"'Quote all eligible' is not selected - ticking it (attempt {attempt}/{attempts}).");
            await label.First.ClickAsync();
            await Page.WaitForTimeoutAsync(settleMs);
        }

        throw new InvalidOperationException(
            $"'Quote all eligible' would not stay selected after {attempts} attempts on the Market Selections page. " +
            "Without a carrier selected the page will not advance, so the flow would time out waiting for Results.");
    }

    /// <summary>
    /// Selects a specific carrier on the market selections page.
    /// Call after deselecting "Quote all eligible" to target a single carrier.
    /// Waits briefly for the DOM to update before trying to click.
    /// </summary>
    public async Task SelectCarrierAsync(string carrierName)
    {
        // Allow Angular to update the DOM after QuoteAllEligible is unchecked
        await Task.Delay(800);

        // Primary: clickable carrier-item <a> (no non-clickable class) — used in BOP/CL carousel
        var primary = Page.Locator(
            $"//a[contains(@class,'carrier-item') and not(contains(@class,'non-clickable'))][.//img[contains(@alt,'{carrierName}')]]");

        // Fallback: checkbox-control element containing carrier name or img alt
        var checkboxControl = Page.Locator(
            $"//*[contains(@class,'checkbox-control')][.//img[contains(@alt,'{carrierName}')] or contains(normalize-space(.), '{carrierName}')]");

        // Second fallback: any label wrapping the carrier img
        var labelFallback = Page.Locator(
            $"//label[.//img[contains(@alt,'{carrierName}')]]");

        foreach (var locator in new[] { primary, checkboxControl, labelFallback })
        {
            try
            {
                await locator.First.WaitForAsync(new LocatorWaitForOptions
                {
                    State = WaitForSelectorState.Visible,
                    Timeout = 3000
                });
                if (await locator.CountAsync() > 0)
                {
                    await locator.First.ClickAsync();
                    _logger?.Info($"Selected carrier: {carrierName}");
                    return;
                }
            }
            catch (TimeoutException) { /* try next locator */ }
        }

        throw new InvalidOperationException(
            $"Carrier '{carrierName}' not found on Market Selections page. " +
            $"Ensure 'Quote all eligible' was unchecked before calling this method.");
    }
}
