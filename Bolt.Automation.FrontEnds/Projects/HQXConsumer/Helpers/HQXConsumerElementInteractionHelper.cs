using Bolt.Automation.Common.Exceptions;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;
using Microsoft.Playwright;

namespace Bolt.Automation.FrontEnds.Projects.HQXConsumer.Helpers
{
    public class HQXConsumerElementInteractionHelper(IPage page, IAutomationLogger? logger = null)
        : ElementInteractionHelper(page, logger)
    {
        // Override dropdown selection for HQXConsumer specifics
        protected override async Task SelectDropdown(ILocator dropDown, string selectValue, string elementName, int timeout, bool isMultiOverride = false)
        {
            // Custom HQXConsumer dropdown logic here
            // Example: click, type, filter, or handle custom rendering
            await dropDown.ClickAsync(new() { Timeout = timeout });
            await Task.Delay(200); // let dropdown open
            var dropdownPanel = dropDown.Page.Locator(".ng-dropdown-panel");
            await dropdownPanel.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = timeout });
            await Task.Delay(100);
            // Optionally type to filter
            // await dropDown.FillAsync(selectValue);
            var optionLocator = dropdownPanel.Locator(".ng-option").Filter(new LocatorFilterOptions { HasText = selectValue });
            if (await optionLocator.CountAsync() == 0)
                throw new PageElementException($"Option '[{selectValue}]' in dropdown '[{elementName}]'", "HQXConsumer dropdown override.");
            await optionLocator.First.ClickAsync(new() { Timeout = timeout });
            await Task.Delay(200);
            await dropdownPanel.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden, Timeout = timeout });
        }
    }
}
