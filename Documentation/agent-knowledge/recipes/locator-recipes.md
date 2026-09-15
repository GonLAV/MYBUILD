---
topic: recipe:locator-recipes
summary: Pattern library for locator strategies — input vs ng-select, label-vs-control, XPath OR for tenant variants.
status: ready
---

# Locator recipes (per `UIFieldType`)

> **When to read:** Phase 3 (write the registry entry), Phase 4 (locator failures — match symptom to recipe).
>
> **Companion files:** [../framework/ui-field-types.md](../framework/ui-field-types.md) (taxonomy + ElementInteractionOptions), [../framework/field-registry.md](../framework/field-registry.md) (registry mechanism), [override-patterns.md](override-patterns.md) (when a page-object override is the right answer instead).

## Recipes per control type

### 1. Plain text input

```csharp
[FirstName] = new UIElement {
    Strategy = LocatorType.XPath,
    Locators = "//*[contains(@class,'PolicyData.FirstName')]//input",
    FieldType = UIFieldType.Input,
    DefaultValue = "AutoTest",
    Pages = [typeof(Product_StartPage)],
},
```

For a form-control input that needs a specific class:

```csharp
[DateOfBirth] = new UIElement {
    Strategy = LocatorType.XPath,
    Locators = "//*[contains(@class,'PolicyData.DateOfBirth')]//input[contains(@class,'form-control')]",
    FieldType = UIFieldType.DatePicker,  // DatePicker still resolves to Fill
    DefaultValue = "01/01/1990",
    Pages = [typeof(Product_StartPage), typeof(Product_ApplicantPage)],
},
```

For a text input where **value commit requires Tab/blur** (Angular validators):

```csharp
InteractionOptions = new ElementInteractionOptions {
    PressTab = true,    // default — keep it
    Timeout = 10000,
}
```

### 2. ng-select dropdown (Angular)

The wrapping element exposes a `<ng-select>` host. Click it to open, then click the matching option in `ng-dropdown-panel`. The framework handles this when `FieldType = Dropdown` and `Strategy = XPath` points to the `<ng-select>` host:

```csharp
[LegalEntity] = new UIElement {
    Strategy = LocatorType.XPath,
    Locators = "//*[contains(@class,'PolicyData.LegalEntity')]//ng-select",
    FieldType = UIFieldType.Dropdown,
    DefaultValue = "Corporation",
    Pages = [typeof(Product_BusinessPage)],
},
```

**Trap:** if you point at `//div[contains(@class,'ng-value-container')]` instead of `//ng-select`, the framework's dropdown handler sees a `<div>` and **throws**:

```
Element [...//div[contains(@class,'ng-value-container')]] with tag 'div'
is not a supported dropdown type. Supported types: 'select', 'ng-select'.
```

This error is searchable; if you grep it in a failing run log, the fix is always the same — change the locator's end-anchor from the inner `div` to the host `//ng-select`. TC 240782 hit this on `CL_BIPD`, `CL_MedPay`, `CurrentBopCarrier`, `CL_TransportationExpense` — every "Dropdown" registry entry must terminate on the `ng-select` element, never on a descendant `div`. See [troubleshooting/div-not-supported-dropdown.md](troubleshooting/div-not-supported-dropdown.md).

### 3. Styled dropdown with `{0}` option matching

Some entries use `{0}` substitution to scope to a specific option, useful when the registry handler expects a click on the option directly:

```csharp
[ArchitectureStyle] = new UIElement {
    Strategy = LocatorType.XPath,
    Locators = "//*[contains(@class,'PolicyData.ArchitectureStyle')]//div[contains(@class,'ng-value-container')]",
    FieldType = UIFieldType.Dropdown,
    DefaultValue = "Colonial",
    Pages = [typeof(Product_StructurePage)],
},
```

When `{0}` is in the locator, `UIElement.GetLocators(value)` substitutes the user value (case-preserving if `PreserveCasing = true`).

### 4. SearchDropdown / typeahead

Custom typeahead is rare in the registry — usually handled by a page-method override (e.g. `Product_StartPage.FillIndustryTypeahead`):

