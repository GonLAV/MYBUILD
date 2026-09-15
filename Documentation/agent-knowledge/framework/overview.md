---
topic: framework:overview
summary: How the framework wires up — DI flow, IScopeContext, page factories, base classes.
status: ready
---

# Framework Overview

> **When to read:** Phase 1 (parse the TC — orient on tenant/environment/role) and Phase 2 (understand which subsystems will be touched).

## Solution layout

```
Bolt.Automation.sln
├── Bolt.Automation.Common              — IScopeContext, enums, logging, exceptions, reporting (TrxParser)
├── Bolt.Automation.Core                — DI bootstrap (TestInfrastructure)
├── Bolt.Automation.TestDataProvider    — UserDataStore, providers, builders
├── Bolt.Automation.FrontEnds           — Playwright page objects, FieldRegistry, Flows
├── Bolt.Automation.ApiClients          — Refit clients (ADBX, CaseManager, Platform, SSO)
├── Bolt.Automation.InternalServices    — Internal microservice integrations
├── Bolt.Automation.ExternalServices    — LaunchDarkly, external integrations
├── Automation.Configuration            — Configuration models
├── Bolt.Automation.Tests               — Main NUnit test project
├── Bolt.Automation.InfraTests          — Infrastructure verification
├── Bolt.Automation.TestDiscovery       — Test discovery for distributed exec
├── Bolt.Automation.WorkerAgent         — .NET hosted service for orchestrator-driven runs
└── Bolt.Automation.AgentTools          — AI-agent CLI (kb / tc / failure / code / browser commands)
```

For TC automation you almost always touch:
- `Bolt.Automation.FrontEnds` — page objects + field registry.
- `Bolt.Automation.Tests/Tests/<Tenant>/` — the test class.
- `Bolt.Automation.TestDataProvider/Repositories/DataStores/UserDataStore.cs` — only if a tenant/role is missing.

## FrontEnds and projects

The `Bolt.Automation.FrontEnds` assembly partitions automation by **FrontEnd** (which UI app the test drives):

| FrontEnd | Project folder | Examples |
|---|---|---|
| `ADBX` | `Projects/ADBX/` | Bolt Agent Dashboard — login, accounts, quotes list, popups |
| `Interview` | `Projects/Interview/` | Quote interview pages (Start/Markets/Business/Vehicle/…) |
| `D2C` | `Projects/D2C/` | Direct-to-Consumer flows |
| `STS` | `Projects/STS/` | Login redirect / SSO landing |
| `CaseManager` | `Projects/CaseManager/` | Underwriter case management |
| `HQXAgent` | `Projects/HQXAgent/` | Progressive partner |
| ... | | |

The `FrontEndType` enum (in `Bolt.Automation.Common`) enumerates these. `IScopeContext.Set(ctx => ctx.FrontEnd, FrontEndType.X)` switches which `FieldRegistry` is consulted by `MergeDataManager.GetSmartFormData` — see [field-registry.md](field-registry.md).

**Crucial pattern:** a single test often spans multiple FrontEnds. Example (KLX CL Auto, TC 240782):

```csharp
ScopeContext.Set(ctx => ctx.FrontEnd, FrontEndType.ADBX);  // login + open account + click NEW QUOTE
// ... ADBX work ...
ScopeContext.Set(ctx => ctx.FrontEnd, FrontEndType.Interview);  // walk the interview pages
```

If you see "could not find UIElement for field X" mid-flow, first check that the FrontEnd is the one whose registry contains X.

## DI and lifecycle

`Bolt.Automation.Tests/TestExtension/Base/TestBase.cs` and `UITestBase.cs` are the test base classes. They build a **per-test scoped service provider** using the layered registration in `Bolt.Automation.Core/AddInfrastructureServices`. A test gets:

- `IBrowserManager` — page/tab lifecycle, navigation, screenshot manager.
- `IPageFactory PageFactory` — `PageFactory.CreatePage<TPage>()` constructs a page with DI and validates page-ready.
- `PlaywrightExecutor Executor` — flow execution (Execute or ExecuteToPage).
- `IPageHelper _pageHelper` — locator interaction, element waiting, table operations.
- `IScopeContext ScopeContext` — per-test context store (tenant, env, URL, user, FrontEnd, custom test values).
- `IAutomationLogger _logger` — structured step/business-rule logging.
- `TestContextAccessor TestContextAccessor` — read-only access to the resolved test data (`CurrentUserCollection.Agent`, `CurrentUrlCollection`, etc.).

See [test-class.md](test-class.md) for what a real test class looks like.

## IScopeContext — the central runtime context

Defined in `Bolt.Automation.Common/Context/IScopeContext.cs`. The two patterns you use most:

