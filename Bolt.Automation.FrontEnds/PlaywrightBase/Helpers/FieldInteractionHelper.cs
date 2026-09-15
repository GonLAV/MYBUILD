using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Exceptions;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.FormData;
using Bolt.Automation.FrontEnds.FormData.Base;
using Bolt.Automation.FrontEnds.FormData.Enums;
using Microsoft.Playwright;

namespace Bolt.Automation.FrontEnds.PlaywrightBase.Helpers
{
    public class FieldInteractionHelper
    {
        private readonly IPage _page;
        private readonly ElementInteractionHelper _elementInteraction;
        private readonly ValueHelper _valueHelper;
        private readonly IAutomationLogger? _logger;
        private readonly IScopeContext? _scopeContext;

        public FieldInteractionHelper(IPage page, ElementInteractionHelper elementInteraction,
            IAutomationLogger? logger = null, IScopeContext? scopeContext = null)
        {
            _page = page ?? throw new ArgumentNullException(nameof(page));
            _elementInteraction = elementInteraction ?? throw new ArgumentNullException(nameof(elementInteraction));
            _logger = logger;
            _scopeContext = scopeContext;
            _valueHelper = new ValueHelper(page);
        }
        public async Task<PageHelper> InteractWithField(string fieldName, string? value = null, PageHelper pageHelper = null!)
        {
            _logger?.Debug($"Starting field interaction for '{fieldName}' with value '{value ?? "<default>"}'");

            var field = GetFieldFromRegistry(fieldName);
            var element = value != null ? field[value] : field;
            element.InteractionOptions = new ElementInteractionOptions
            {
                Value = value,
                IgnoreIfNotFound = false
            };

            return await _elementInteraction.InteractWithElement(element, pageHelper);
        }

        public async Task<PageHelper> InteractWithField(string fieldName, ElementInteractionOptions options, PageHelper pageHelper = null!)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            _logger?.Debug($"Starting field interaction for '{fieldName}' with options");

            var field = GetFieldFromRegistry(fieldName);
            var locatorValue = options.Value ?? options.Values?.FirstOrDefault();
            var element = locatorValue != null ? field[locatorValue] : field;

            options.IgnoreIfNotFound = false;
            element.InteractionOptions = options;

            return await _elementInteraction.InteractWithElement(element, pageHelper);
        }

        public async Task<string> GetFieldValue(string fieldName, ValueType valueType = ValueType.Text, int timeout = 0, bool waitForVisibility = false, int retries = 3)
        {
            var field = GetFieldFromRegistry(fieldName);
            // Resolve the locator placeholder with the field's DefaultValue rather than an empty string.
            // Per-option radio fields register their locator as a format string (e.g.
            // "input[id='RoofResponsible_{0}']"), so an empty substitution produces the dead locator
            // "input[id='RoofResponsible_']" which matches nothing and times out. Substituting the
            // DefaultValue ("false") resolves to a concrete option that exists on the page — mirroring
            // ElementExists' fallback (see the UIElement overload below). For SelectedValue reads the
            // located element only needs to exist; GetElementValue then scans the page for the actually
            // checked option. GetLocators ignores the value for non-placeholder locators, so this is a
            // no-op for every field without a "{0}" token.
            var resolved = new UIElement
            {
                Strategy = field.Strategy,
                Locators = field.GetLocators(field.DefaultValue ?? string.Empty),
                FieldType = field.FieldType,
                DefaultValue = field.DefaultValue,
                Pages = field.Pages
            };
            var result = await TryGetValueWithLocators(resolved, fieldName, valueType, timeout, waitForVisibility, retries);
            _logger?.Debug($"GetFieldValue '{fieldName}' [{valueType}] → '{result}'");
            return result;
        }

        public Task<string> GetFieldValue(LocatorType locatorType, string locatorValue, ValueType valueType = ValueType.Text, int timeout = 0, bool waitForVisibility = false, int retries = 3)
        {
            return _valueHelper.GetValue(locatorType, locatorValue, valueType, timeout, waitForVisibility, retries);
        }