```csharp
public async Task FillIndustryTypeahead(string searchText) {
    var controlContainer = Page.Locator("//*[contains(@class,'PolicyData.EposNaicDescription')]");
    var input = controlContainer.Locator("input[role='combobox']");
    await input.ClickAsync();
    await input.TypeAsync(searchText, new LocatorTypeOptions { Delay = 80 });
    var firstOption = controlContainer.Locator("div[role='option']").First;
    await firstOption.WaitForAsync(new LocatorWaitForOptions {
        State = WaitForSelectorState.Visible, Timeout = 15000
    });
    await firstOption.ClickAsync();
}
```

The registry entry then either targets the input directly (so MergeDataManager can attempt `Type`), or — preferred — un-tags from `Pages` and is driven from the override. The pattern is: handle typeahead in the page object; keep the registry simple.

### 5. Radio (Yes/No, multiple choice)

Two flavors depending on the rendered HTML:

**(a) Real `<input type="radio">`** — use `FieldType = Radio` with `Check`:

```csharp
[HomeUnderConstruction] = new UIElement {
    Strategy = LocatorType.XPath,
    Locators = "//*[contains(@class,'PolicyData.HomeUnderConstruction')]//input[@type='radio' and @value='{0}']",
    FieldType = UIFieldType.Radio,
    DefaultValue = "No",
    Pages = [typeof(Product_StructurePage)],
    PreserveCasing = true,
},
```

**(b) Styled radio with hidden input + label overlay** — use `FieldType = Button` clicking the label:

```csharp
[SlabType] = new UIElement {
    Strategy = LocatorType.CSS,
    Locators = "[name='PolicyData.SlabType'] label:has-text('{0}')",
    FieldType = UIFieldType.Button,
    DefaultValue = "Concrete",
    Pages = [typeof(Product_StructurePage)],
},
```

### 6. Checkbox — real `<input type="checkbox">`

```csharp
[ChargingStation] = new UIElement {
    Strategy = LocatorType.XPath,
    Locators = "//*[contains(@class,'PolicyData.ChargingStation')]//input[@type='checkbox']",
    FieldType = UIFieldType.Checkbox,
    DefaultValue = "false",
    Pages = [typeof(Product_FeaturesPage)],
},
```

Value "true"/"false" → `Check` / `Uncheck`.

### 7. Checkbox-wrapper (KLX-style label-as-toggle)

When the page renders the input hidden and uses a `<label class="checkbox-wrapper">` for clicks, treat as Button:

```csharp
[Certification] = new UIElement {
    Strategy = LocatorType.XPath,
    Locators = "//*[contains(@class,'PolicyData.CLCertifyFarmersDeclined')]//label[contains(@class,'checkbox-wrapper')]",
    FieldType = UIFieldType.Button,
    DefaultValue = "true",
    Pages = [typeof(Product_StartPage)],
},
```

`Certification` is a normal page-tagged entry. A dependent field whose appearance is gated by this click — like `DeclinationReason` — uses `DependsOn = Certification, DependsOnValue = "true"` so the registry orders the two and waits between them. See [override-patterns.md](override-patterns.md) "Override pattern A — conditional reveal via DependsOn". `Pages = []` is the older "drive everything out-of-band from the page object" pattern; only fall back to it if `DependsOn` can't model the gating.

### 8. Switcher Yes/No (toggle wrapped in a label)

The DOM is typically:

```html
<label class="switcher-wrapper">
  <input type="checkbox" hidden>
  <span class="switcher-button"></span>
  <span>Yes</span>
</label>
<label class="switcher-wrapper">
  <input type="checkbox" hidden>
  <span class="switcher-button"></span>
  <span>No</span>
</label>
```

Use `FieldType = Button` with `{0}` substitution to pick the matching label:

```csharp
[OwnerInvolvedInDailyOperations] = new UIElement {
    Strategy = LocatorType.XPath,
    Locators =
        "//*[contains(@class,'PolicyData.OwnerInvolvedDailyOperations')]" +
        "//label[contains(@class,'switcher-wrapper') and .//*[contains(text(),'{0}')]]",
    FieldType = UIFieldType.Button,
    DefaultValue = "Yes",
    Pages = [typeof(Product_BusinessPage)],
    PreserveCasing = true,
},
```

### 9. Date picker

Two flavors:

