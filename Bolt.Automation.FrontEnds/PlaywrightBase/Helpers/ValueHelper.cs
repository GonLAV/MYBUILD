using Bolt.Automation.Common.Exceptions;
using Microsoft.Playwright;

namespace Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;

public class ValueHelper(IPage page)
{
    public async Task<string> GetValue(LocatorType locatorType, string locatorValue, ValueType valueType = ValueType.Text, int timeout = 0, bool waitForVisibility = false, int retries = 3)
    {
        timeout = timeout > 0 ? timeout : 10000;
        ILocator locator = GetLocator(locatorType, locatorValue);
        var locatedElement = await WaitForElementAsync(locator, timeout, waitForVisibility, retries);
        if (locatedElement == null)
            throw new ElementNotFoundException($"Element with {locatorType} '{locatorValue}' not found after {retries} retries.");
        valueType = await AdjustValueTypeIfNeeded(locatedElement, valueType);
        return await ExtractValueBasedOnType(locatedElement, valueType);
    }

    private async Task<ValueType> AdjustValueTypeIfNeeded(ILocator element, ValueType valueType)
    {
        if (valueType != ValueType.Text) return valueType;
        string tagName = await GetTagNameAsync(element);
        if (tagName == "input" || tagName == "select" || tagName == "textarea")
        {
            string inputType = await element.EvaluateAsync<string>("el => (el.getAttribute('type') || '').toLowerCase()");
            if (inputType == "checkbox" || inputType == "radio")
                return ValueType.IsChecked;
            else
                return ValueType.Value;
        }
        if (tagName == "label")
        {
            string associatedInputType = await element.EvaluateAsync<string>(
                "el => { const id = el.getAttribute('for'); const input = id ? document.getElementById(id) : el.querySelector('input'); return input ? (input.getAttribute('type') || '').toLowerCase() : ''; }");
            if (associatedInputType == "checkbox" || associatedInputType == "radio")
                return ValueType.IsChecked;
        }
        return valueType;
    }

    private async Task<string> ExtractValueBasedOnType(ILocator element, ValueType valueType)
    {
        if (valueType == ValueType.IsChecked && await GetTagNameAsync(element) == "label")
        {
            bool isChecked = await element.EvaluateAsync<bool>(
                "el => { const id = el.getAttribute('for'); const input = id ? document.getElementById(id) : el.querySelector('input[type=checkbox],input[type=radio]'); return input ? input.checked : false; }");
            return isChecked.ToString();
        }
        return valueType switch
        {
            ValueType.Text => await element.TextContentAsync() ?? string.Empty,
            ValueType.Value => await element.InputValueAsync() ?? string.Empty,
            ValueType.IsChecked => (await element.IsCheckedAsync()).ToString(),
            ValueType.IsVisible => (await element.IsVisibleAsync()).ToString(),
            _ => throw new TestSetupException($"Unsupported value type: {valueType}")
        };
    }

    private async Task<string> GetTagNameAsync(ILocator element)
    {
        return await element.EvaluateAsync<string>("el => el.tagName.toLowerCase()");
    }

    private ILocator GetLocator(LocatorType locatorType, string locatorValue)
    {
        return locatorType switch
        {
            LocatorType.CSS => page.Locator(locatorValue),
            LocatorType.XPath => page.Locator($"xpath={locatorValue}"),
            LocatorType.Text => page.GetByText(locatorValue),
            LocatorType.Role => page.GetByRole(Enum.Parse<AriaRole>(locatorValue)),
            LocatorType.TestId => page.GetByTestId(locatorValue),
            LocatorType.Placeholder => page.GetByPlaceholder(locatorValue),
            LocatorType.Label => page.GetByLabel(locatorValue),
            LocatorType.Title => page.GetByTitle(locatorValue),
            LocatorType.Alt => page.GetByAltText(locatorValue),
            LocatorType.Name => page.GetByAltText(locatorValue),
            _ => throw new ArgumentException($"Unsupported locator type: {locatorType}")
        };
    }

    private async Task<ILocator?> WaitForElementAsync(ILocator locator, int timeout = 10000, bool waitForVisibility = false, int retries = 3)
    {
        for (int attempt = 0; attempt < retries; attempt++)
        {
            try
            {
                await locator.WaitForAsync(new LocatorWaitForOptions { State = waitForVisibility ? WaitForSelectorState.Visible : WaitForSelectorState.Attached, Timeout = timeout });
                return locator;
            }
            catch (TimeoutException)
            {
                if (attempt == retries - 1)
                    throw new ElementNotFoundException($"Failed to find the element: {locator} after {retries} attempts.");
                await Task.Delay(1000);
            }
        }
        return null;
    }

    public class ElementNotFoundException : Exception
    {
        public ElementNotFoundException(string message) : base(message) { }
        public ElementNotFoundException(string message, Exception innerException) : base(message, innerException) { }
    }
}
