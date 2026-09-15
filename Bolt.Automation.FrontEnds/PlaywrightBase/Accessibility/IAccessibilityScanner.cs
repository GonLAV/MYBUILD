using Microsoft.Playwright;

namespace Bolt.Automation.FrontEnds.PlaywrightBase.Accessibility;

/// <summary>Runs axe against a page or a region of one.</summary>
public interface IAccessibilityScanner
{
    /// <summary>Scans the whole page and labels the result with the screen and state under test.</summary>
    Task<AccessibilityScanResult> ScanAsync(
        IPage page,
        string screen,
        string state,
        AccessibilityScanOptions? options = null);

    /// <summary>Scans only the region the locator resolves to — use for a newly revealed question.</summary>
    Task<AccessibilityScanResult> ScanAsync(
        ILocator locator,
        string screen,
        string state,
        AccessibilityScanOptions? options = null);
}
