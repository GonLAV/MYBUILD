---
topic: recipe:override-patterns
summary: Page-object override patterns A-G — conditional reveal, click-overlay, JS-fill, arrow-wrapper, first-if-missing.
status: ready
---

# Page-object override patterns (A–G)

> **When to read:** Phase 3 (extending a page), Phase 4 (override decisions for out-of-band fields).
>
> **Companion files:** [../framework/page-objects.md](../framework/page-objects.md) (mechanism + when-to-override criteria), [locator-recipes.md](locator-recipes.md) (per-field-type registry patterns), [../framework/field-registry.md](../framework/field-registry.md) (DependsOn semantics).

## Override pattern A — conditional reveal via `DependsOn` (no override needed)

The KLX `Certification` checkbox **reveals** the `DeclinationReason` ng-select after a few seconds of Angular hydration. Earlier iterations of this skill drove both fields manually in `Product_StartPage.FillForm` with `Pages = []` on each registry entry. That's no longer necessary: the field registry's `DependsOn` mechanism already covers ordering plus a wait.

The two entries on the cleaned `wip/klx-cl-auto-tc240782` branch look like:

```csharp
[Certification] = new UIElement {
    Strategy = LocatorType.XPath,
    Locators = "//*[contains(@class,'PolicyData.CLCertifyFarmersDeclined')]//label[contains(@class,'checkbox-wrapper')]",
    FieldType = UIFieldType.Button,
    DefaultValue = "true",
    Pages = [typeof(Product_StartPage)],
},

[DeclinationReason] = new UIElement {
    Strategy = LocatorType.XPath,
    Locators = "//*[contains(@class,'PolicyData.CL_IneligibleReason')]//ng-select",
    FieldType = UIFieldType.Dropdown,
    DefaultValue = "Class/SIC code not available with Farmers",
    Pages = [typeof(Product_StartPage)],
    PreserveCasing = true,
    DependsOn = Certification,
    DependsOnValue = "true",
    InteractionOptions = new ElementInteractionOptions { Timeout = 15000 }
},
```

How this works:

- `FormDataHelper.GetOrderedFields` topologically sorts so `Certification` fills first; `DeclinationReason` runs after.
- `ProcessField.ShouldFillField` skips `DeclinationReason` if `Certification`'s recorded value isn't `"true"`.
- `InteractionOptions.Timeout = 15000` absorbs KLX Staging's hydration delay (3s flakes, 15s reliable).

No `FillForm` override is needed for this scenario. `Product_StartPage` may still override `FillForm` to handle the **industry typeahead** (the typeahead is too async to model in the registry — see [locator-recipes.md](locator-recipes.md) recipe 4), but `Certification` / `DeclinationReason` ride through `base.FillForm`.

When to fall back to the older "pull out of `formData`, drive after `base.FillForm`" pattern: only when `DependsOn` plus `InteractionOptions.Timeout` aren't expressive enough — e.g. when the dependent field needs custom retry logic, or when the trigger involves a popup rather than a value change.

## Override pattern B — VIN decode handshake

`Product_VehiclePage.FillForm` types the VIN, clicks Submit, waits for the decoded Year ng-select to attach, **then** suppresses the decoded fields from the iteration so cascading dropdowns don't fight:

```csharp
public override async Task FillForm(Dictionary<string,string>? formData = null) {
    string? annualMileageValue = null;     // also pulled out — handled by arrow-wrapper (pattern F)

    if (formData != null && formData.TryGetValue(VIN, out var vinValue)
        && !string.IsNullOrEmpty(vinValue))
    {
        await FillVinAndDecodeAsync(vinValue);

        // Decode populated and locked Year/Make/Model/BodyStyle. Suppress so
        // base.FillForm doesn't try to re-select on disabled ng-selects.
        var filtered = new Dictionary<string,string>(formData) {
            [VIN] = "",
            [PLYear] = "",
            [PLMake] = "",
            [PLModel] = "",
            [BodyStyle] = ""
        };
        formData = filtered;
    }

    await base.FillForm(formData);
}

private async Task FillVinAndDecodeAsync(string vinValue) {
    _logger?.Info($"Typing VIN '{vinValue}' and clicking Submit to decode");

    var input = Page.Locator($"xpath={VinInputLocator}");
    await input.FillAsync(vinValue);
    await input.PressAsync("Tab");                   // commit through Angular blur/validators

    var submit = Page.Locator($"xpath={VinSubmitButtonLocator}");
    await submit.ClickAsync();

    try {
        await Page.Locator($"xpath={YearDecodedLocator}").First.WaitForAsync(
            new LocatorWaitForOptions { State = WaitForSelectorState.Attached });
    } catch (TimeoutException) {
        _logger?.Warning("VIN decode signal not detected; continuing.");
    }

    _logger?.Info("VIN decode complete");
}
```

