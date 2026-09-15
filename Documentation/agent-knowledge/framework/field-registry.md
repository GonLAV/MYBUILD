---
topic: framework:field-registry
summary: FieldRegistry mechanism — sparse dictionaries, Pages tagging, DefaultValue injection, MergeDataManager.
status: ready
---

# Field Registry — mechanism

> **When to read:** Phase 2 (gap analysis — does this field already exist?), Phase 3 (registering new fields), Phase 4 (default-injection bugs).
>
> **Companion file:** [../recipes/locator-recipes.md](../recipes/locator-recipes.md) — locator patterns per `UIFieldType`.

## What a field registry is

For each FrontEnd, a static `Dictionary<string, UIElement>` decorated with `[FieldRegistry]` is the source of truth for "given this field name, how do I interact with it on the page." Examples:

- `Bolt.Automation.FrontEnds/Projects/Interview/FormData/FieldRegistryInterview.cs` — `FieldRegistryInterview.Fields`.
- `Bolt.Automation.FrontEnds/Projects/ADBX/FormData/FieldRegistryADBX.cs` — `FieldRegistryADBX.Fields`.
- (etc. — one per FrontEnd folder under `Projects/<FrontEnd>/FormData/`)

`FieldRegistryProvider` (`Bolt.Automation.FrontEnds/FormData/Base/FieldRegistryProvider.cs`) discovers these by reflection on first use and exposes:

```csharp
public static Dictionary<string, UIElement> GetRegistry(FrontEndType frontEndType);
```

The registry is **keyed on the canonical short field name** — usually a `nameof()` constant from `FieldNames` partials.

## FieldNames partials

`FieldNames` is a public partial class spread across:

- `Bolt.Automation.FrontEnds/FormData/Common/FieldNames.cs` — shared across all FrontEnds (auto, home, commercial, payment).
- `Bolt.Automation.FrontEnds/Projects/Interview/FormData/FieldNamesInterview.cs` — Interview-specific.
- `Bolt.Automation.FrontEnds/Projects/ADBX/FormData/FieldNamesADBX.cs` — ADBX-specific.

Convention: `public const string FieldName = nameof(FieldName);` so the constant value matches the constant name.

**Critical:** prefer reusing constants from `Common.FieldNames` over inventing new aliases. The registry is keyed exactly on those names. If you write `[FieldNames.VehicleVIN] = "..."` in the test's `formData` dict but only `VIN` is registered, the field is silently skipped. (This bit us hard when authoring TC 240782 — use `VIN`, `BodyStyle`, `AnnualMileage`, `PrimaryUseOfVehicle`, `Email`, `OrganizationName`, `EposNaicDescription`.)

## The `UIElement` model

Defined in `Bolt.Automation.FrontEnds/FormData/Models/UIElement.cs`:

```csharp
public class UIElement {
  public required LocatorType Strategy { get; set; }       // CSS, XPath, Text, Role, TestId, Placeholder, Label, Title, Alt, Name
  public required LocatorSet Locators { get; set; }        // One or more locator strings — supports {0} placeholder
  public required UIFieldType FieldType { get; set; }      // Input, Dropdown, MultiDropdown, Radio, Checkbox, DatePicker, Button, Link, SearchDropdown
  public required string? DefaultValue { get; set; }       // Injected if user formData omits the key
  public required HashSet<Type>? Pages { get; set; }       // [typeof(Product_StartPage)] — gates which pages auto-fill
  public ValidationRules? Validation { get; set; }
  public string? DependsOn { get; set; }                   // Field name this one depends on
  public string? DependsOnValue { get; set; }              // Trigger value for dependency
  public bool Required { get; set; }
  public ElementInteractionOptions? InteractionOptions { get; set; }
  public bool PreserveCasing { get; set; }                 // Keeps {0} substitution case-exact
  public string? CustomErrorMessage { get; set; }
  public string? Version { get; set; }
  public string? Label { get; set; }
  public string? FieldName { get; set; }
}
```

### `Pages` — the gate

When `MergeDataManager.GetSmartFormData(pageType, userInput)` runs, it iterates every field in the registry whose `Pages?.Contains(pageType) == true`. **Untagged fields (or fields tagged for a different page) are skipped.** This is your primary tool for keeping fields out of the iteration on pages where they don't belong.

Example registry entries (paraphrased from `FieldRegistryInterview.cs`):

