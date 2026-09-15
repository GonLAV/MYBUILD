---
topic: framework:accessibility
summary: axe-core scanning, keyboard traversal and screen-state enumeration — how accessibility coverage works and why a screen is not one scannable unit.
status: ready
---

# Accessibility testing

> **When to read:** adding accessibility coverage to a front-end, triaging an accessibility failure, or deciding whether a WCAG concern is automatable at all.

## What exists

`Bolt.Automation.FrontEnds/PlaywrightBase/Accessibility/` — front-end agnostic:

| Type | Does |
|---|---|
| `IAccessibilityScanner` / `AccessibilityScanner` | Wraps `Deque.AxeCore.Playwright`'s `RunAxe`. Scans an `IPage` or an `ILocator`. |
| `AccessibilityScanOptions` | WCAG tag filter (2.1 AA by default), disabled rules, iframe toggle. |
| `AccessibilityScanResult` / `AccessibilityViolation` | Framework-owned result types, so tests and reports never reference axe types. |
| `AccessibilityFindings` | Accumulates results across a walk; produces the JSON artifact and the assertion message. |
| `AccessibilityBaselineStore` | Known, bug-tracked violations that must not fail the gate. |
| `ConditionalRevealMap` | Derives value-gated reveals from any field registry. |
| `KeyboardTraversalHelper` | Tabs a screen; reports focus order, traps, missing focus indicators, and focusable content inside `aria-hidden`. |

Registered via `AddAccessibilityServices()` from `AddFrontEndServices` — scoped, like `IBrowserManager`.

Per-front-end state inventories live beside the project, e.g.
`Projects/HQXConsumer/Services/Accessibility/HQXConsumerAccessibilityStates.cs`.

## The coverage unit is a screen *state*, not a screen

One scan per screen only checks the default prefilled rendering. It misses every collapsed
section and every conditional question — which is most of the form surface. Two kinds of hidden
content, both already modelled in the framework:

**Collapsed sections.** `HQXConsumer_OverviewPage.ExpandSectionAsync(section)` toggles
`button.section-toggle` and waits on `aria-expanded='true'`. Expanding changes no data, so these
states are safe to sweep inside a flow walk.

**Conditional child questions.** `UIElement.DependsOn` + `DependsOnValue` is a machine-readable
map of every reveal. `ConditionalRevealMap.ForPage(registry, pageType)` groups it into
(parent, value, children) triples — **never hand-maintain the list**; a new conditional question
in the registry is picked up automatically.

Two rules that are easy to get wrong:

- **An empty `DependsOnValue` is not a gate.** `FormDataHelper.ShouldFillField` fills those
  unconditionally — the dependency exists only to order the fill. Only non-empty values are real
  reveal states. `ConditionalRevealMap` filters on this.
- **Driving a reveal mutates form data, and restoring the parent does not undo it.** Measured on
  QA: after driving and restoring the Details reveals, `ClickContinue` no longer navigated and the
  walk died on `/details`. Sweep reveals in a **dedicated test** that does not need to reach Rates.

### Known limits of the reveal map

`ConditionalRevealMap` is a flat, single-level derivation. Two consequences, both observed:

- **Dependency chains are not modelled.** `DwellingUsage=Rental` reveals a child, but `DwellingUsage`
  itself only renders when `IsPrimaryResidence=false`. Driving the leaf without its ancestor fails
  with "locator not visible". Chained reveals must be driven parent-first by hand.
- **`Pages` tags the child, not the parent.** A reveal surfaces on whatever page its *child* is
  registered against, which is not always where the *parent control* lives — `SingleFamilyHome`
  comes back for the Details page but its control is not on Details. Verify the parent is present
  before driving.

## Traps

1. **Report-only mode uploads nothing unless the test asks it to.** `UITestBase.CaptureArtifactsAsync`
   early-returns unless the test failed, so a passing scan produces no artifact. Call
   `UploadArtifactBytesAsync(..., ArtifactType.AccessibilityReport)` explicitly.
2. **ARIA snapshots must be scoped.** Quote numbers, premiums and carrier names change per run.
   Scope to `form.stage` (what `GetQuestionLabelsAsync` already targets) and normalise dynamic text,
   or every run fails.