```csharp
// Property accessors (compile-time safe via Expression<Func<TestContextData, T>>)
ScopeContext.Set(ctx => ctx.FrontEnd, FrontEndType.Interview);
ScopeContext.Set(ctx => ctx.CurrentUser, user);
var tenant = ScopeContext.Get(ctx => ctx.CurrentTenant);

// String-keyed test values (for ad-hoc state)
ScopeContext.SetTestValue("MyKey", "value");
var v = ScopeContext.GetTestValue("MyKey");
```

`TestContextData` (the strongly-typed core) carries:
- `Tenant`, `Environment`, `FrontEnd`, `Lob` — test configuration.
- `CurrentUser`, `CurrentUrl`, `UrlDataCollection` — runtime data.
- `QuoteId`, `FriendlyId`, `ExternalId`, `ApplicantId` — API identifiers.
- `Errors`, `Metadata` — collections for custom test state.

URL resolution (typical TC pattern):
```csharp
var loginUrl = ScopeContext.Data.UrlDataCollection.FrontEnd?.LoginUrl
    ?? throw new TestSetupException("KLX Staging LoginUrl not configured");
```

## Field registry — quick concept

Each FrontEnd has its own static field registry — e.g. `FieldRegistryInterview.Fields` (a `Dictionary<string, UIElement>`) decorated with `[FieldRegistry]`. `FieldRegistryProvider` discovers all such dictionaries by reflection and exposes `GetRegistry(FrontEndType)`. A `UIElement` knows its locator strategy, locator string(s), `UIFieldType`, `DefaultValue`, and which page types it belongs to (`Pages = [typeof(Product_StartPage)]`).

Field iteration on a page goes through `MergeDataManager.GetSmartFormData(pageType, userInput)` → returns only the fields tagged for that page, with user-provided values or `DefaultValue`. `Page.FillRelevantFields` then dispatches each field via `IPageHelper.InteractWithElement` using the `UIFieldType`-derived `ElementAction`.

Full mechanics in [field-registry.md](field-registry.md). Locator recipes per `UIFieldType` in [../recipes/locator-recipes.md](../recipes/locator-recipes.md).

## Flow concept

A **flow** is an ordered list of page types representing a TC walk. Defined in `Projects/Interview/Flows/Flows.cs` (and corresponding D2C/HQXAgent flow files). Each flow:

```csharp
[FlowInitializer(flowType: FlowType.InterviewHO3Flow)]
public static FlowsHelpers InterviewHO3Flow() => CreateInterviewFlow(
    FlowType.InterviewHO3Flow,
    new List<Type> { typeof(Product_StartPage), ..., typeof(Product_ResultsPage) },
    FlowDefaultsProvider.GetFlowDefaults(FrontEndType.Interview, LobType.Home)
);
```

`PlaywrightExecutor.Execute<TStart, TEnd>(...)` walks the flow's pages, calling `FillForm()` then `ClickContinue()` on each, until `TEnd`. `ExecuteToPage<TEnd>(currentPage, ...)` resumes from a page you've already navigated to manually.

Full mechanics in [flows-executor.md](flows-executor.md).

## Test class shape

Tests inherit `UITestBase` (UI) or `TestBase` (API-only). Attributes drive routing:

- `[Tenant(Tenant.X)]` → resolves user/URL data via `(Tenant, Environment)` tuple.
- `[RunIn(Environment.X)]` → gates execution; tests skip with `Assert.Inconclusive` when running in a different env.
- `[Author(Author.Viktor)]`, `[TestCaseId(N)]`, `[Category("KLX")]` etc. → reporting/categorization.

The test method is small: log in, get to the start page of the flow, call `Executor.Execute<…>` or `ExecuteToPage<…>`, assert the final state.

Full skeleton in [test-class.md](test-class.md).

## Tenants

`Tenant` enum values you'll see most often: `BOLTAG`, `KRAFTLAKEX`, `USAA`, `UNIFY`, `COMPARION`, `LIBERTYX`, `PROGRESSIVEPL`, `BOLTACCESS`. Each has different login mechanics (password vs SSO), different field schemes (KLX class collapsing), and different popup behaviors. See [../domain/partners/INDEX.md](../domain/partners/INDEX.md) for the data side and the per-partner pages for UI quirks.

## When *not* to start from this skill

The skill is for **TC-driven automation authoring**. Skip it (or stop and ask) when:

- The user wants infrastructure changes (DI, configuration, runners).
- The user wants to *debug* an existing test rather than author a new one.
- The TC is for an external system that doesn't have a FrontEnd in nexus yet.
- The TC describes manual exploratory testing, not a deterministic flow.

For ordinary "fix this test"/"add this assertion" work, just edit directly — no skill needed.