```csharp
[FirstName] = new UIElement {
  Strategy = LocatorType.XPath,
  Locators = "//*[contains(@class,'PolicyData.FirstName')]//input",
  FieldType = UIFieldType.Input,
  DefaultValue = "AutoTest",
  Pages = [typeof(Product_StartPage)],
},

[DateOfBirth] = new UIElement {
  Strategy = LocatorType.XPath,
  Locators = "//*[contains(@class,'PolicyData.DateOfBirth')]//input[contains(@class,'form-control')]",
  FieldType = UIFieldType.DatePicker,
  DefaultValue = "01/01/1990",
  Pages = [typeof(Product_StartPage), typeof(Product_ApplicantPage)],
},
```

### `DefaultValue` — when it's injected

`MergeDataManager.GetSmartFormData` (in `Bolt.Automation.FrontEnds/FormData/Helpers/MergeDataManager.cs`):

```csharp
foreach (var (fieldName, fieldConfig) in fields.Where(kvp => kvp.Value.Pages?.Contains(pageType) == true)) {
    result[fieldName] = userInput?.TryGetValue(fieldName, out var userValue) == true
        ? userValue
        : fieldConfig.DefaultValue ?? string.Empty;
}
```

So a field on this page with no user-provided value gets `DefaultValue` (or `""`).

### Test dictionaries should be sparse

Because `MergeDataManager.GetSmartFormData` falls back to the registry's `DefaultValue` for every key the test omits, the test's `formData` dictionary should contain **only the keys whose values differ from the registry default**. Re-supplying defaults is noise — it hides the values that actually matter for the scenario, and the entries silently rot when the registry default later changes.

Concrete rule of thumb:

- If `registry[F].DefaultValue == "No"` and you want `"No"`, don't include `[F] = "No"` in the test dict.
- If you want `"Yes"`, include it.
- If the field isn't on the current page (`Pages` filter excludes it), `GetSmartFormData` won't iterate it — even putting it in the dict is a no-op for that page.

This makes the test self-documenting: every key in the dict is a deliberate divergence from the framework default. See also [test-class.md](test-class.md) ("Test method shape") and [../domain/partners/INDEX.md](../domain/partners/INDEX.md) ("Reusable UI form-data profiles") for layering a per-flow defaults profile on top of the registry defaults.

### `DependsOn` / `DependsOnValue` — conditional fill

A field is filled only if its parent has the trigger value:

```csharp
[YearRoofUpdated] = new UIElement {
  DependsOn = RoofReplaced,
  DependsOnValue = "Complete Update",
  // ...
},
```

`FormDataHelper.GetOrderedFields` topologically sorts so the parent fills first; `ProcessField.ShouldFillField` checks the dependency before invoking the locator.

## Empty-string suppression — the override pattern

`FormDataHelper.ProcessField` skips a field when its value is empty string:

```csharp
if (string.IsNullOrEmpty(value)) return;  // (paraphrased)
```

This lets a `FillForm` override **prevent** the default from being injected without un-tagging the field from `Pages`:

```csharp
// Product_VehiclePage.cs:55-65
var filtered = new Dictionary<string, string>(formData);
filtered[VIN] = "";          // already typed in the VIN-decode flow
filtered[PLYear] = "";       // populated by decode — don't fight cascading dropdowns
filtered[PLMake] = "";
filtered[PLModel] = "";
filtered[BodyStyle] = "";    // also populated by decode
formData = filtered;
await base.FillForm(formData);
```

