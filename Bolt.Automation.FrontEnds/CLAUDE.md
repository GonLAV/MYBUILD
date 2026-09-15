# Bolt.Automation.FrontEnds — CLAUDE.md

Playwright UI automation: page objects, flows, `FieldRegistry`, `BrowserManager`, `PlaywrightExecutor`.

## Page objects

```csharp
await BrowserManager.NavigateAsync(url);
var page = PageFactory.CreatePage<Product_StartPage>();
var value = await page.GetFieldValue();
```

Create pages via `PageFactory.CreatePage<T>()`.

## Field interactions — string field-name constants

- Pass **field names as strings** to `IPageHelper` (`InteractWithField`, `FillField`, `ClickField`). Do **not** use `UIElement` objects directly.
- Field dispatch is built into `PageHelper` — no reflection on the interaction path (registry/flow *discovery* uses reflection). Use `PageHelper.InteractWithField(fieldName)`. (The old `PageHelperFieldExtensions` were removed from the codebase.)

## Flows

Flows are `[FlowInitializer]`-registered page sequences keyed by a per-product `FlowType` enum; `PlaywrightExecutor.Execute<TStart, TEnd>(flowType, …)` walks them, filling each page from the flow's default data.

## Never do

- **No XPath in page objects or tests** — locators belong in the `FieldRegistry`. Inside the registry prefer role / text / CSS; XPath is the accepted fallback for tenant-variant class schemes (`kb lookup --topic recipe:locator-recipes`).
- **No `Thread.Sleep`** — rely on Playwright auto-waiting / explicit wait helpers.

## Deeper context

- `kb lookup --topic framework:field-registry`
- `kb lookup --topic framework:page-objects`
- `kb lookup --topic framework:flows-executor`
- `kb lookup --topic framework:popups`
- Agent CLI: `code field-lookup <name>`, `code flow-trace --flow <name>`.