**Lessons:**
- `PressAsync("Tab")` after `FillAsync` commits through Angular validators; without it the Submit button can stay disabled.
- The cleaned `YearDecodedLocator` is `//*[contains(@class,'PolicyDataVehicles') and contains(@class,'YearVehicleDropdownWithSearch')]//ng-select[contains(@class,'ng-select-disabled')]` — the framework Decode marks the Year ng-select as disabled once populated, which is the signal we wait for.
- Catch `TimeoutException` on the decode signal and log-and-continue. The downstream Year/Make/Model selects fail loudly enough if decode silently didn't complete. See [troubleshooting/vin-decode-flake.md](troubleshooting/vin-decode-flake.md).
- Earlier iterations of this skill used `WaitForFunctionAsync` to poll the Submit button's disabled attribute before clicking. The cleaner approach is to PressAsync("Tab") to commit validators and then click directly — Submit is enabled by the time the click queues.

## Override pattern C — custom Continue button (historical)

Earlier iterations of this skill recommended a custom `ClickContinue` override on `Product_MarketsPage` to absorb the market-lookup wait with a 30s/60s timeout. The cleaned `wip/klx-cl-auto-tc240782` branch no longer carries that override — once `button.Get.quotes` was added to `InterviewBase.ContinueButton`'s selector list, the standard click path on Markets proved reliable enough on Staging. The current `Product_MarketsPage` is bare:

```csharp
public class Product_MarketsPage : InterviewBase
{
    public Product_MarketsPage(IBrowserManager b, IPageHelper p, IScopeContext s,
        bool validatePageReady = true, IAutomationLogger? l = null)
        : base(b, p, s, validatePageReady, l) { }
    protected override string PageIdentifier => "MarketResults";
    protected override string PageName => "Markets Page";
}
```

Add a `ClickContinue` override **only** if a future tenant's market-lookup path turns out to need a long custom timeout the base selector can't model.

## Override pattern D — strict popup interception (label-as-toggle click)

When a control has a transparent label overlay that intercepts the standard click but the field isn't an ng-select (so pattern F doesn't apply), drive the click on the outer label directly:

```csharp
private async Task SelectLossesAnswer(string answer) {
    var control = Page.Locator("[id='PolicyData.PLHaveAnyLosses']");
    var label = control.Locator($"label.switcher-wrapper:has-text('{answer}')");
    await label.ClickAsync();  // click the label, not the inner span
}
```

For floated-label ng-select interception specifically, prefer pattern F (arrow-wrapper click) — it handles the panel-open + option-pick + close lifecycle in one helper. Use pattern D for non-ng-select toggle controls (legacy `Product_PolicyPage.PLHaveAnyLosses` etc.). Keep the override small and well-named.

## Override pattern E — mat-datepicker hidden-input via DOM events

KLX renders `EffectiveDate` (CL Policy) and `OperatorDateOfBirth` (Operator) as Angular Material datepickers. The visible `<input placeholder='MM/DD/YYYY'>` is a façade — the underlying `matinput` is `display: none`, and Playwright's `FillAsync` refuses to type into a non-visible element. The fix is to set the value directly via JS and dispatch the events Angular's two-way binding listens for:

```csharp
public override async Task FillForm(Dictionary<string, string>? formData = null) {
    string? dobValue = null;
    if (formData != null && formData.TryGetValue(OperatorDateOfBirth, out var dob)
        && !string.IsNullOrEmpty(dob))
    {
        var filtered = new Dictionary<string, string>(formData) { [OperatorDateOfBirth] = "" };
        dobValue = dob;
        formData = filtered;          // suppress so base.FillForm doesn't fight
    }
    await base.FillForm(formData);
    if (!string.IsNullOrEmpty(dobValue))
        await SetKlxOperatorDobAsync(dobValue);
}

private const string KlxOperatorDobInputLocator =
    "//*[contains(@class,'OperatorDOBDateInput')]//input[@placeholder='MM/DD/YYYY']";

private async Task SetKlxOperatorDobAsync(string value) {
    var input = Page.Locator($"xpath={KlxOperatorDobInputLocator}").First;
    await input.EvaluateAsync(@"(el, v) => {
        el.value = v;
        el.dispatchEvent(new Event('input',  { bubbles: true }));
        el.dispatchEvent(new Event('change', { bubbles: true }));
        el.dispatchEvent(new Event('blur',   { bubbles: true }));
    }", value);
}
```

The registry entry for the field stays as a normal `Input` / `DatePicker`. The override only triggers on the affected page so other tenants are unaffected. Cross-reference: [locator-recipes.md](locator-recipes.md) Recipe 14.

## Override pattern F — floated-label arrow-wrapper (list-driven)