        public async Task<IReadOnlyList<string>> GetFieldDropdownListValues(string fieldName, int timeout = 5000, bool openDropdown = true)
        {
            var field = GetFieldFromRegistry(fieldName);

            if (field.FieldType is not (UIFieldType.Dropdown or UIFieldType.MultiDropdown))
                throw new InvalidOperationException($"Field '{fieldName}' is not a dropdown field type.");

            foreach (var rawLocator in field.Locators)
            {
                try
                {
                    var locator = CreateLocator(field.Strategy, rawLocator);
                    await locator.WaitForAsync(new LocatorWaitForOptions
                    {
                        State = WaitForSelectorState.Attached,
                        Timeout = timeout
                    });

                    if (openDropdown)
                        await locator.ClickAsync(new LocatorClickOptions { Timeout = timeout });

                    var tagName = await locator.EvaluateAsync<string>("el => el.tagName.toLowerCase()");
                    IReadOnlyList<string> values;

                    if (tagName == "select")
                    {
                        values = await locator.Locator("option").AllTextContentsAsync();
                    }
                    else
                    {
                        var optionsLocator = _page.Locator(".ng-dropdown-panel .ng-option");
                        await optionsLocator.First.WaitForAsync(new LocatorWaitForOptions
                        {
                            State = WaitForSelectorState.Visible,
                            Timeout = timeout
                        });
                        values = await optionsLocator.AllTextContentsAsync();
                    }

                    if (openDropdown)
                        await _page.Keyboard.PressAsync("Escape");

                    var normalized = values
                        .Select(v => v.Trim())
                        .Where(v => !string.IsNullOrWhiteSpace(v))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToArray();

                    _logger?.Debug($"GetFieldDropdownListValues '{fieldName}' → '{string.Join(", ", normalized)}'");
                    return normalized;
                }
                catch (Exception ex)
                {
                    _logger?.Debug($"GetFieldDropdownListValues: locator '{rawLocator}' failed — {ex.Message}");
                }
            }

            throw new PageElementException(fieldName, "could not read dropdown values using any locator");
        }

        public async Task<bool> ElementExists(string fieldName, int timeout = 1000, string? expectedPlaceholder = null)
        {
            var field = GetFieldFromRegistry(fieldName);

            if (!string.IsNullOrEmpty(expectedPlaceholder))
            {
                try
                {
                    return await CheckElementWithPlaceholder(field, expectedPlaceholder, timeout);
                }
                catch
                {
                    return false;
                }
            }

            return await ElementExists(field, timeout);
        }

        public async Task<bool> ElementExists(LocatorType locatorType, string locatorValue, int timeout = 1000, string? expectedPlaceholder = null)
        {
            if (string.IsNullOrEmpty(expectedPlaceholder))
            {
                return await CheckElementExistence(() => GetFieldValue(locatorType, locatorValue, ValueType.Text, timeout));
            }

            try
            {
                var playwrightLocator = CreateLocator(locatorType, locatorValue);
                await playwrightLocator.WaitForAsync(new LocatorWaitForOptions 
                { 
                    State = WaitForSelectorState.Attached, 
                    Timeout = timeout 
                });

                var placeholder = await playwrightLocator.GetAttributeAsync("placeholder");
                var exists = placeholder != null && placeholder.Contains(expectedPlaceholder, StringComparison.OrdinalIgnoreCase);
                
                _logger?.Debug($"Element {(exists ? "found" : "not found")} with placeholder '{expectedPlaceholder}'");
                return exists;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ElementExists(UIElement field, int timeout = 1000, string? placeholderValue = null)
        {
            foreach (var rawLocator in field.Locators)
            {
                try
                {
                    // Format locator if placeholder value is provided or DefaultValue exists
                    var locator = !string.IsNullOrEmpty(placeholderValue) 
                        ? string.Format(rawLocator, placeholderValue)
                        : !string.IsNullOrEmpty(field.DefaultValue)
                            ? string.Format(rawLocator, field.DefaultValue)
                            : rawLocator;

                    var playwrightLocator = CreateLocator(field.Strategy, locator);
                    await playwrightLocator.WaitForAsync(new LocatorWaitForOptions 
                    { 
                        State = WaitForSelectorState.Attached, 
                        Timeout = timeout 
                    });

                    _logger?.Debug($"Element found using locator '{locator}'");
                    return true;
                }
                catch (Exception ex)
                {
                    _logger?.Debug($"Locator '{rawLocator}' failed: {ex.Message}");
                }
            }

            return false;
        }

        public async Task<bool> IsFieldEnabled(string fieldName, int timeout = 3000)
        {
            var field = GetFieldFromRegistry(fieldName);

            foreach (var locator in field.Locators)
            {
                try
                {
                    var playwrightLocator = CreateLocator(field.Strategy, locator);
                    await playwrightLocator.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Attached, Timeout = timeout });

                    var hasDisabledClass = await playwrightLocator.EvaluateAsync<bool>("el => el.classList.contains('ng-select-disabled')");
                    var isEnabled = await playwrightLocator.IsEnabledAsync();

                    return isEnabled && !hasDisabledClass;
                }
                catch (Exception ex)
                {
                    _logger?.Debug($"Failed to check enabled state for locator '{locator}': {ex.Message}");
                }
            }

            throw new PageElementException(fieldName, "could not find enabled state using any locator");
        }