Note: for KLX `VehicleOwnerShip` the right move is **not** suppression — the test should supply a tenant-valid option like `"Less than 1 month"` (which the registry default `"Owned"` doesn't satisfy on KLX). Suppression is for fields you've genuinely handled out-of-band; option-mismatch belongs in the test's formData. See [../domain/partners/kraftlakex.md](../domain/partners/kraftlakex.md).

**When to suppress vs untag:**
- *Suppress* (set to `""` in override) — the field is normally filled, but for this specific page/flow you've already handled it out-of-band. Keep the registry entry intact for other tests.
- *Untag* (`Pages = []`) — the field is genuinely never iterated by `MergeDataManager`; its UIElement exists only so an out-of-band helper can call `PageHelper.InteractWithField(name, value)` directly. Reserve this for fields whose dependency on others can't be expressed with `DependsOn` (rare). For most conditionally-revealed fields like `DeclinationReason`, prefer keeping `Pages = [...]` and adding `DependsOn = Certification, DependsOnValue = "true"` with a generous `InteractionOptions.Timeout`.

## Iteration pipeline

`FormDataHelper.cs`:

```csharp
public static (Dictionary<string,string> formData, Dictionary<string,UIElement> fields)
    GetFieldsForPage(object pageInstance, IScopeContext scopeContext, Dictionary<string,string>? userOverrides) {
    // Reads FieldRegistryProvider.GetRegistry(scopeContext.FrontEnd)
    // Calls MergeDataManager.GetSmartFormData(pageInstance.GetType(), userOverrides)
    // Returns (merged formData, the registry filtered to this page's fields)
}

public static async Task FillRelevantFields(this IPage page,
    Dictionary<string,string> formData,
    Dictionary<string,UIElement> fields,
    IScopeContext scopeContext,
    IPageHelper? pageHelper = null,
    IAutomationLogger? logger = null) {
    foreach (var fieldName in GetOrderedFields(fields)) {
        await ProcessField(formData, fields, fieldName, pageHelper, logger);
    }
}
```

`InterviewBase.FillForm` is a thin wrapper:

```csharp
public virtual async Task FillForm(Dictionary<string,string>? formData = null) {
    var (pageSpecificData, fields) = FormDataHelper.GetFieldsForPage(this, ScopeContext, formData);
    await Page.FillRelevantFields(pageSpecificData, fields, ScopeContext, PageHelper, _logger);
}
```

## XPath OR for tenant-variable class schemes

Some tenants (KLX) collapse `PolicyData.Vehicles[?(@__id__='guid')].Year` into class names like `PolicyDataVehicles__id__-{shortGuid}-YearVehicleDropdownWithSearch...`. Use XPath OR to match both the v3 scheme and the KLX scheme:

```csharp
[PLYear] = new UIElement {
  Strategy = LocatorType.XPath,
  Locators =
    "//*[contains(@class,'PolicyData.PLYear')]//ng-select | " +
    "//*[contains(@class,'PolicyDataVehicles') and contains(@class,'Year') and not(contains(@class,'IsDifferent'))]//ng-select",
  FieldType = UIFieldType.Dropdown,
  // ...
},
```

Use `not(contains(...))` to exclude sibling fields whose class names share a prefix (e.g. `YearOfDriving`).

KLX CL Auto also collapses **per-vehicle** controls into the same class string. For coverage fields under a specific Vehicle (Full Glass, Road Side, Comp/Coll Deductible, etc.) the class attribute reads:

```
class="PolicyData.Vehicles[?(@__id__='adcd99ef-1ce9-485e-b6d6-96dd9c0ca24d')].CL_FullGlass …"
```

Because XPath `contains(@class, …)` is substring-based, you can match the per-vehicle instance with just the CL-prefixed leaf class — no need to encode the `Vehicles[?(...)]` prefix:

```xpath
//*[contains(@class,'CL_FullGlass')]//label[contains(@class,'switcher-wrapper') and .//span[contains(normalize-space(.),'{0}')]]
```

See [../domain/partners/kraftlakex.md](../domain/partners/kraftlakex.md) "CL Policy per-vehicle coverage classes" for the full list of CL-prefixed names.

## Strict-mode pitfalls

Playwright runs in strict mode — a locator that matches multiple elements throws. Two common failure modes:

1. **`.loader, .loading-overlay, .spinner`** matches more than one node when more than one loader is present. Fix: `Page.Locator(Loader).First`.
2. **`ng-dropdown-panel .ng-option`** at page scope can match options from multiple open dropdowns. Fix: scope to the control container — `controlContainer.Locator("ng-dropdown-panel .ng-option").First`.

## Dynamic defaults

`DefaultValue` can call helpers like `RandomManager.GetRandomString(6)` at static init. The value is captured **once** when `[FieldRegistry]` is first read — not per-test. If you need per-test randomness (random emails, etc.), set the value inline in the test's `formData` dictionary instead.

## Adding a field — concrete recipe

1. Add the constant to `FieldNamesInterview.cs` (or whichever partial fits):
   ```csharp
   public const string OwnerInvolvedInDailyOperations = nameof(OwnerInvolvedInDailyOperations);
   ```
2. Add the registry entry in `FieldRegistryInterview.cs`:
   ```csharp
   [OwnerInvolvedInDailyOperations] = new UIElement {
       Strategy = LocatorType.XPath,
       Locators =
           "//*[contains(@class,'PolicyData.OwnerInvolvedDailyOperations')]" +
           "//label[contains(@class,'switcher-wrapper') and .//*[contains(text(),'{0}')]]",
       FieldType = UIFieldType.Button,    // switcher click via label
       DefaultValue = "Yes",
       Pages = [typeof(Product_BusinessPage)],
       PreserveCasing = true,             // keeps {0}="Yes" capital Y
       InteractionOptions = new ElementInteractionOptions { Timeout = 10000 }
   },
   ```
3. Reference it in the test's `formData` dictionary (only if your test needs a value other than the registry default):
   ```csharp
   [FieldNames.OwnerInvolvedInDailyOperations] = "Yes",
   ```
4. Build & run. If the field doesn't fire, check (in this order):
   - Is `Pages` correct?
   - Is the FrontEnd correct? (Interview registry vs ADBX registry)
   - Does the locator actually match? Open the captured `page_source_*.html` and grep the class name.

See [../recipes/locator-recipes.md](../recipes/locator-recipes.md) for ready-made locator recipes per `UIFieldType`.

## Adding a field is a change to every test that fills that page

`Pages` plus `DefaultValue` is not an opt-in for your test — it is an opt-in for **every** fill of those pages. `MergeDataManager` falls back to `DefaultValue` for every key the test omits, so a new entry starts being answered by suites that never asked for it.

A real regression from this shape: adding `ElectricCircuitBreaker` with `Pages = [typeof(HQXConsumer_DiscountsPage)]` and `DefaultValue = "true"` made every Discounts fill click that radio, which turned an already-green MPQ3 DNQ test red.

The nastier variant is a default that **reveals a required field nothing can answer**. `IsNewBusiness`
(`DefaultValue = "true"`) was added for the WC scenario on `ProductInsuranceHistoryPageCL` — a page BOP,
GL and Commercial Auto share. Ticking it disables that LOB's prior-carrier dropdown and reveals a
required "reason no prior ..." follow-up, which had no registry entry on the Auto flow, so the fill pass
had nothing to answer with and the flow never left Insurance History. Note what a default asserts:
`IsNewBusiness` is a fact about the insured, and three existing entries (`CurrentBopCarrier`,
`CurrentWCCarrier`, `PriorCarrierAutoCL`) only make sense when it is clear — so `"false"` is the default
the rest of the registry already implies, and the one scenario that really is a new business says so from
its test.

When a field is only needed by one scenario, keep normal fills clear of it:

- **Gate it with `DependsOn` / `DependsOnValue`** on a parent whose default excludes your case — the pattern `AnimalsOnThePremises_Exotic` and `DwellingUsage` use. A normal fill skips the child; a test that sets the parent gets both, in order.
- Or leave it out of `Pages` and drive it explicitly with `IPageHelper.InteractWithField`.

**After adding or changing a shared registry entry (or a shared helper), re-run the tests that already passed on the affected pages.** A registry edit has no compile-time blast radius, so the only signal is a run. Scope the check to the suites that fill those pages rather than the whole project.

Scoping the *constant* matters too: declare it in the project-specific `FieldsName<Project>` partial when another project already declares the same name. A duplicate in `Common.FieldNames` makes every reference in that other project ambiguous (CS0229) — `ElectricCircuitBreaker` is declared by D2C, so HQXConsumer's copy belongs in `FieldsNameHQXConsumer`.

## Anti-patterns

- **Inventing new field-name aliases** like `VehicleVIN` when `VIN` exists in `Common.FieldNames`. The registry is keyed on canonical names — aliases produce silent skips.
- **Adding an entry with `Pages` + `DefaultValue` for a one-scenario field.** Every test filling that page now answers it. Gate with `DependsOn`, or drive it explicitly.
- **Tagging a field for a page where its DOM class doesn't exist.** That causes a noisy `MergeDataManager` timeout. But the opposite is fine and common: when the **same** locator pattern works on two pages (e.g. `HomeAutoInsurance`, `CurrentBopCarrier`, `CL_TransportationExpense` appear on both `Product_PolicyPage` and `Product_CLPolicyPage` with identical class names), extend `Pages` to include both — it avoids duplicating the entry under different names.
- **Setting `DefaultValue` to a value that doesn't exist as an option on the target tenant.** KLX's `CLLengthVehicleOwnership` doesn't have "Owned" — its options are ranges like "Less than 1 month". Either pick a tenant-valid default, or leave `DefaultValue = null` and require the test to supply a value.
- **Relying on `DefaultValue` for per-test data** (e.g. random emails). The default is captured at static init, not per-test. Set the value inline in the test's `formData` dictionary instead.