KLX Vehicle Radius and almost every CL Policy ng-select carry an `<app-form-label>` overlay that intercepts the pointer events Playwright sends when opening the panel. The base framework's `Select` action reports success but the option never commits (UI stays on "Please select").

Workaround: click `.ng-arrow-wrapper` (right edge of the ng-select, outside the label overlay) to open the panel, then click the option from the panel. When multiple fields on the same page are affected, encode them as a `(FieldName, ClassFragment, FirstIfMissing)` tuple list and iterate one helper — keeps the override compact:

```csharp
private static readonly (string FieldName, string ClassFragment, bool FirstIfMissing)[]
    LabelInterceptedFields =
{
    (InterviewFieldNames.CurrentBopCarrier,                  "PolicyData.CurrentBopCarrier",  false),
    (InterviewFieldNames.CL_BIPD,                            "PolicyData.CL_BIPD",            true),
    (InterviewFieldNames.CL_MedPay,                          "PolicyData.CL_MedPay",          true),
    (InterviewFieldNames.CombinedUninsuredUnderinsuredMotorist,
                                                             "PolicyData.CL_Combined_UM_UIM", true),
    (InterviewFieldNames.UninsuredMotoristPropertyDamage,    "PolicyData.CL_UMPD",            true),
    (InterviewFieldNames.CL_ComprehensiveDeductible,         "CL_Comprehensive",              true),
    (InterviewFieldNames.CL_CollisionDeductible,             "CL_Collision",                  true),
    (InterviewFieldNames.CL_FireTheftCAC,                    "CL_FireTheftCAC",               true),
};

public override async Task FillForm(Dictionary<string, string>? formData = null) {
    var labelInterceptValues = new Dictionary<string, string>();
    if (formData != null) {
        var filtered = new Dictionary<string, string>(formData);
        foreach (var (fieldName, _, _) in LabelInterceptedFields) {
            if (filtered.TryGetValue(fieldName, out var value) && !string.IsNullOrEmpty(value)) {
                labelInterceptValues[fieldName] = value;
                filtered[fieldName] = "";             // suppress base path
            }
        }
        formData = filtered;
    }
    await base.FillForm(formData);
    foreach (var (fieldName, classFragment, firstIfMissing) in LabelInterceptedFields) {
        if (labelInterceptValues.TryGetValue(fieldName, out var value))
            await SelectViaArrowAsync(classFragment, value, firstIfMissing);
    }
}

private async Task SelectViaArrowAsync(string classFragment, string optionText, bool firstIfMissing) {
    var container = Page.Locator($"xpath=//*[contains(@class,'{classFragment}')]").First;
    try {
        await container.WaitForAsync(new LocatorWaitForOptions {
            State = WaitForSelectorState.Attached, Timeout = 3000 });
    } catch (TimeoutException) {
        _logger?.Warning($"CLPolicy: container '{classFragment}' not present, skipping");
        return;                                       // conditional field; not an error
    }

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

### Waiting for conditionally-rendered fields

Some CL Policy fields only render after earlier selections settle:

- `CL_FireTheftCAC` appears only **after** Comprehensive and Collision deductibles are selected.
- `CL_Combined_UM_UIM` and `CL_MedPay` **disappear** after `HomeAutoInsurance = "No"`.

The `WaitForAsync(state=Attached, Timeout = 3000)` at the top of `SelectViaArrowAsync` covers both cases: 3 seconds is long enough for a one-render-cycle reveal, short enough that legitimately-hidden fields don't slow the test. A timeout there logs a benign skip — correct behaviour when the page genuinely doesn't show the field for the answers chosen. See [troubleshooting/conditional-field-not-rendered.md](troubleshooting/conditional-field-not-rendered.md).

## Override pattern G — first-option fallback for unknown carrier options

Coverage-limit and similar dropdowns vary their option text per carrier; the test data may not match. The arrow-wrapper helper accepts a `firstIfMissing` flag that picks `panel.Locator(".ng-option").First` when no `.ng-option` matches the requested text:

```csharp
var option = panel.Locator(".ng-option", new() { HasText = optionText }).First;
if (await option.CountAsync() == 0 && firstIfMissing) {
    option = panel.Locator(".ng-option").First;
    _logger?.Info($"CLPolicy: '{optionText}' not in '{classFragment}' — selecting first available");
}
```

Use this on fields where carrier-specific options aren't known ahead of time (TC 240782 used it for `CL_BIPD`, `CombinedUninsuredUnderinsuredMotorist`, `UninsuredMotoristPropertyDamage` after the carrier rejected the "100,000 / 300,000" guesses). Leave `firstIfMissing = false` for fields where a specific option is required (e.g. `CurrentBopCarrier = "My insurance company is not listed"` — picking the wrong company gives a different test outcome). See [troubleshooting/dropdown-option-missing.md](troubleshooting/dropdown-option-missing.md).