3. **`perPageAction` skips the end page.** `FlowExecutionService` fires it for `[start, end)`. Add a
   trailing scan for the terminal screen — `CCPATests` does the same for its link assertions.
4. **`Execute<TStart,TEnd>` has no `perPageAction`** — only `ExecuteToPage` does. Create the first
   page yourself and use `ExecuteToPage`.
5. **Never wait for `NetworkIdle` before scanning.** Quantum Metric beacons mean these pages never
   go idle.
6. **The FieldRegistry cannot express accessibility queries.** Real usage is overwhelmingly XPath and
   CSS with zero `LocatorType.Role`, and the `Role` arm cannot carry an accessible name. Accessibility
   checks work off the raw `IPage` and axe.
7. **Waiting on a reveal:** `ElementExists` reports *attached*, not visible, and hidden wrappers stay
   attached. Wait on the child's registry locator with `waitForVisibility: true`.
8. **iframes are off by default.** axe aggregates cross-frame results by opening a blank page, which
   desynchronises `BrowserManager`'s tab tracking. Only enable where a screen genuinely needs it.
9. **Keyboard trap detection needs a unique element identity.** A `tag#id[name]` signature is
   identical for every unnamed link, so three consecutive links read as a trap. `FocusedElement.Path`
   carries a computed DOM path for this reason — do not compare on tag or id.
10. **`perPageAction` cannot measure focus on arrival.** It fires *after* `FillForm`, so focus is
    wherever the last field interaction left it. Use a `BeforeFillForm` page callback, and exclude
    the flow's entry page — focus on body there is ordinary page load, not a lost transition.

## Measured baseline — HQXShortFlow, QA, 2026-08-07

First full sweep: 9 states, 12 violations, **4 distinct rules** — a far smaller inventory than a
greenfield scan usually returns.

| Rule | Impact | WCAG | Root cause |
|---|---|---|---|
| `meta-viewport` | Moderate ×9 | 1.4.4 | `maximum-scale=1, user-scalable=no` in the app shell — one tag, every screen |
| `color-contrast` | Serious | 1.4.3 | Overview personal-info: `LastName` label/input, `DateOfBirth` label |
| `aria-command-name` | Serious | 4.1.2 | Owner: `<div role="button" tabindex="0">` datepicker icon with no accessible name |
| `autocomplete-valid` | Serious | 1.3.5 | Owner: same datepicker, `autocomplete="nope"` |

Three root causes, not twelve. The last two are the same control.

**Hypothesis disproven:** focusable content inside `aria-hidden` was *not* found. The consumer app
pairs `aria-hidden` with `inert`, which correctly removes hidden questions from the tab order.
Keyboard traversal reached 75–78 controls per screen with no trap.

**Found by keyboard testing, invisible to axe:** advancing the interview leaves focus on
`<body>` on **Details, Discounts and Owner** — every screen reached by a transition. A screen-reader
user presses Continue and is silently returned to the top of the document with no announcement that
the step changed. `PGR_HQX2_Accessibility_Focus_Moves_On_Page_Transition` is red by design until
this is fixed.

## Gate policy

Land report-only first, then gate. Every muted violation lives in a reviewable JSON baseline keyed
by **(screen, state, ruleId)** and **must carry an ADO bug id** — `AccessibilityBaselineStore.Load`
throws if one does not. Keying on screen alone would silently mute states nobody triaged; `"*"` is
available as an explicit, diff-visible state wildcard.

## What automation cannot cover

Do not let these be counted as covered:

- What a screen reader actually announces. Automation sees the tree, not the speech.
- Whether an accessible name is *meaningful* — `alt="image1"` passes every rule.
- Whether focus order is *logical*. Reachability and traps are checkable; sense is not.
- Plain language, cognitive load, error-message helpfulness.
- Whether a colour-only cue has a sensible non-colour equivalent.

Automated rule scanning catches roughly a third to a half of real WCAG issues. The point of the
suite is to take that portion off the manual QA permanently, not to replace manual auditing.

## Cross-references

- [field-registry.md](field-registry.md) — `DependsOn` / `Pages` semantics the reveal map reads.
- [flows-executor.md](flows-executor.md) — the `perPageAction` hook.
- [../philosophy/tests-stay-clean.md](../philosophy/tests-stay-clean.md) — helpers gather, tests assert.
