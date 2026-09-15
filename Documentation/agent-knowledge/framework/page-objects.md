---
topic: framework:page-objects
summary: PageBase contract, FillForm + ClickContinue, override patterns for custom fields.
status: ready
---

# Page objects — mechanism

> **When to read:** Phase 3 (creating or extending a page), Phase 4 (override decisions for out-of-band fields, custom continue, page validation timing).
>
> **Companion file:** [../recipes/override-patterns.md](../recipes/override-patterns.md) — the seven override patterns (A–G) with worked examples.

## Page-object hierarchy

```
IInterview / IDashboard
  └─ InterviewBase     (Projects/Interview/Base/InterviewBase.cs)
       └─ Product_StartPage, Product_VehiclePage, Product_BusinessPage, …
  └─ ADBX_BasePage     (Projects/ADBX/Base/ADBX_BasePage.cs)
       └─ ADBX_HomePage, ADBX_AccountSummaryPage, …, popups in ADBX_*Popup.cs
```

Each base provides:
- Constructor with `IBrowserManager`, `IPageHelper`, `IScopeContext`, `bool validatePageReady = true`, optional `IAutomationLogger?`.
- `PageIdentifier` (abstract) — URL substring used by `PageValidationHelper.ValidatePageReadyAsync`.
- `PageName` (abstract) — human-readable name used in logs.
- `ValidatePageReady()` (virtual) — overrideable for custom readiness checks.
- `FillForm(Dictionary<string,string>?)` (virtual) — default delegates to `FormDataHelper`/`FillRelevantFields`.
- `ClickContinue()` (virtual) — default tries `InterviewBase.ContinueButton` selectors.

## InterviewBase essentials

`Bolt.Automation.FrontEnds/Projects/Interview/Base/InterviewBase.cs`:

```csharp
public const string ContinueButton =
    "button.Next, .button.Confirm, button.Get.quotes";  // comma-separated CSS list
public const string Loader =
    ".loader, .loading-overlay, .spinner";              // also comma-separated
public const string ErrorMessage =
    ".error-message, .field-error, .form-error, .validation-error";

protected abstract string PageIdentifier { get; }
protected abstract string PageName { get; }
public IPage Page => _page ??= BrowserManager.GetCurrentTab()!;

public virtual async Task ValidatePageReady() {
    await PageHelper.ValidatePageReadyAsync(PageIdentifier, PageName);
    _logger?.LogBusinessRule("PageReady", true, $"{PageName} page validated successfully");
}

public virtual async Task ClickContinue() => await ClickContinueButton(throwErrorMessage: true);

public virtual async Task FillForm(Dictionary<string,string>? formData = null) {
    var (pageData, fields) = FormDataHelper.GetFieldsForPage(this, ScopeContext, formData);
    await Page.FillRelevantFields(pageData, fields, ScopeContext, PageHelper, _logger);
}
```

`ClickContinueButton` walks the comma-separated `ContinueButton` selectors and clicks the first visible match. Adding a new continue button selector means **adding to that list**, not duplicating it.

## `PageIdentifier` is a substring

`PageHelper.ValidatePageReadyAsync(expectedUrlPart, pageName)` checks `Page.Url.Contains(expectedUrlPart)`. Pick the smallest unique substring of the URL:

| URL fragment | Good `PageIdentifier` | Bad |
|---|---|---|
| `/CL_Start?accountId=…` | `_Start` (matches CL_Start, BL_Start, etc.) | `CL_Start?accountId=` (over-specific) |
| `/MarketResults` | `MarketResults` | `_Markets` (wrong — KLX uses MarketResults, not Markets) |
| `/CL_Vehicle` | `_Vehicle` | `CL_Vehicle` (won't reuse for personal-line `Vehicle`) |

For TC 240782 `_Start` matches both BOLTAG `Start` and KLX `CL_Start`. Reuse via substring is the framework's idiom.

## Page-object scaffolding template

For a new page, the smallest useful skeleton is:

```csharp
public class Product_NewPage : InterviewBase
{
    public Product_NewPage(
        IBrowserManager browserManager, IPageHelper pageHelper,
        IScopeContext scopeContext, bool validatePageReady = true,
        IAutomationLogger? logger = null)
        : base(browserManager, pageHelper, scopeContext, validatePageReady, logger) { }

    protected override string PageIdentifier => "_NewSegment";
    protected override string PageName => "New Page";
}
```

That's it. `FillForm` and `ClickContinue` inherit from base. Add overrides only when one of the patterns in [../recipes/override-patterns.md](../recipes/override-patterns.md) applies.

## Strict-mode pitfalls (most common bugs)

1. **`Loader` matches multiple nodes.** `Page.Locator(Loader)` → `.First`.
2. **`ng-dropdown-panel .ng-option` at page scope** matches across all open dropdowns. Scope to the control container.
3. **Multiple `<input>` inside the same registry container.** Tighten with `[contains(@class,'form-control')]` or `[@type='text']`.
4. **Two switcher labels with overlapping text** (e.g. "Yes, sometimes" vs "Yes"). Use `normalize-space(.)='Yes'` instead of `contains(.,'Yes')`.

## When **not** to override

- The default selector list works — just slower than you'd like. Raise `InteractionOptions.Timeout` on the registry entry instead.
- A field needs a non-default value. That's what the test's inline `formData` dictionary is for — set the value at the test level.
- The field has a conditional reveal that `DependsOn` + `DependsOnValue` + a generous `Timeout` can model. Try that first; only fall back to a page-object override if the trigger is something other than a value change (popup, navigation, async load).
- "Just to be safe." Overrides cost maintenance. The base path is tested by every other test that uses the page.

## When **to** override

- The field is part of a multi-step handshake the registry can't model (VIN decode → Year/Make/Model/BodyStyle populate; [override-patterns.md](../recipes/override-patterns.md) pattern B).
- A control has an overlay/intercepting element that breaks Playwright's standard click on ng-select center — pattern F (floated `<app-form-label>`).
- A field is rendered as a Material datepicker with a hidden underlying matinput that `FillAsync` refuses — pattern E (DOM events).
- A control's dropdown options vary per carrier and the test can't predict the exact text — pattern G (first-option fallback).
- The Continue button doesn't match any selector in `InterviewBase.ContinueButton` and needs a custom wait/timeout (rare; usually adding to the base selector list is enough).
- The page validation requires more than a URL substring (rare; `ValidatePageReady` override).
