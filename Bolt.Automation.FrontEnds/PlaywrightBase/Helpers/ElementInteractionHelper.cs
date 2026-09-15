using Bolt.Automation.Common.Exceptions;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.FormData;
using Bolt.Automation.FrontEnds.FormData.Enums;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Screenshot;
using Microsoft.Playwright;
using System.Text.RegularExpressions;

namespace Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;

public class ElementInteractionHelper(IPage page, IAutomationLogger? logger = null, IScreenshotManager? screenshotManager = null)
{
    private const int DefaultTimeout = 3000;

    public async Task<PageHelper> InteractWithElement(
        LocatorType locatorType,
        LocatorSet locatorValues,
        ElementAction action,
        ElementInteractionOptions? options = null,
        PageHelper? pageHelper = null)
    {

        if (locatorValues == null || !locatorValues.Any())
        {
            var argumentException = new ArgumentException("LocatorSet cannot be empty");
            logger?.LogException(argumentException, "Element interaction failed due to empty locator set");
            throw argumentException;
        }

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var effectiveTimeout = options?.Timeout > 0 ? options.Timeout : DefaultTimeout;
        var waitTimeout = locatorValues.Count() > 1 ? Math.Max(effectiveTimeout / locatorValues.Count(), 500) : effectiveTimeout;

        foreach (var locatorValue in locatorValues)
        {
            try
            {

                var locator = GetLocator(locatorType, locatorValue);

                // Fast-skip for optional fields: avoid burning the full waitTimeout on
                // elements that are simply not on the page. Conditionally-rendered fields
                // use their configured Timeout as the grace window so backend-loaded content
                // (e.g. dropdowns revealed after a preceding field click) has adequate time.
                if (options?.IgnoreIfNotFound == true && await locator.CountAsync() == 0)
                {
                    var graceMs = options?.Timeout > 500 ? options.Timeout : 500;
                    try
                    {
                        await locator.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Attached, Timeout = graceMs });
                    }
                    catch (TimeoutException) { }

                    if (await locator.CountAsync() == 0)
                        continue;
                }

                // Wait for element to exist before proceeding
                await locator.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Attached, Timeout = waitTimeout });

                if (await locator.IsVisibleAsync())
                {
                    var detail = await Interact(locatorType, locatorValue, action, options);

                    // After ajax-triggering actions (selects, switchers/radios), wait briefly
                    // for any same-origin backend round-trip to land so the next field probe
                    // sees the revealed DOM. Best-effort, budget-bounded, ignores analytics.
                    if (IsAjaxTriggerAction(action) && pageHelper?.RequestTracker != null)
                    {
                        await pageHelper.RequestTracker.WaitForBackendIdleAsync();
                    }

                    sw.Stop();
                    var payload = UiActionLogger.CreatePayload(action.ToString(), "Success", sw.ElapsedMilliseconds, page.Url, new()
                    {
                        { "locatorType", locatorType.ToString() },
                        { "locator", locatorValue },
                        { "detail", detail ?? string.Empty }
                    });
                    logger?.LogUiAction(action.ToString(), locatorValue, payload);
                    return pageHelper!;
                }
                else if (options?.ForceInteractionIfNotVisible == true && action == ElementAction.Click)
                {
                    await ForceElementClick(locatorType, locatorValue);

                    sw.Stop();
                    var payload = UiActionLogger.CreatePayload("ForceClick", "Success", sw.ElapsedMilliseconds, page.Url, new()
                    {
                        { "locatorType", locatorType.ToString() },
                        { "locator", locatorValue },
                        { "detail", "ForceClick (JS)" }
                    });
                    logger?.LogUiAction("ForceClick", locatorValue, payload);
                    return pageHelper!;
                }
            }
            catch (Exception ex)
            {
                logger?.LogException(ex, "Failed to interact with locator '{0}' using {1}", locatorValue, locatorType);
                if (locatorValue == locatorValues.Last() && options?.IgnoreIfNotFound != true)
                {
                    var wrapped = WrapPlaywrightException(ex, action, locatorType, locatorValue);
                    if (wrapped != ex)
                        throw wrapped;
                    throw;
                }
            }
        }

        if (options?.IgnoreIfNotFound == true)
        {
            logger?.Debug($"Element not found but ignoring due to IgnoreIfNotFound option");
            return pageHelper!;
        }

        var locatorDetails = string.Join(", ", locatorValues.Select((loc, idx) => $"#{idx + 1}: '{loc}'"));
        var actionDescription = action.ToString().ToLowerInvariant();

        var exception = new Exception(
            $"Failed to {actionDescription} element using {locatorType} locator strategy. " +
            $"Tried {locatorValues.Count} locator(s): [{locatorDetails}]. " +
            $"All locators either don't exist, are not visible, or failed interaction. " +
            $"Page URL: {page.Url}");

        logger?.LogException(exception, "Element interaction completely failed after trying all locators");
        throw exception;
    }

    public async Task<PageHelper> InteractWithElement(
        LocatorType locatorType,
        LocatorSet locatorValues,
        UIFieldType fieldType,
        string value,
        ElementInteractionOptions? options = null,
        PageHelper? pageHelper = null)
    {
        var action = UIElement.GetActionForFieldType(fieldType, value);
        options ??= new ElementInteractionOptions();
        options.Value = value;
        return await InteractWithElement(locatorType, locatorValues, action, options, pageHelper);
    }

    public async Task<PageHelper> InteractWithElement(UIElement element, PageHelper? pageHelper = null)
    {
        return await InteractWithElement(element, value: null, pageHelper);
    }

    public async Task<PageHelper> InteractWithElement(UIElement element, string? value, PageHelper? pageHelper = null)
    {
        if (element == null)
        {
            var exception = new ArgumentNullException(nameof(element));
            logger?.LogException(exception, "UIElement is null in InteractWithElement");
            throw exception;
        }

        var useValue = value
            ?? element.InteractionOptions?.Value
            ?? element.DefaultValue
            ?? element.InteractionOptions?.Values?.FirstOrDefault()
            ?? string.Empty;

        if (!string.IsNullOrEmpty(element.FieldName))
            logger?.Debug($"Processing field: {element.FieldName} = {useValue}");

        return await InteractWithElement(
            element.Strategy,
            element.GetLocators(useValue),
            element.FieldType,
            useValue,
            element.InteractionOptions,
            pageHelper
        );
    }

    private async Task<string?> Interact(
        LocatorType locatorType,
        string locatorValue,
        ElementAction action,
        ElementInteractionOptions? options = null)
    {
        ValidateRequiredValue(locatorValue, "locator value");
        options ??= new ElementInteractionOptions { Timeout = DefaultTimeout };
        options.Timeout = options.Timeout > 0 ? options.Timeout : DefaultTimeout;
        ILocator locator = GetLocator(locatorType, locatorValue);

        if (!await ElementExists(locator, options.IgnoreIfNotFound))
            return null;

        if (!await WaitForElementVisible(locator, options, action, locatorType, locatorValue))
            return "ForceClick (JS)";

        if (!await CheckElementEnabled(locator, options, action, locatorType, locatorValue))
            return null;

        return await PerformElementAction(locator, action, options, locatorType, locatorValue);
    }

    private async Task<bool> ElementExists(ILocator locator, bool ignoreIfNotFound)
    {
        int elementCount = await locator.CountAsync();
        if (elementCount == 0)
        {
            if (ignoreIfNotFound)
            {
                logger?.Debug("Element not found but ignoring due to IgnoreIfNotFound setting");
                return false;
            }

            var exception = new Exception(
                $"Element not found on page. " +
                $"Locator: '{locator}' returned 0 elements. " +
                $"Page URL: {page.Url}. " +
                $"Page title: '{await page.TitleAsync()}'");

            logger?.LogException(exception, "Element existence check failed");
            throw exception;
        }
        if (ignoreIfNotFound && !await locator.IsVisibleAsync())
        {
            logger?.Debug($"Element exists but is hidden, ignoring: {locator}");
            return false;
        }
        return true;
    }

    private async Task<bool> WaitForElementVisible(
        ILocator locator,
        ElementInteractionOptions options,
        ElementAction action,
        LocatorType locatorType,
        string locatorValue)
    {
        try
        {
            await locator.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = options.ForceInteractionIfNotVisible ? 1000 : options.Timeout });
            return true;
        }
        catch (TimeoutException ex) when (options.ForceInteractionIfNotVisible && action == ElementAction.Click)
        {
            // visibility timed out; force click will be attempted by caller
            await ForceElementClick(locatorType, locatorValue);
            return false;
        }
        catch (Exception ex)
        {
            logger?.LogException(ex, "Failed to wait for element visibility for {0}:{1}", locatorType, locatorValue);
            throw;
        }
    }

    private async Task<bool> CheckElementEnabled(
        ILocator locator,
        ElementInteractionOptions options,
        ElementAction action,
        LocatorType locatorType,
        string locatorValue)
    {
        try
        {
            bool isEnabled = await locator.IsEnabledAsync();
            return isEnabled;
        }
        catch (Exception ex)
        {
            logger?.LogException(ex, "Failed to check element enabled state for {0}:{1}", locatorType, locatorValue);
            throw;
        }
    }

    private async Task<string> PerformElementAction(
        ILocator locator,
        ElementAction action,
        ElementInteractionOptions options,
        LocatorType locatorType,
        string locatorValue)
    {
        try
        {
            switch (action)
            {
                case ElementAction.Click:
                    string clickKind;
                    bool clickNeedsForce;
                    try
                    {
                        await locator.ClickAsync(new() { Timeout = options.Timeout });
                        clickKind = "Standard click";
                        clickNeedsForce = false;
                    }
                    catch (Exception ex)
                    {
                        logger?.Debug($"Standard click failed: {ex.Message}");
                        await Task.Delay(100);
                        await locator.ClickAsync(new() { Timeout = options.Timeout, Force = true });
                        clickKind = "Force click";
                        clickNeedsForce = true;
                    }

                    return await VerifyRadioClickCommitted(locator, locatorValue, options.Timeout, clickKind, clickNeedsForce);
                case ElementAction.Fill:
                case ElementAction.Type:
                    await FillOrType(locator, locatorValue, options, locatorType);
                    return $"Filled: {options.Value}";
                case ElementAction.Check:
                    await locator.CheckAsync(new() { Timeout = options.Timeout });
                    return "Checked";
                case ElementAction.Uncheck:
                    await locator.UncheckAsync(new() { Timeout = options.Timeout });
                    return "Unchecked";
                case ElementAction.Select:
                    if (!string.IsNullOrWhiteSpace(options.Value))
                    {
                        await SelectDropdown(locator, options.Value, $"{locatorValue}", options.Timeout);
                        return $"Selected: {options.Value}";
                    }
                    return string.Empty;
                case ElementAction.MultiSelect:
                    var multiValue = options.Values != null && options.Values.Count > 0
                        ? string.Join("|", options.Values)
                        : options.Value;

                    if (!string.IsNullOrWhiteSpace(multiValue))
                    {
                        await SelectDropdown(locator, multiValue, $"{locatorValue}", options.Timeout, isMultiOverride: true);
                        return $"Selected: {multiValue}";
                    }
                    return string.Empty;
                case ElementAction.SearchSelect:
                    if (!string.IsNullOrWhiteSpace(options.Value))
                    {
                        await SearchSelectDropdown(locator, options.Value, $"{locatorValue}", options.Timeout);
                        return $"Selected via search: {options.Value}";
                    }
                    return string.Empty;
                case ElementAction.Clear:
                    await locator.ClearAsync(new() { Timeout = options.Timeout });
                    return "Cleared";
                case ElementAction.Hover:
                    await locator.HoverAsync(new() { Timeout = options.Timeout });
                    return "Hovered";
                case ElementAction.DoubleClick:
                    await locator.DblClickAsync(new() { Timeout = options.Timeout });
                    return "Double-clicked";
                case ElementAction.Press:
                    if (!string.IsNullOrWhiteSpace(options.Value))
                    {
                        await locator.PressAsync(options.Value, new() { Timeout = options.Timeout });
                        return $"Pressed: {options.Value}";
                    }
                    return string.Empty;
                default:
                    var exception = new Exception($"Unsupported action: {action}");
                    logger?.LogException(exception, "Unsupported element action attempted");
                    throw exception;
            }
        }
        catch (Exception ex)
        {
            if (screenshotManager != null)
            {
                var context = $"{action}_{locatorType}_{locatorValue}";
                await screenshotManager.CaptureFailureScreenshotAsync(context, ex);
            }

            logger?.LogException(ex, "Failed to perform {0} action on {1}:{2}", action, locatorType, locatorValue);
            throw;
        }
    }

    private async Task FillOrType(ILocator locator, string locatorValue, ElementInteractionOptions options, LocatorType locatorType)
    {
        try
        {
            bool isAddressField = IsAddressField(locatorValue);

            if (isAddressField)
            {
                await SetAddress(locatorValue, options.Value ?? "");
            }
            else
            {
                await locator.FocusAsync();
                await Task.Delay(20);

                if (options.UseSequentialTyping)
                {
                    await locator.ClearAsync();
                    await locator.PressSequentiallyAsync(options.Value ?? "");
                }
                else
                {
                    await locator.FillAsync(options.Value ?? "");
                }

                await locator.BlurAsync();
                await Task.Delay(20);
            }

            if (options.PressEnter)
            {
                await locator.PressAsync("Enter", new() { Timeout = options.Timeout });
            }

            if (options.PressTab)
            {
                await locator.PressAsync("Tab", new() { Timeout = options.Timeout });
            }
        }
        catch (Exception ex)
        {
            logger?.LogException(ex, "Failed to fill/type in element {0}:{1}", locatorType, locatorValue);
            throw;
        }
    }

    private bool IsAddressField(string locatorValue)
    {
        var addressPatterns = new[]
        {
            "address",
            "AddressAutoComplete",
            "addressAutoComplete",
            "street",
            "location",
            "addr"
        };

        return addressPatterns.Any(pattern => locatorValue.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }

    private async Task ForceElementClick(LocatorType locatorType, string locatorValue)
    {
        logger?.Debug($"Attempting JavaScript force-click on {locatorType}:{locatorValue}");

        try
        {
            bool success;
            if (locatorType == LocatorType.XPath)
            {
                success = await page.EvaluateAsync<bool>(@"
                (xpath) => {
                    try {
                        const result = document.evaluate(xpath, document, null, XPathResult.FIRST_ORDERED_NODE_TYPE, null);
                        const element = result.singleNodeValue;
                        if (element) { 
                            element.click(); 
                            return true;
                        }
                        return false;
                    } catch (e) {
                        console.error('XPath error:', e);
                        return false;
                    }
                }
            ", locatorValue);
            }
            else
            {
                success = await page.EvaluateAsync<bool>(@"
                (selector) => {
                    try {
                        const element = document.querySelector(selector);
                        if (element) { 
                            element.click(); 
                            return true;
                        }
                        return false;
                    } catch (e) {
                        console.error('CSS selector error:', e);
                        return false;
                    }
                }
            ", locatorValue);
            }

            if (!success)
            {
                var exception = new Exception($"JavaScript force-click failed on {locatorType}:{locatorValue}");
                logger?.LogException(exception, "Force click operation failed");
                throw exception;
            }

            await page.WaitForTimeoutAsync(300);
        }
        catch (Exception ex)
        {
            logger?.LogException(ex, "Force click attempt failed for {0}:{1}", locatorType, locatorValue);
            throw;
        }
    }

    protected virtual async Task SelectDropdown(ILocator dropDown, string selectValue, string elementName, int timeout, bool isMultiOverride = false)
    {
        const int PRE_ACTION_DELAY = 200;
        const int POST_ACTION_DELAY = 500;

        try
        {
            if (await dropDown.CountAsync() == 0)
            {
                var exception = new Exception($"Dropdown element [{elementName}] not found.");
                logger?.LogException(exception, "Dropdown selection failed - element not found");
                throw exception;
            }

            string tagName = await dropDown.EvaluateAsync<string>("el => el.tagName.toLowerCase()");
            logger?.Debug($"Dropdown element type detected: {tagName} for {elementName}");

            if (isMultiOverride)
            {
                var trimmedValues = GetMultiSelectValues(selectValue, elementName);

                if (tagName == "select")
                    await HandleSelectDropdownMulti(dropDown, trimmedValues, timeout, PRE_ACTION_DELAY, POST_ACTION_DELAY);
                else if (tagName == "ng-select")
                    await HandleNgSelectDropdownMulti(dropDown, trimmedValues, elementName, timeout, PRE_ACTION_DELAY, POST_ACTION_DELAY);
                else
                {
                    var exception = new Exception($"Element [{elementName}] with tag '{tagName}' is not a supported dropdown type. Supported types: 'select', 'ng-select'.");
                    logger?.LogException(exception, "Unsupported dropdown type encountered");
                    throw exception;
                }

                return;
            }

            ValidateRequiredValue(selectValue, elementName, "select");

            string trimmedValue = selectValue.Trim();
            if (tagName == "select")
                await HandleSelectDropdown(dropDown, trimmedValue, timeout, PRE_ACTION_DELAY, POST_ACTION_DELAY);
            else if (tagName == "ng-select")
                await HandleNgSelectDropdown(dropDown, trimmedValue, elementName, timeout, PRE_ACTION_DELAY, POST_ACTION_DELAY);
            else
            {
                var exception = new Exception($"Element [{elementName}] with tag '{tagName}' is not a supported dropdown type. Supported types: 'select', 'ng-select'.");
                logger?.LogException(exception, "Unsupported dropdown type encountered");
                throw exception;
            }
        }
        catch (Exception ex)
        {
            logger?.LogException(ex, "Failed to select dropdown option '{0}' for {1}", selectValue, elementName);
            throw;
        }
    }

    private async Task HandleSelectDropdown(ILocator dropDown, string trimmedValue, int timeout, int preDelay, int postDelay)
    {
        await Task.Delay(preDelay);
        await dropDown.SelectOptionAsync(new[] { new SelectOptionValue { Label = trimmedValue } }, new() { Timeout = timeout });
        await Task.Delay(postDelay);
    }

    private async Task HandleSelectDropdownMulti(ILocator dropDown, IReadOnlyList<string> trimmedValues, int timeout, int preDelay, int postDelay)
    {
        await Task.Delay(preDelay);
        var selectOptions = trimmedValues.Select(value => new SelectOptionValue { Label = value }).ToArray();
        await dropDown.SelectOptionAsync(selectOptions, new() { Timeout = timeout });
        await Task.Delay(postDelay);
    }

    private async Task HandleNgSelectDropdown(ILocator dropDown, string trimmedValue, string elementName, int timeout, int preDelay, int postDelay, bool isMultiOverride = false)
    {
        await Task.Delay(preDelay);
        await OpenNgSelectPanel(dropDown, elementName, timeout);
        await Task.Delay(postDelay);

        // ng-dropdown-panel is rendered outside the ng-select subtree (portaled to body),
        // so we must query at the page level. Use :visible to avoid strict-mode violations
        // when stale/hidden panels from other ng-selects are still in the DOM.
        var dropdownPanel = page.Locator(".ng-dropdown-panel:visible");
        await dropdownPanel.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = timeout });

        await Task.Delay(preDelay);
        var optionLocator = await ResolveNgOption(dropDown, dropdownPanel, trimmedValue, elementName);

        await optionLocator.First.ClickAsync(new() { Timeout = timeout });
        await Task.Delay(postDelay);

        if (isMultiOverride)
        {
            await CloseNgSelectDropdown(dropDown, dropdownPanel, timeout);
            return;
        }

        await dropdownPanel.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden, Timeout = timeout });
    }

    private async Task HandleNgSelectDropdownMulti(ILocator dropDown, IReadOnlyList<string> trimmedValues, string elementName, int timeout, int preDelay, int postDelay)
    {
        await Task.Delay(preDelay);
        await OpenNgSelectPanel(dropDown, elementName, timeout);
        await Task.Delay(postDelay);

        // ng-dropdown-panel is portaled outside the ng-select subtree; query at page level
        // and filter to the visible panel to avoid strict-mode violations.
        var dropdownPanel = page.Locator(".ng-dropdown-panel:visible");
        await dropdownPanel.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = timeout });

        foreach (string trimmedValue in trimmedValues)
        {
            await Task.Delay(preDelay);
            var optionLocator = dropdownPanel.Locator(SelectableNgOption).Filter(new LocatorFilterOptions { HasText = trimmedValue });
            if (await optionLocator.CountAsync() == 0)
            {
                var exception = new Exception($"Could not find option [{trimmedValue}] in [{elementName}] dropdown.");
                logger?.LogException(exception, "Dropdown option not found");
                throw exception;
            }

            await optionLocator.First.ClickAsync(new() { Timeout = timeout });
            await Task.Delay(postDelay);
        }

        await CloseNgSelectDropdown(dropDown, dropdownPanel, timeout);
    }

    // A switcher / radio option is answered by clicking its wrapping <label>, because the input itself
    // is visually hidden (1x1 + clip-path) and loses Playwright's hit-target check to the sibling span -
    // measured: check() aimed at the input times out after 30s, while clicking the label works.
    // But ClickAsync reports success whether or not the option took, so a click that lands on a subtree
    // Angular has just re-rendered leaves the question unanswered and the run none the wiser. That is how
    // three required questions reached the end of CL_CommercialAutoCoverages unanswered on TC 237634,
    // with the only symptom a disabled Next button.
    //
    // Only radios are verified, and only where the expected post-condition is unambiguous. A Click on a
    // checkbox is a toggle, so "checked" is not its expected outcome; a Click on anything else - or on
    // something wrapping more than one radio - resolves nothing here and passes straight through.
    private async Task<string> VerifyRadioClickCommitted(
        ILocator locator, string elementName, int timeout, string clickKind, bool needsForce)
    {
        var radio = await ResolveRadioForClick(locator);
        if (radio == null)
            return clickKind;

        if (await IsCheckedWithin(radio, CommitSettleMs))
        {
            // Logged explicitly rather than left to the returned detail string: that only reaches the
            // structured Mongo payload, so a plain TRX could not tell "verified" from "never checked".
            logger?.Debug($"Clicked [{elementName}] and the option is selected");
            return $"{clickKind} (option selected)";
        }

        // Deliberately a warning and a retry rather than an immediate throw: the retry usually takes, and
        // the log line is the evidence that tells a re-render race apart from an answer being reverted.
        // The retry repeats the click that worked - a plain click after the first one already had to be
        // forced would just burn the whole timeout and surface a Playwright error instead of this one.
        logger?.Warning($"Clicked [{elementName}] but the option did not take - retrying once.");
        await locator.ClickAsync(new() { Timeout = timeout, Force = needsForce });

        if (await IsCheckedWithin(radio, CommitSettleMs))
            return $"{clickKind} (option selected on retry)";

        // As with the typeahead verifier, this throw fails the step only on a direct InteractWithField
        // call. The registry-default switchers it was written for are filled by FillForm, which swallows
        // it (IgnoreIfNotFound = true, plus ProcessField's catch) - there the LogException above is the
        // deliverable, naming the control that lost its answer before Next reports itself disabled.
        var exception = new PageElementException(
            $"[{elementName}]", "was clicked but the radio option is still not selected after a retry.");
        logger?.LogException(exception, "Radio option did not take");
        throw exception;
    }

    // The click is the gesture; the framework's model update is a beat behind it, and the failure this
    // guards against - Angular re-rendering the subtree - lands in that same beat. A single immediate
    // read is therefore both too early to confirm and too early to catch the defect, so poll instead.
    // Short budget: this runs on every radio click, and a control that has not committed within it has
    // not lost a race, it has lost the answer.
    private const int CommitSettleMs = 1000;

    private static async Task<bool> IsCheckedWithin(ILocator radio, int timeout)
    {
        try
        {
            await Assertions.Expect(radio).ToBeCheckedAsync(new() { Timeout = timeout });
            return true;
        }
        catch (PlaywrightException)
        {
            return false;
        }
    }

    // The clicked element is normally the wrapping <label>; a few registry entries point at the inner
    // <span> instead, so fall back to the nearest ancestor label before concluding this was not a radio.
    //
    // Bails out when the subtree holds more than one radio. The clicked element is single (Playwright's
    // strict mode saw to that), but it can still wrap a whole yes/no pair - and there is no way to tell
    // from here which one the click was aiming at. Guessing .First would fail the "No" answers of every
    // such field, which is the shape of the Button-typed HQX/D2C yes-no entries no suite covers.
    private static async Task<ILocator?> ResolveRadioForClick(ILocator locator)
    {
        var radios = locator.Locator("input[type='radio']");
        var count = await radios.CountAsync();
        if (count == 1)
            return radios.First;
        if (count > 1)
            return null;

        radios = locator.Locator("xpath=ancestor::label[1]").Locator("input[type='radio']");
        return await radios.CountAsync() == 1 ? radios.First : null;
    }

    // ng-select renders non-selectable rows ("No items found", "Loading…") as .ng-option
    // too, so every option lookup has to exclude the disabled ones.
    private const string SelectableNgOption = ".ng-option:not(.ng-option-disabled)";

    // A remote-search ng-select-typeahead (FieldType.SearchDropdown): the option list is
    // populated server-side per keystroke rather than filtered from a preloaded list, so -
    // unlike SelectDropdown/ResolveNgOption - there's nothing to text-match against. Set the
    // search term and take whatever the server returns first. Unlike ResolveNgOption's
    // keystroke-by-keystroke search (needed there to filter an already-open, already-loaded
    // list), this widget's remote search fires off the single `input` event FillAsync raises -
    // confirmed against Unify Staging (TC 253185) - so there's no need to simulate real typing.
    private async Task SearchSelectDropdown(ILocator locator, string searchValue, string elementName, int timeout)
    {
        ValidateRequiredValue(searchValue, elementName, "search-select");

        string tagName = await locator.EvaluateAsync<string>("el => el.tagName.toLowerCase()");
        var input = tagName == "input" ? locator : locator.Locator("input[role='combobox']").First;

        await input.ClickAsync(new() { Timeout = timeout });
        await input.FillAsync(searchValue.Trim(), new() { Timeout = timeout });

        var dropdownPanel = page.Locator(".ng-dropdown-panel:visible");
        await dropdownPanel.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = timeout });

        var option = dropdownPanel.Locator(SelectableNgOption).First;
        await option.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = timeout });
        var optionText = (await option.InnerTextAsync()).Trim();
        await option.ClickAsync(new() { Timeout = timeout });

        await dropdownPanel.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden, Timeout = timeout });

        // Taking the first option on trust is the contract above, but committing nothing at all is not:
        // a panel that closes without the control taking a value leaves the field empty.
        //
        // Know what the throw below does and does not buy you. On a direct InteractWithField call it
        // fails the step. On the FillForm path it does NOT: MergeOptionsWithDefaults hardcodes
        // IgnoreIfNotFound = true, so InteractWithElement logs and does not rethrow, and ProcessField
        // catches whatever escapes. There the value is the logged evidence - the run still reaches the
        // page's own signals (the RetryInvalidFieldsAsync pass, then ClickContinueButton naming the
        // unsatisfied fields), but it reaches them with a log line saying which control came back empty
        // instead of leaving the cause to be guessed from a disabled Next button.
        await VerifyNgSelectCommitted(locator, tagName, elementName, searchValue, optionText);
    }

    // ng-select marks a control that holds a value with .ng-has-value on its container and renders the
    // label in .ng-value-label. Only treat the control as empty when the container is actually there and
    // says so - if the widget isn't shaped the way we expect, say so and move on rather than failing a
    // selection that may well have landed.
    private async Task VerifyNgSelectCommitted(
        ILocator locator, string tagName, string elementName, string searchValue, string optionText)
    {
        var control = tagName == "input" ? locator.Locator("xpath=ancestor::ng-select[1]") : locator;
        var container = control.Locator(".ng-select-container").First;
        if (await container.CountAsync() == 0)
        {
            logger?.Debug($"Selected [{elementName}] via typeahead search '{searchValue}' -> '{optionText}' (no ng-select container to confirm against)");
            return;
        }

        // Same beat problem as the radio verification: ng-select stamps .ng-has-value on the container
        // through Angular's change detection, which has not necessarily run by the time the panel hides.
        // Give it the settle budget before calling the control empty.
        var hasValue = await HasNgValueWithin(container, CommitSettleMs);

        var committed = string.Empty;
        var valueLabel = control.Locator(".ng-value-label").First;
        if (await valueLabel.CountAsync() > 0)
            committed = (await valueLabel.InnerTextAsync()).Trim();

        if (!hasValue && string.IsNullOrWhiteSpace(committed))
        {
            var exception = new PageElementException(
                $"[{elementName}]",
                $"typeahead search for '{searchValue}' clicked option '{optionText}' but the control committed no value.");
            logger?.LogException(exception, "Typeahead selection did not commit");
            throw exception;
        }

        logger?.Debug($"Selected [{elementName}] via typeahead search '{searchValue}' -> committed '{(committed.Length == 0 ? optionText : committed)}'");
    }

    private static async Task<bool> HasNgValueWithin(ILocator container, int timeout)
    {
        try
        {
            await Assertions.Expect(container).ToHaveClassAsync(new Regex(@"\bng-has-value\b"), new() { Timeout = timeout });
            return true;
        }
        catch (PlaywrightException)
        {
            return false;
        }
    }

    // Some pages float an <app-form-label> over the ng-select; it intercepts the centre
    // click Playwright uses to open the panel, and the click silently never lands. The
    // .ng-arrow-wrapper on the right edge sits outside that overlay, so retry there.
    private async Task OpenNgSelectPanel(ILocator dropDown, string elementName, int timeout)
    {
        try
        {
            // Kept short: an intercepted click is the expected failure here, and the
            // arrow-wrapper retry below is just as valid, so don't burn the full budget.
            await dropDown.ClickAsync(new() { Timeout = Math.Min(timeout, 1500) });
            return;
        }
        catch (Exception ex) when (ex is PlaywrightException or System.TimeoutException)
        {
            logger?.Debug($"Click on [{elementName}] did not open the panel ({ex.Message}); retrying via .ng-arrow-wrapper");
        }

        var arrow = dropDown.Locator(".ng-arrow-wrapper").First;
        if (await arrow.CountAsync() == 0)
        {
            var exception = new Exception($"Could not open dropdown [{elementName}]: the click was intercepted and the element has no .ng-arrow-wrapper to fall back to.");
            logger?.LogException(exception, "Dropdown could not be opened");
            throw exception;
        }

        await arrow.ClickAsync(new() { Timeout = timeout });
    }

    // A searchable ng-select only renders the options matching its current search term, so
    // an option missing from the open panel is not proof it doesn't exist — type the value
    // in and look again before declaring it absent.
    private async Task<ILocator> ResolveNgOption(ILocator dropDown, ILocator dropdownPanel, string trimmedValue, string elementName)
    {
        var optionLocator = dropdownPanel.Locator(SelectableNgOption).Filter(new LocatorFilterOptions { HasText = trimmedValue });
        if (await optionLocator.CountAsync() > 0)
            return optionLocator;

        var search = dropDown.Locator("input[role='combobox']").First;
        if (await search.CountAsync() > 0)
        {
            try
            {
                await search.PressSequentiallyAsync(trimmedValue, new LocatorPressSequentiallyOptions { Delay = 50 });

                optionLocator = dropdownPanel.Locator(SelectableNgOption).Filter(new LocatorFilterOptions { HasText = trimmedValue });
                if (await optionLocator.CountAsync() > 0)
                    return optionLocator;
            }
            catch (Exception ex) when (ex is PlaywrightException or System.TimeoutException)
            {
                logger?.Debug($"Could not search [{elementName}] for '{trimmedValue}': {ex.Message}");
            }
        }

        var notFound = new Exception($"Could not find option [{trimmedValue}] in [{elementName}] dropdown.");
        logger?.LogException(notFound, "Dropdown option not found");
        throw notFound;
    }

    private async Task CloseNgSelectDropdown(ILocator dropDown, ILocator dropdownPanel, int timeout)
    {
        if (!await dropdownPanel.IsVisibleAsync())
            return;

        try
        {
            await page.Keyboard.PressAsync("Escape");
        }
        catch
        {
            // ignore
        }

        if (await dropdownPanel.IsVisibleAsync())
        {
            try
            {
                await dropDown.ClickAsync(new() { Timeout = timeout });
            }
            catch
            {
                // ignore
            }
        }

        await dropdownPanel.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden, Timeout = timeout });
    }

    private static IReadOnlyList<string> GetMultiSelectValues(string selectValue, string elementName)
    {
        if (string.IsNullOrWhiteSpace(selectValue))
            throw new ArgumentException($"Cannot select empty value for [{elementName}]");

        string[] values = selectValue
            .Split(new[] { ",", "|" }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .ToArray();

        if (values.Length == 0)
            throw new ArgumentException($"Cannot select empty value for [{elementName}]");

        return values;
    }

    private static bool IsAjaxTriggerAction(ElementAction action) => action switch
    {
        ElementAction.Select => true,
        ElementAction.MultiSelect => true,
        ElementAction.Check => true,
        ElementAction.Uncheck => true,
        _ => false
    };

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
            LocatorType.Name => page.Locator($"[name='{locatorValue}']"),
            _ => throw new ArgumentException($"Unsupported locator type: {locatorType}")
        };
    }

    private void ValidateRequiredValue(string value, string fieldName, string actionName = "use")
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            var exception = new ArgumentException($"Cannot {actionName} empty value for [{fieldName}]");
            logger?.LogException(exception, "Value validation failed for field {0}", fieldName);
            throw exception;
        }
    }

    private Exception WrapPlaywrightException(Exception ex, ElementAction action, LocatorType locatorType, string locatorValue)
    {
        if (ex is not (TimeoutException or PlaywrightException))
            return ex;

        var actionName = action.ToString().ToLowerInvariant();

        if (ex is TimeoutException)
        {
            var duration = ExtractTimeoutMs(ex.Message) is > 0 and var ms ? $"{ms}ms" : "the configured timeout";
            var waitState = ExtractWaitState(ex.Message);
            return new Exception(
                $"Timed out trying to {actionName} element — it was not {waitState} within {duration}. " +
                $"Verify the element exists and is visible on the page. " +
                $"Locator: {locatorType} = '{locatorValue}' | Page: {page.Url}",
                ex);
        }

        var playwrightReason = ExtractPlaywrightReason(ex.Message);
        return new Exception(
            $"Could not {actionName} element — {playwrightReason} " +
            $"Locator: {locatorType} = '{locatorValue}' | Page: {page.Url}",
            ex);
    }

    private static int ExtractTimeoutMs(string message)
    {
        const string prefix = "Timeout ";
        const string suffix = "ms exceeded";
        var start = message.IndexOf(prefix, StringComparison.Ordinal);
        if (start < 0) return 0;
        start += prefix.Length;
        var end = message.IndexOf(suffix, start, StringComparison.Ordinal);
        return end > start && int.TryParse(message[start..end], out var ms) ? ms : 0;
    }

    private static string ExtractWaitState(string message)
    {
        if (message.Contains("to be visible")) return "visible";
        if (message.Contains("to be enabled")) return "enabled";
        if (message.Contains("to be attached")) return "present in the DOM";
        if (message.Contains("to be hidden")) return "hidden";
        return "ready";
    }

    private static string ExtractPlaywrightReason(string message)
    {
        var firstLine = message
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault(l => !string.IsNullOrWhiteSpace(l))
            ?.Trim() ?? message;
        return firstLine.EndsWith('.') ? firstLine : firstLine + ".";
    }

    public async Task SetAddress(string locatorValue, string inputValue)
    {
        // Timeout for suggestion dropdown to appear. UAT can be slower than QA,
        // so we use a generous value instead of a fixed sleep.
        const int SuggestionWaitTimeoutMs = 15000;

        try
        {
            var address = page.Locator(locatorValue);
            await address.ClearAsync();
            await address.FillAsync(inputValue);

            // Wait for autocomplete suggestions to be visible before using keyboard navigation.
            // Covers Google Places (.pac-item) and Angular Bootstrap ngbTypeahead (ngb-typeahead-window).
            var suggestions = page.Locator(".pac-item, ngb-typeahead-window").First;
            try
            {
                await suggestions.WaitForAsync(new LocatorWaitForOptions
                {
                    State = WaitForSelectorState.Visible,
                    Timeout = SuggestionWaitTimeoutMs
                });
                logger?.Debug("Address suggestions visible — selecting first result via keyboard");
            }
            catch (TimeoutException)
            {
                logger?.Debug(
                    "Address suggestions not detected within {0}ms; proceeding with keyboard navigation anyway",
                    SuggestionWaitTimeoutMs);
            }

            await page.Keyboard.PressAsync("ArrowDown");
            await page.Keyboard.PressAsync("Enter");
            // Brief pause to let the selected address commit to the input value.
            await Task.Delay(500);
        }
        catch (Exception ex)
        {
            logger?.LogException(ex, "Failed to set address using simple method for {0}", locatorValue);
            throw;
        }
    }
}
