---
topic: philosophy:design-decisions
summary: ADR placeholder — records the why behind major framework decisions (additive, append-only).
status: ready
---

# Design decisions (ADR log)

This file is an **append-only architecture decision record** for the framework. Each entry records context (what was happening), decision (what we picked), and consequences (what we got into when we picked it). When a future change conflicts with a decision here, the conversation starts with "let's re-open ADR-NNN" — not with "let's just override it."

Format (Michael Nygard ADR template):

```markdown
## ADR-NNN — <decision title>

**Status:** Proposed | Accepted | Superseded by ADR-NNN | Deprecated

**Date:** YYYY-MM-DD

**Context:**
What was the situation? What forces were at play (technical, organizational, business)?

**Decision:**
What did we decide to do? Be specific — name the alternatives we rejected and why.

**Consequences:**
What does choosing this give us? What does it cost us? What follow-up work does it create?
```

Decisions are numbered in chronological order. When a decision is superseded, leave the original entry intact (don't rewrite history) and add a new entry that supersedes it.

---

## ADR-001 — Sparse, page-tagged FieldRegistry

**Status:** Accepted

**Date:** 2025-09 (initial framework design)

**Context:**
Each tenant's quote interview has hundreds of fields. Tests written against full-field dictionaries (one entry per field, even when the value matches the default) grew unreviewable — 80-line dictionaries with 3 meaningful entries hidden among defaults.

**Decision:**
The `FieldRegistry` is the source of truth for `DefaultValue` and `Pages` tagging. Tests' `formData` dictionaries express only divergences from the registry's defaults. `MergeDataManager.GetSmartFormData` layers user input over defaults and filters by `Pages?.Contains(pageType)`.

Empty-string suppression (set a field to `""` in user input) is the override mechanism for "I handled this field out-of-band, don't iterate."

**Consequences:**
+ Tests document intent — every key is a deliberate divergence.
+ Adding a new tenant means writing tenant-specific `DefaultValue` entries; existing tests don't need to know.
+ The Pages tagging gives the registry per-page scoping without a separate namespace.
− Authors who don't know the registry holds defaults will re-supply them "to be safe"; reviewer enforcement is required.
− Renaming a `FieldNames` constant doesn't propagate to `TestDataProvider`'s string-keyed dictionaries (they reference the constant by `nameof()` value, not by symbol).

See: [sparse-dictionaries.md](sparse-dictionaries.md).

---

## ADR-002 — Logging lives in implementation files, not test methods

**Status:** Accepted

**Date:** 2025-12 (refined; original policy earlier)

**Context:**
Tests authored across multiple sprints accumulated narrative logging (`_logger.Info("clicking continue")` immediately before `await page.ClickContinue()`). Step reports duplicated entries; test methods became 200+ lines of mixed orchestration and narration; new tests inherited the pattern.

**Decision:**
Page objects, API clients, and helpers own their logging. Test methods use `_logger.ExecuteStepAsync(name, action, expectedOutcome)` for structural phases and, where an assertion's actual value would otherwise go unreported, `_logger.LogDataValidation(...)` before it — nothing else. The operational policy lives at `Documentation/AI-Agent-Logging-Instructions.md`.

*Amended:* `LogDataValidation` is not required when the actual value already reaches the report — via the assertion's own failure message or via the helper that gathered it. Duplicating it adds noise, and pushing the value into the gathering helper is the same principle as the rest of this decision. See [../framework/logging.md](../framework/logging.md), "When it is not needed".

**Consequences:**
+ The MongoDB step report is reliable — no duplicate entries.
+ New tests get good logging for free (they use existing page objects).
+ Logging refactors happen in one place per concern.
− Page objects must be designed to log meaningfully — the method names need to describe business actions.
− Drive-by logging additions across the codebase are explicitly disallowed; reviewer enforcement is required.

See: [logging-where-work-happens.md](logging-where-work-happens.md).

---

## ADR-003 — Fluent page objects; no Playwright at the test layer

**Status:** Accepted

**Date:** 2026-02

**Context:**
Earlier tests used `Page.Locator` directly. As tenant schemes diverged (KLX class collapsing, Progressive HQX 2.0), test-layer locators became per-tenant per-test code duplication. Reviewers couldn't enforce the no-XPath rule because locators were everywhere.

**Decision:**
Page-object methods name user actions, not DOM operations. The full Playwright surface is hidden behind `FillForm`, `ClickContinue`, `SelectLob`, `IsXxxPopupExists`, etc. Tests never instantiate locators. Override patterns (A–G) extend the fluent surface where defaults don't suffice; each override is named for the business action it accomplishes.

**Consequences:**
+ The test layer is tenant-agnostic — `await page.FillForm(formData)` works across BOLTAG, KLX, USAA without changes.
+ The XPath ban at the test layer is enforceable; XPath inside page objects is allowed and audited.
+ When a tenant changes its DOM, only the page object changes — no test edits.
− Page-object code is denser than test code; the abstraction has up-front cost.
− Override-pattern proliferation requires periodic consolidation (e.g. pattern A historical fallback was retired when `DependsOn` matured).

See: [fluent-page-objects.md](fluent-page-objects.md).

---

## ADR-004 — TestBase vs UITestBase split

**Status:** Accepted

**Date:** 2026-03

**Context:**
API-only tests don't need Playwright, but inheriting a single `TestBase` that always initializes the browser made every test pay the cost.

**Decision:**
`TestBase` provides DI scope, `IScopeContext`, `IAutomationLogger`, and `TestContextAccessor` for all tests. `UITestBase` extends it with `BrowserManager`, `PageFactory`, `_pageHelper`, and `Executor`. API-only tests inherit `TestBase` directly; UI tests inherit `UITestBase`.

**Consequences:**
+ API tests run faster (no browser init).
+ The base-class choice is a documentation point — readers can tell at a glance whether a test is API-only or UI.
− Two base classes to maintain. The split must stay narrow: cross-cutting concerns go in `TestBase`; only UI-specific helpers go in `UITestBase`.

See: [../framework/test-class.md](../framework/test-class.md).

---

## ADR-005 — Agent CLI lives in-solution as `Bolt.Automation.AgentTools`

**Status:** Accepted

**Date:** 2026-05

**Context:**
The AI-agent extension (four skills + KB) needs deterministic tools beyond what AskUserQuestion/Bash/Read can provide — TC fetching with caching, page-source correlation, flow reflection, persistent browser sessions. The alternative — putting these in a sibling repo — would force agents to context-switch between the nexus solution and the toolchain repo for every operation.

**Decision:**
Build the tools as an in-solution executable project (`Bolt.Automation.AgentTools`) that references `Common`, `Core`, `FrontEnds`, `ApiClients` directly. CLI is invoked via Bash from the agent (`dotnet run --project Bolt.Automation.AgentTools -- <noun> <verb>`). JSON output uses `JsonNamingPolicy.SnakeCaseLower` for predictable consumption.

The browser command (`browser navigate` / `pause` / `resume`) uses a long-lived host process with `HttpListener` on `127.0.0.1:5151` so the agent's Bash slot isn't tied up while a session is live.

**Consequences:**
+ The CLI consumes the same DI graph as the test framework — no version skew between `FlowType` enums or `FieldRegistry` shape.
+ Agents can reflect over the framework (`Bolt.Automation.FrontEnds.dll`) without a separate model file.
+ The pause/resume IPC works as designed (validated by the Phase 0 spike).
− One more project to build with the solution; build time increases marginally.
− The host process introduces a new failure mode (orphaned listeners). Lock file at `%TMP%\nexus-agent\host.lock` plus auto-respawn on connection-refused mitigates.

See: `.skill-explore/design.md` (architecture rationale), `.skill-explore/spike-pause-resume.md` (IPC PoC).

---

## ADR-006 — A UI fill verifies its own post-condition

**Status:** Accepted

**Date:** 2026-09-01

**Context:**
Playwright actions report success when the *gesture* succeeded, not when the *application accepted the
value*. Two fills in this framework relied on that distinction and lost:

- `SearchSelectDropdown` typed a term, clicked whatever the server returned first, and moved on. It could
  commit the wrong option — or none — and still pass. Once it logged what it committed, a WC scenario for
  a linen-supply business turned out to be selecting *"8215 - Feed, Hay, Or Grain Dealer"*.
- Switcher/radio options are clicked through their wrapping `<label>` because the input is visually
  hidden. `ClickAsync` cannot tell a click that answered the question from one that landed on a subtree
  Angular had just re-rendered. On TC 237634 three required questions reached the end of a page
  unanswered, and the only symptom was a disabled Next button 100+ seconds later.

Both failures are silent at the point of damage and expensive to diagnose from where they surface.
`FormDataHelper.ProcessField` swallows fill exceptions by design, so an unverified fill is indistinguishable
from a successful one.

**Decision:**
A fill helper that drives a control asserts the control took the value, close to the interaction.

- Prefer **act-then-verify** over Playwright's self-verifying `check()`. `check()` skips the click when it
  believes the control is already in the wanted state, so exactly the failure we are guarding against
  becomes a false pass. Rejected for that reason.
- Rejected "point `FieldType.Radio` at the now-visible hidden input": measured, `check()` on a
  `1px`/`clip-path` input times out after 30s because the hit-target check loses to the covering span.
- Verify only where the post-condition is unambiguous. Radios yes; a `Click` on a checkbox is a toggle,
  so "checked" is not its expected outcome, and it is left alone. And *which* radio has to be unambiguous
  too: a clicked element wrapping a whole yes/no pair does not say which one was aimed at, so that case
  is skipped rather than guessed at — picking the first would fail every "No" answer.
- Degrade to a log line, not a throw, when the control is not shaped the way we expect. An unfamiliar
  widget must not fail a fill that may well have worked.
- Poll for the post-condition, do not sample it. The model update trails the gesture, and the re-render
  this guards against lands in that same beat, so a single immediate read is both too early to confirm
  and too early to catch the defect. One second, then it is a lost answer and not a lost race.
- Log the verified path explicitly. The returned detail string only reaches the structured Mongo payload,
  so a plain TRX cannot otherwise tell "verified" from "never checked".

**Consequences:**
Silent wrong-value and lost-click failures become *diagnosed* ones at the point they happen, and the log
carries the committed value. A retry absorbs the transient case, and the warning that precedes it is the
evidence that separates a re-render race from an answer being cleared later.

Diagnosed, not necessarily fatal — and the difference is the calling path. A direct `InteractWithField`
sets `IgnoreIfNotFound = false`, so the throw fails the step. `FillForm` does not: it hardcodes
`IgnoreIfNotFound = true` and `ProcessField` catches what escapes, which is the same by-design swallow
named in the Context above. Since the registry-default switchers this was written for are filled only by
`FillForm`, the throw does not fail those runs — the `LogException` does the work, naming the control
before the run reaches the page's own signals (`RetryInvalidFieldsAsync`, then `ClickContinueButton`
listing the unsatisfied fields). Making a lost answer fail the run outright would mean escalating through
a channel the fill path honours, which is a change to the best-effort fill contract and is not in scope
here.

The cost is a shared-path change: two extra `CountAsync` round-trips on every `Click`. 18 of 96
`Button`-typed registry fields have label-shaped locators; the interviewv3 CL ones are covered by the CL
suites, the HQX/D2C yes-no fields are not. Those uncovered ones are why the ambiguity rule above is a
bail-out and not a best guess — a field whose locator wraps a radio it does not itself select is skipped,
so the worst case on an unexercised field is the behaviour it had before this ADR, not a new red.

A verified count of **zero** means the test never reached those fields — not that there was nothing to
verify. Read the count before concluding a run exercised the path.

---

## Adding a new ADR

1. Pick the next number (`ADR-NNN`).
2. Append a new section in chronological order — don't insert into the middle.
3. Be specific about *forces* — what was making the previous shape painful?
4. List the alternatives you rejected. The decision is more useful when readers can see the option space.
5. Reference the ADR from the relevant philosophy/mechanics files. Cross-links make the decision discoverable from the place it bites.

If an ADR is later superseded, change its `Status` line to `Superseded by ADR-NNN` and add a new ADR. **Never edit a previously-accepted ADR's content.** The history is the value.