        private async Task<bool> CheckElementExistence(Func<Task<string>> getValueFunc)
        {
            try
            {
                await getValueFunc();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task<string> TryGetValueWithLocators(UIElement field, string fieldName, ValueType valueType, int timeout, bool waitForVisibility, int retries)
        {
            Exception? lastException = null;
            bool multipleLocators = field.Locators.Count > 1;

            foreach (var locator in field.Locators)
            {
                try
                {
                    if (multipleLocators)
                        _logger?.Debug($"Trying locator '{locator}' for '{fieldName}'");
                    var locatorObj = CreateLocator(field.Strategy, locator);
                    return await GetElementValue(locatorObj, valueType);
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    _logger?.Debug($"Locator '{locator}' failed for '{fieldName}': {ex.Message}");
                }
            }

            throw CreateGetValueException(field, fieldName, lastException);
        }

        private async Task<string> GetElementValue(ILocator locatorObj, ValueType valueType = ValueType.Text)
        {
            var tagName = await locatorObj.EvaluateAsync<string>("el => el.tagName.toLowerCase()");

            if (valueType == ValueType.IsChecked)
            {
                return await locatorObj.EvaluateAsync<bool>(
                    "el => { const input = el.tagName === 'LABEL' ? (document.getElementById(el.getAttribute('for')) ?? el.querySelector('input[type=checkbox],input[type=radio]')) : el; return input ? input.checked : false; }")
                    .ContinueWith(t => t.Result.ToString());
            }

            if (valueType == ValueType.SelectedValue)
            {
                return await locatorObj.EvaluateAsync<string>(
                    "el => { const labels = el.tagName === 'LABEL' ? [el] : Array.from(document.querySelectorAll('label')); " +
                    "const checked = labels.find(l => { const input = document.getElementById(l.getAttribute('for')); return input && input.checked; }); " +
                    "if (!checked) return ''; const forAttr = checked.getAttribute('for') ?? ''; const idx = forAttr.lastIndexOf('_'); return idx >= 0 ? forAttr.substring(idx + 1) : forAttr; }");
            }

            return tagName switch
            {
                "input" or "textarea" => await locatorObj.InputValueAsync(),
                "select" => await locatorObj.EvaluateAsync<string>(
                    "el => el.selectedIndex >= 0 ? el.options[el.selectedIndex].text : ''"),
                _ => await GetCustomElementValue(locatorObj)
            };
        }

        private async Task<string> GetCustomElementValue(ILocator locatorObj)
        {
            var valueElement = locatorObj.Locator(".ng-value-label");
            var count = await valueElement.CountAsync();

            return count > 0
                ? await valueElement.TextContentAsync() ?? string.Empty
                : await locatorObj.TextContentAsync() ?? string.Empty;
        }

        private ILocator CreateLocator(LocatorType strategy, string locator)
        {
            return strategy switch
            {
                LocatorType.CSS => _page.Locator(locator),
                LocatorType.XPath => _page.Locator($"xpath={locator}"),
                LocatorType.Name => _page.Locator($"[name='{locator}']"),
                _ => _page.Locator(locator)
            };
        }

        public async Task<string?> GetFieldAttribute(string fieldName, string attributeName, int timeout = 5000)
        {
            var field = GetFieldFromRegistry(fieldName);

            foreach (var rawLocator in field.Locators)
            {
                try
                {
                    var locator = CreateLocator(field.Strategy, rawLocator);
                    await locator.WaitForAsync(new LocatorWaitForOptions
                    {
                        State = WaitForSelectorState.Attached,
                        Timeout = timeout
                    });
                    var value = await locator.GetAttributeAsync(attributeName);
                    _logger?.Debug($"GetFieldAttribute '{fieldName}' [{attributeName}] → '{value}'");
                    return value;
                }
                catch (Exception ex)
                {
                    _logger?.Debug($"GetFieldAttribute: locator '{rawLocator}' failed — {ex.Message}");
                }
            }

            throw new PageElementException(fieldName, $"could not find element to read attribute '{attributeName}'");
        }

        private Exception CreateGetValueException(UIElement field, string fieldName, Exception? lastException)
        {
            var allLocators = string.Join(", ", field.Locators.Select((loc, idx) => $"#{idx + 1}: '{loc}'"));
            return new Exception(
                $"Failed to get value for field '{fieldName}' using {field.Strategy} strategy. " +
                $"Tried {field.Locators.Count} locator(s): [{allLocators}]. " +
                $"Page URL: {_page.Url}", lastException);
        }

        private UIElement GetFieldFromRegistry(string fieldName)
        {
            var fields = ProjectContextManager.GetFieldRegistry(_scopeContext);

            if (!fields.TryGetValue(fieldName, out var field))
            {
                var availableFields = string.Join(", ", fields.Keys.ToArray());
                throw new PageElementException(fieldName, $"not found. Available fields: {availableFields}");
            }

            field.FieldName = fieldName;
            return field;
        }

        private async Task<bool> CheckElementWithPlaceholder(UIElement field, string expectedPlaceholder, int timeout)
        {
            foreach (var locator in field.Locators)
            {
                try
                {
                    var playwrightLocator = CreateLocator(field.Strategy, locator);
                    await playwrightLocator.WaitForAsync(new LocatorWaitForOptions 
                    { 
                        State = WaitForSelectorState.Attached, 
                        Timeout = timeout 
                    });

                    var placeholder = await playwrightLocator.GetAttributeAsync("placeholder");
                    if (placeholder != null && placeholder.Contains(expectedPlaceholder, StringComparison.OrdinalIgnoreCase))
                    {
                        _logger?.Debug($"Element found with matching placeholder '{expectedPlaceholder}' using locator '{locator}'");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    _logger?.Debug($"Failed checking locator '{locator}': {ex.Message}");
                }
            }

            return false;
        }

        public async Task TriggerValidationAsync(string fieldName, int timeout = 10000)
        {
            var field = GetFieldFromRegistry(fieldName);

            foreach (var rawLocator in field.Locators)
            {
                try
                {
                    var locator = CreateLocator(field.Strategy, rawLocator);
                    await locator.WaitForAsync(new LocatorWaitForOptions
                    {
                        State = WaitForSelectorState.Attached,
                        Timeout = timeout
                    });

                    if (field.FieldType is UIFieldType.Dropdown or UIFieldType.MultiDropdown)
                    {
                        var input = locator.Locator("input").First;
                        if (await input.CountAsync() > 0)
                        {
                            await input.FocusAsync();
                            await input.BlurAsync();
                            return;
                        }

                        await locator.ClickAsync();
                        await _page.Keyboard.PressAsync("Tab");
                        return;
                    }

                    await locator.FocusAsync();
                    await locator.BlurAsync();
                    return;
                }
                catch (Exception ex)
                {
                    _logger?.Debug($"TriggerValidationAsync failed for '{fieldName}' locator '{rawLocator}': {ex.Message}");
                }
            }

            throw new Exception($"Failed to trigger validation for field '{fieldName}' using any locator.");
        }
        public string GetValidationLocator(string fieldName)
        {
            var field = GetFieldFromRegistry(fieldName);

            var locator = field.Validation?.Locator;

            if (string.IsNullOrWhiteSpace(locator))
                throw new InvalidOperationException($"Validation locator is not configured for field '{fieldName}'.");

            return locator;
        }
    }
}