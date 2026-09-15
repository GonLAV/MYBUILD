---
topic: domain:product-areas
summary: Product areas — ADBX vs Interview vs D2C vs Partner Portal — which FrontEnd, which flows, which tenants.
status: ready
---

# Product / feature areas

> **When to read:** Phase 1 (matching `feature` from a TC to a FrontEnd) and Phase 2 (deciding which `FlowType` covers the TC's pages).

The `feature` field on a TC names the *product area* — the user-facing flow the TC validates. Each feature maps to one or more `FrontEndType` values + a set of `FlowType` candidates. This file is the lookup.

## Feature → FrontEnd / Flow map

### Interview (12 TCs in sample)

The agent-facing guided quote interview. Multi-page form with explicit page-by-page navigation (Start → Markets → Vehicle → Operator → Policy → Applicant → Results, or LOB-specific variants).

- **FrontEndType:** `Interview`
- **Flow inventory:** `InterviewHO3Flow`, `InterviewHO4Flow`, `InterviewAutoFlow`, `InterviewBundleFlow`, `InterviewFloodFlow`, `InterviewWCFlow`, `InterviewMotorcycleFlow`, `InterviewCLAutoFlow`, `InterviewBOPFlow`, `InterviewGLFlow` (see `Bolt.Automation.FrontEnds/Projects/Interview/Flows/Flows.cs`).
- **Sub-areas observed in titles:** `Interview V3` (modern vs legacy), `CL` (Commercial Lines), specific page names (`Market availability page`, `Result page`, `Application forms`).
- **Cross-tenant:** Used by every partner that has agent flows — Bolt AG, Kraftlake, Unify, Progressive, USAA.
- **Real TCs:**
  - **240782** (KLX CL Auto) — old interview, full E2E to result page.
  - **236743** (Bolt AG CL Auto) — 2 vehicles + 2 drivers E2E.
  - **236379** (Bolt AG CL BOP) — Acord forms on result page.
  - **235414** (Unify WC) — monopolistic-state market availability message.
  - **241435** (Unify Interview) — second floor material check.
  - **238828** (Progressive Pets) — adding "Cane Corso" to dog breed list.
  - **242142** (Progressive HO6) — RoofResponsible enforced when Condominium selected.
  - **235615** (KLX Personal Home) — application-forms result-page E2E.
  - **237437** (KraftlakeX CL BOP) — Markets results / CL_offline page / Offline request.
- **Patterns:** E2E full-flow walks, UI/UX validations of single fields, page-readiness checks, result-page assertion (rates appear, forms downloadable).
- **Phase 2 hint:** `feature=Interview` + `partner=KRAFTLAKEX` + `LOB=CL Auto` → `FlowType.InterviewCLAutoFlow`. Map other combinations to flows by inspecting `Flows.cs` directly.

### D2C (5 TCs in sample)

Direct-to-consumer self-service quote flow. Different page set from Interview; carrier-specific OLB variants; ends at payment + e-signature.

- **FrontEndType:** `D2C`
- **Flow inventory:** `D2CAutoFlow`, `D2CHomeFlow`, `D2CRentersFlow`, `D2CCondoFlow`, `D2CCondoAutoFlow`, `D2CAutoHomeFlow`, `D2CHomeAutoFlow`, `PetsFlow`, `SafecoAutoFQFlow`, `USAAAutoFQFlow`, `StillwaterHomeFQFlow`, `LakeviewHomeFlow`, `LemonadeFQFlow` (see `Bolt.Automation.FrontEnds/Projects/D2C/Flows/`).
- **Sub-areas in titles:** `D2C Safeco`, `D2C Bristol West`, `HQX 2.0` (Progressive's modern consumer UI generation).
- **Real TCs:**
  - **162448** (USAA D2C Auto) — retrieval process and going back without losing fields.
  - **237026** (USAA D2C Bristol West) — E2E with payment (DocuSign + 4111111111111111 test card).
  - **235870** (USAA D2C Bristol West) — adding OLB flow, e-sign step.
  - **241340** (PGR HQX 2.0 HO6) — Condo Overview section validation.
  - **242142** (PGR HQX 2.0 HO6) — RoofResponsible field enforcement.
- **Patterns:** Quote start → rates → carrier-specific FQ pages → payment → e-sign. Heavy feature-flag testing for partner-specific carrier behavior (Plymouth Rock, Bristol West, Safeco).
- **Phase 2 hint:** Carrier-specific D2C flows have their own FlowType (`SafecoAutoFQFlow`, `USAAAutoFQFlow`). Reuse before inventing.

### GetQuoteAPI (5 TCs in sample)

Backend quote-creation API. Doesn't have its own FrontEnd — TCs typically combine an API call with subsequent UI verification (account in ADBX, quote in Interview, prefill data in HQX).

- **FrontEndType:** Mixed — usually starts with API call (no UI), then asserts UI state via `ADBX` or `Interview`.
- **Flow involvement:** No standalone `GetQuoteAPIFlow`. The TC body typically: (1) calls `IGetQuoteApi` from `Bolt.Automation.ApiClients`, (2) opens ADBX or Interview by URL with the resulting account/quote ID, (3) asserts the UI-side data.
- **Sub-areas in titles:** `GetQuoteAPI`, `GQ`, `GQ → ADBX`, `GQ → MPQ3`, `GetQuteAPI` (typo seen in 145915).
- **Real TCs:**
  - **145915** (Comparion GQ PL Auto) — Fenris prefill, validates VIN decode + Splunk events.
  - **212329** (USAA GQ SSO Agents) — quote creation with agent key, redirects to Interview Start.
  - **239957** (Progressive GQ) — `dynamic-custom-fields` flag: unknown custom fields rejected with 422 vs silently accepted.
  - **240777** (Progressive GQ) — Plymouth Rock CovMod flag re-enable.
  - **239923** (Unify GQ CL Auto) — Progressive DHUB invalid-VIN warning vs error behavior.
- **Patterns:** Test author writes the API request payload as test data, calls via `RefitApiServiceLocator.GetService<IGetQuoteApi>()`, then either asserts API response shape or follows up with UI navigation.
- **Phase 2 hint:** API-first TCs typically inherit `TestBase` (not `UITestBase`), unless they end with UI verification.

### ADBX (7 TCs in sample)

Agent dashboard / back-office. Cases, leads, accounts, defaults, email templates, user permissions.

- **FrontEndType:** `ADBX` (and `CaseManager` for case-specific work).
- **Sub-areas in titles:** `ADBX`, `Default tool`, `Defaults table`, `Email template`, `Sales case`, `CMAPI`, `ADBX to CM`.
- **Real TCs:**
  - **234237** (Comparion ADBX) — Defaults table CRUD.
  - **236062** (Bolt AG ADBX) — Email template + send with attachment.
  - **241459** (Bolt AG ADBX) — Default tool / enable additional users CRUD permissions.
  - **197386** (KLX ADBX → CM) — Sales case create / PL bind request / dummy quote / message.
  - **198617** (KLX CMAPI) — Sales case → KLX login → ADBX → Lead page.
  - **188906** (KLX) — Create note for sales case creation without messages.
  - **194175** (KL/BoltAccess/Unify) — Remove empty note as message to CM.
- **Patterns:** Login → navigate to a specific tab/tool → CRUD operation → assert UI state. Heavy use of `AdbxTestHelper` for login + account opening (see [framework/test-class.md](../framework/test-class.md)).
- **Phase 2 hint:** ADBX TCs always start in `FrontEndType.ADBX`. If they cross into Interview (e.g. account → New Quote), switch FrontEnd before instantiating Interview pages.

### STS & Login (2 TCs in sample)

Security Token Service / SSO flows. Drives login mechanics across tenants.

- **FrontEndType:** `STS` (initial), then transitions to ADBX or Interview.
- **Real TCs:**
  - **200974** (Comparion SSO) — Interview Adjustment, generic kickout error 109 page.
  - **212329** (USAA SSO Agents) — SSO into the Interview.
- **Patterns:** Validate redirect token handling, error pages, agent vs consumer SSO. The login implementation lives in `STS_LoginPage.Login()` (see [framework/test-class.md](../framework/test-class.md)).

### Provision (1 TC in sample)

Carrier credential / wholesaler-onboarding API. Backend-only.

- **FrontEndType:** None (API).
- **Real TC:** **235402** — Platform API credentials API: PATCH/GET wholesaler credentials.
- **Patterns:** API-only via Refit clients. Inherits `TestBase`, not `UITestBase`.

### CRM / Leads (sub-area of ADBX)

Tagged with `CRM; lead` in titles or tags. Sub-area of ADBX covering the lead-management workflow (lead creation, queue management, TCPA consent, lead lifecycle).

- **FrontEndType:** `ADBX`.
- **Real TCs in sample:** Lead-grid TCs (242194-242211) — most in `Design` state. The sample is heavy on this area in May 2026 — recent feature push.
- **Patterns:** Lead grid filtering, queue counts, status transitions, TCPA consent via SMS, lead match + quote association.
- **Phase 2 hint:** When a CRM/Leads TC is automated, expect UserDataStore to need `Agent` plus a CRM-specific role (some TCs require admin access to lead pools).

### Payment (1 TC in sample)

Payment-flow validation. Smoke / canary on the payment site.

- **Real TC:** **241722** — "Product | Payment | Production | Boltag | Testing payment site is up." Lightweight Sanity TC.

### Cross-cutting `Product | …` titles

Many TCs use `Product` as the title prefix when the work spans features (e.g. "Product | D2C | Adding Bristol West OLB flow to the Auto D2C | E-sign"). The `feature` field still tells you the dominant feature; treat the title prefix `Product` as "this is platform-wide cross-cutting work."

## Mapping a TC's `feature` to your test class

Use this table in Phase 3 when picking which folder to put the new test in:

| `feature` value | Test class folder | Base | FrontEnd |
|---|---|---|---|
| Interview | `Bolt.Automation.Tests/Tests/<Tenant>/<...>InterviewTests.cs` | `UITestBase` | `Interview` |
| D2C | `Bolt.Automation.Tests/Tests/<Tenant>/D2C<...>Tests.cs` (or `Bolt.Automation.Tests/Tests/D2C/`) | `UITestBase` | `D2C` |
| ADBX | `Bolt.Automation.Tests/Tests/<Tenant>/<...>AdbxTests.cs` (or `Bolt.Automation.Tests/Tests/ADBX/`) | `UITestBase` | `ADBX` |
| GetQuoteAPI | `Bolt.Automation.Tests/Tests/<Tenant>/<...>ApiTests.cs` | `TestBase` (API-only) or `UITestBase` if UI verification follows | varies |
| STS & Login / SSO | `Bolt.Automation.Tests/Tests/<Tenant>/SsoTests.cs` | `UITestBase` | `STS` → `ADBX`/`Interview` |
| Provision | `Bolt.Automation.Tests/Tests/Platform/` | `TestBase` | none |
| CRM (lead tags) | `Bolt.Automation.Tests/Tests/<Tenant>/CrmTests.cs` | `UITestBase` | `ADBX` |

## Anti-patterns

- **Picking a flow because the title includes a page name.** "Result page" appears in many TCs but doesn't mean a new flow — the existing flow's `Product_ResultsPage` is the terminus. Use the LOB + tenant to choose the flow.
- **Creating a new feature folder when the TC is cross-cutting.** A `Product | D2C | ...` TC still belongs in D2C — put it under that tenant's D2C folder.
- **Conflating `Interview` (agent) and `D2C` (consumer)** when both involve the same LOB. The page sequence and registry differ — picking the wrong feature means picking the wrong FlowType.
