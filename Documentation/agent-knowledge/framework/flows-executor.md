---
topic: framework:flows-executor
summary: FlowType enum, FlowInitializer, PlaywrightExecutor — how flows chain pages from start to end.
status: ready
---

# Flows and the Executor

> **When to read:** Phase 2 (decide reuse vs new flow), Phase 3 (registering a new flow), Phase 4 (Executor variant choice — Execute vs ExecuteToPage).

## What a Flow is

A `FlowsHelpers` value bundles three things:
- a `FlowType` (enum value, identifies the flow),
- an ordered `List<Type>` of page-object types (the walk),
- a `Dictionary<string,object?>` of default form data (resolved per-LOB by `FlowDefaultsProvider`).

`FlowRegistry` lazy-loads flows on first request via reflection over `[FlowInitializer]` attributes. The static method must be parameterless and return `FlowsHelpers`.

## Defining a flow

`Bolt.Automation.FrontEnds/Projects/Interview/Flows/Flows.cs`:

```csharp
[FlowInitializer(flowType: FlowType.InterviewHO3Flow)]
public static FlowsHelpers InterviewHO3Flow() => CreateInterviewFlow(
    FlowType.InterviewHO3Flow,
    new List<Type> {
        typeof(Product_StartPage),
        typeof(Product_LobsPage),
        typeof(Product_HomePage),
        typeof(Product_StructurePage),
        typeof(Product_FeaturesPage),
        typeof(Product_PolicyPage),
        typeof(Product_ApplicantPage),
        typeof(Product_ResultsPage)
    },
    FlowDefaultsProvider.GetFlowDefaults(FrontEndType.Interview, LobType.Home)
);
```

`CreateInterviewFlow(flowType, pages, defaults)` constructs a `FlowsHelpers` and registers it with `FlowRegistry`.

For the KLX CL Auto old interview (TC 240782):

```csharp
[FlowInitializer(flowType: FlowType.InterviewCLAutoFlow)]
public static FlowsHelpers InterviewCLAutoFlow() => CreateInterviewFlow(
    FlowType.InterviewCLAutoFlow,
    new List<Type> {
        typeof(Product_StartPage),     // /CL_Start — Business Contact Info
        typeof(Product_MarketsPage),   // /MarketResults — Get Quotes button only
        typeof(Product_BusinessPage),  // /CL_Business
        typeof(Product_VehiclePage),   // /CL_Vehicle — VIN-decode handshake (pattern B)
        typeof(Product_OperatorPage),  // /CL_Operator — DOB mat-datepicker (pattern E)
        typeof(Product_CLPolicyPage),  // /CL_Policy — floated-label ng-selects (pattern F)
        typeof(Product_ApplicantPage), // /CL_Applicant
        typeof(Product_ResultsPage)
    },
    FlowDefaultsProvider.GetFlowDefaults(FrontEndType.Interview, LobType.CommercialAuto)
);
```

Notes:

- `Product_StartPage` doubles as the "Business Contact Info" step on KLX — its `PageIdentifier = "_Start"` substring-matches `/CL_Start`. We did **not** create a separate `Product_BusinessContactPage`; `Product_StartPage` carries an industry-typeahead override and the `Certification`/`DeclinationReason` ordering is handled by `DependsOn` in the field registry (see [recipes/override-patterns.md](../recipes/override-patterns.md) pattern A).
- `Product_CLPolicyPage` is a separate class from `Product_PolicyPage` because the CL Policy DOM needs the arrow-wrapper and mat-datepicker overrides. Its `PageIdentifier = "CL_Policy"` (unique substring) prevents collision with `Product_PolicyPage`'s `"_Policy"`. See [domain/partners/kraftlakex.md](../domain/partners/kraftlakex.md) "Page identifiers".

## Existing flow inventory (Interview)

Quick reference to avoid duplicating:

| FlowType | Pages | LOB | Tenant scope |
|---|---|---|---|
| `InterviewHO3Flow` | Start → Lobs → Home → Structure → Features → Policy → Applicant → Results | Homeowners | All |
| `InterviewHO4Flow` | (same shape as HO3) | Renters | All |
| `InterviewAutoFlow` | Start → Lobs → Vehicle → Operator → Policy → Applicant → Results | Personal Auto | All |
| `InterviewBundleFlow` | Start → Lobs → Home → Structure → Features → Vehicle → Operator → Policy → Applicant → Results | Home + Auto bundle | All |
| `InterviewFloodFlow` | Start → Lobs → Home → Structure → Policy → Applicant → Results | Flood | All |
| `InterviewWCFlow` | Start → Business → Locations → Employee → Policy → Applicant → Results | Workers Comp | All |
| `InterviewMotorcycleFlow` | (Auto-shaped) | Motorcycle | All |
| `InterviewCLAutoFlow` | Start → Markets → Business → Vehicle → Operator → **CLPolicy** → Applicant → Results | Commercial Auto (KLX old interview) | KLX |