**(a) Plain input formatted MM/DD/YYYY** — use `Input` or `DatePicker` (both resolve to `Fill`):

```csharp
[EffectiveDate] = new UIElement {
    Strategy = LocatorType.XPath,
    Locators = "//*[contains(@class,'PolicyData.EffectiveDate')]//input[contains(@class,'form-control')]",
    FieldType = UIFieldType.DatePicker,
    DefaultValue = "01/01/2030",
    Pages = [typeof(Product_PolicyPage)],
},
```

**(b) Mat-datepicker with overlay calendar** — bypass the calendar, target the visible input directly. If the visible input is hidden, use a different visible input or open the placeholder element. See `Product_PolicyPage.FillEffectiveDate` for a worked example. For the hidden-input case, see [override-patterns.md](override-patterns.md) pattern E.

### 10. Button-as-list-item (LOB selection)

```csharp
[Lob] = new UIElement {
    Strategy = LocatorType.XPath,
    Locators = "//*[contains(@class,'PolicyData.Lobs[]')]//li[contains(.,'{0}')]",
    FieldType = UIFieldType.Button,
    DefaultValue = "Homeowners",
    Pages = [typeof(Product_StartPage)],
},
```

Often combined with a page-method override (`Product_StartPage.SelectLob`) that does additional checkbox-state checking before clicking.

### 11. Tenant-variable class scheme (KLX collapsing)

KLX collapses `PolicyData.Vehicles[?].Year` into `PolicyDataVehicles…YearVehicleDropdownWithSearch…`. Match both schemes with XPath OR:

```csharp
[PLYear] = new UIElement {
    Strategy = LocatorType.XPath,
    Locators =
        "//*[contains(@class,'PolicyData.PLYear')]//ng-select | " +
        "//*[contains(@class,'PolicyDataVehicles') and contains(@class,'Year') and not(contains(@class,'IsDifferent'))]//ng-select",
    FieldType = UIFieldType.Dropdown,
    DefaultValue = null,
    Pages = [typeof(Product_VehiclePage)],
},
```

Always include a `not(contains(@class,'SiblingThatStartsWithSamePrefix'))` clause if the page has fields like `Year`, `YearOfDriving`, `YearOfManufacture` that share the prefix.

### 12. Multi-page-tagged shared field (different class on each page)