Other FrontEnds register their own flow types in their `Flows.cs` (D2C, HQXAgent, etc.). Look there before adding a new one.

## When to add a new flow vs reuse

Add a new flow when:
- The page sequence differs from any existing flow (extra page, different order, missing page).
- The tenant uses a different URL scheme that requires a different `PageIdentifier` per page (rare — usually substring matching handles it).

Reuse an existing flow when:
- The page sequence is the same; only field values differ (set them inline in the test's `formData` dictionary or pull a reusable `ApplicationTestData.<X>FormData.Defaults` profile — see [domain/partners/INDEX.md](../domain/partners/INDEX.md)).
- The tenant adds a popup mid-flow — handle the popup at the test-class level around `ExecuteToPage`, don't fork the flow.

## `PlaywrightExecutor` — two variants

`Bolt.Automation.FrontEnds/Executor/PlaywrightExecutor.cs`.

### Variant 1: `Execute<TStart, TEnd>` — start from URL

```csharp
var resultsPage = await Executor.Execute<Product_StartPage, Product_ResultsPage>(
    FlowType.InterviewHO3Flow,
    formData,                                 // inline Dictionary<string,string>
    fillForms: true,
    startUrl: "https://qa.example.com/start"  // optional — navigates if provided
);
```

Use when:
- The test owns the navigation. Login already happened, you have the URL of the first flow page, and you want the executor to drive from there.

### Variant 2: `ExecuteToPage<TEnd>` — resume from a live page

```csharp
ScopeContext.Set(ctx => ctx.FrontEnd, FrontEndType.Interview);
var startPage = PageFactory.CreatePage<Product_StartPage>();  // page validates itself

var resultsPage = await Executor.ExecuteToPage<Product_ResultsPage>(
    FlowType.InterviewCLAutoFlow,
    startPage,                  // a live IInterview instance
    formData,                   // CLAutoFormData.Defaults + per-test overrides
    fillForms: true);
```

Use when:
- An ADBX popup or external nav landed you on the flow's first page. The executor takes over from there.
- This is the pattern in TC 240782: ADBX popup ADD opens a new tab on `/CL_Start`; we switch to the tab, switch FrontEnd, instantiate `Product_StartPage`, and `ExecuteToPage` from it.

### Decision

If your flow starts from a URL → `Execute<TStart, TEnd>`.
If your flow resumes from a page that was opened by a popup, an ADBX action, or any external transition → `ExecuteToPage<TEnd>`.

## What happens inside the executor

For each page in the flow (from `TStart` to `TEnd`):
1. If the current page hasn't been instantiated yet, `PageFactory.CreatePage<TPage>()` constructs and validates it.
2. If `fillForms == true`, calls `page.FillForm(mergedFormData)` — `MergeDataManager` merges user `formData` with `FlowDefaultsProvider` defaults filtered by page tagging.
3. Calls `page.ClickContinue()` (unless this is `TEnd`).
4. Advances to the next page in the flow (`PageFactory.CreatePage<NextPage>()`).

If a step throws `PageElementException` / `NavigationException` / `PopupTimeoutException`, the executor surfaces it. The `[TearDown]` in `UITestBase` captures the DOM snapshot for diagnosis (under `bin/Debug/net10.0/TestResults/<test>/page_source_*.html`).

## Skipping pages

Both variants accept an optional `pagesToSkip` list — a `HashSet<Type>` of page types to instantiate but **not** call `FillForm` on. Useful when a page is a no-op for some flows (e.g. a transitional confirmation page).

If a page must be **fully skipped** (no instantiation), redefine the flow without that page or use `perPageAction` to short-circuit.

## `perPageAction` — observability hook

`ExecuteToPage` accepts an optional `Func<IInterview, Task>` that runs after each page's `FillForm`/`ClickContinue` cycle. Useful for:
- Snapshot capture mid-flow.
- Asserting a calculated value before continuing.
- Triggering a popup-handler check.

Don't abuse it for control flow — if a page needs special logic, override its `FillForm`/`ClickContinue`.

## Adding a new `FlowType`

1. Add the enum value in `FlowType.cs`:
   ```csharp
   public enum FlowType {
       // ...
       InterviewCLAutoFlow,
   }
   ```
2. Add the `[FlowInitializer]` static method in `Flows.cs` (next to similar flows for grep-ability).
3. If the LOB is new, add a `LobType` enum value too.
4. Don't add a flow alias just for ergonomics. Multiple flow names pointing at the same page sequence is a maintenance burden.

## When **not** to use a flow

- Single-page tests (e.g. login + assert). Just instantiate the page, call `FillForm`, assert. No flow needed.
- API-only tests. They use `TestBase`, not `UITestBase`, and don't go through pages or executor.
- Tests that diverge mid-walk based on UI state (e.g. branch on whether a popup appears). Use a flow up to the divergence, then switch to manual page transitions.