Sometimes the same field name maps to different selectors on different pages (e.g. `AnnualPayroll` on the WC PolicyPage uses `PolicyData.AnnualPayroll`; on the KLX BusinessPage it's `PolicyData.AnnualOwnerPayroll`). Solve with XPath OR and tag both pages:

```csharp
[AnnualPayroll] = new UIElement {
    Strategy = LocatorType.XPath,
    Locators =
        "//*[contains(@class,'PolicyData.AnnualPayroll')]//input | " +
        "//*[contains(@class,'PolicyData.AnnualOwnerPayroll')]//input",
    FieldType = UIFieldType.Input,
    DefaultValue = "0",
    Pages = [typeof(Product_PolicyPage), typeof(Product_BusinessPage)],
},
```

If the value type also differs by page, split into two separate field names and tag each for its page. Don't try to make one entry handle two semantically different fields.

### 13. Yes/No radio with `_true`/`_false` ID suffix (KLX per-vehicle coverages)

KLX CL Vehicle Coverage controls (Full Glass, Road Side Assistance) render as a pair of `<label class="switcher-wrapper">` elements whose inner inputs have IDs ending in `_true` and `_false`:

```
id="PolicyDataVehiclesid<HASH>CLFullGlassYesNoFullGlasstrue_<W>_<H>_true"
id="PolicyDataVehiclesid<HASH>CLFullGlassYesNoFullGlasstrue_<W>_<H>_false"
```

Mechanically this is just the Switcher Yes/No pattern (Recipe 8). The class fragment to match on is the bare CL-prefixed leaf name (`CL_FullGlass`, `CL_RoadSide`) — `contains(@class, …)` is substring-based, so the `PolicyData.Vehicles[?(...)]` prefix doesn't need to be encoded:

```csharp
[CL_FullGlass] = new UIElement {
    Strategy = LocatorType.XPath,
    Locators =
        "//*[contains(@class,'CL_FullGlass')]" +
        "//label[contains(@class,'switcher-wrapper') and .//span[contains(normalize-space(.),'{0}')]]",
    FieldType = UIFieldType.Button,
    DefaultValue = "No",
    Pages = [typeof(Product_CLPolicyPage)],
    PreserveCasing = true,
    Required = true
},
```

See [../domain/partners/kraftlakex.md](../domain/partners/kraftlakex.md) "CL Policy per-vehicle coverage classes" for the complete CL_-prefixed name inventory.

### 14. Mat-datepicker hidden-input — DOM-event workaround

KLX CL Policy `EffectiveDate` and `Operator DOB` are rendered as Angular Material datepickers. The visible `<input placeholder='MM/DD/YYYY'>` is a façade; the underlying `matinput` is `display: none`, and Playwright's `FillAsync` rejects it with:

```
element is not visible, enabled, or editable
```

The fix lives in a page-object override (the registry can't model "set a hidden value via JS"). Pull the field out of `formData`, run `base.FillForm` for the rest, then set the value with a JS `EvaluateAsync` that dispatches the events Angular's two-way binding listens for:

```csharp
private const string EffectiveDateInputLocator =
    "//*[contains(@class,'PolicyData.EffectiveDate')]//input[@placeholder='MM/DD/YYYY']";

private async Task SetEffectiveDateAsync(string value) {
    var input = Page.Locator($"xpath={EffectiveDateInputLocator}").First;
    if (await input.CountAsync() == 0) return;
    var currentValue = await input.InputValueAsync();
    if (!string.IsNullOrEmpty(currentValue)) return;       // idempotent guard
    await input.EvaluateAsync(@"(el, v) => {
        el.value = v;
        el.dispatchEvent(new Event('input',  { bubbles: true }));
        el.dispatchEvent(new Event('change', { bubbles: true }));
        el.dispatchEvent(new Event('blur',   { bubbles: true }));
    }", value);
}
```

Same shape is used in `Product_OperatorPage.SetKlxOperatorDobAsync`. The registry entry stays a normal `Input` / `DatePicker`; the override only kicks in for the affected page-tenant combo.

See [override-patterns.md](override-patterns.md) "Override pattern E — mat-datepicker DOM events".

### 15. Floated-label ng-select — arrow-wrapper workaround

KLX Vehicle "How many miles…" (`AnnualMileage` / Radius) and **every** CL Policy ng-select have an `<app-form-label>` overlay that intercepts pointer events on the center of the control. Symptoms:

- Framework `Select` reports success but the UI still shows "Please select".
- Playwright log: `<app-form-label …> intercepts pointer events`.
- The container has `ng-select-disabled` class transiently at click time.

Workaround: open the panel by clicking the `.ng-arrow-wrapper` (right edge of the ng-select — outside the label overlay), then pick the option from the panel. Encapsulated in a single page-level helper that runs after `base.FillForm`:

```csharp
private async Task SelectViaArrowAsync(string classFragment, string optionText, bool firstIfMissing) {
    var container = Page.Locator($"xpath=//*[contains(@class,'{classFragment}')]").First;
    await container.WaitForAsync(new LocatorWaitForOptions {
        State = WaitForSelectorState.Attached, Timeout = 3000 });

    var arrow = container.Locator(".ng-arrow-wrapper").First;
    await arrow.ClickAsync(new LocatorClickOptions { Timeout = 5000 });

    var panel = Page.Locator(".ng-dropdown-panel");
    await panel.WaitForAsync(new LocatorWaitForOptions {
        State = WaitForSelectorState.Visible, Timeout = 5000 });

    var option = panel.Locator(".ng-option", new() { HasText = optionText }).First;
    if (await option.CountAsync() == 0 && firstIfMissing)
        option = panel.Locator(".ng-option").First;
    await option.ClickAsync();
    await panel.WaitForAsync(new LocatorWaitForOptions {
        State = WaitForSelectorState.Hidden, Timeout = 5000 });
}
```

The page-object's `FillForm` keeps a list of `(FieldName, ClassFragment, FirstIfMissing)` tuples and iterates the helper once per intercepted field. See [override-patterns.md](override-patterns.md) "Override pattern F — floated-label arrow-wrapper" for the full list-driven shape.
