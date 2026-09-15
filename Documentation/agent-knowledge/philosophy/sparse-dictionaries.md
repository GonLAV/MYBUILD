---
topic: philosophy:sparse-dictionaries
summary: Why FieldRegistry is sparse and Pages-tagged — agent fills only what's relevant, defaults handle the rest.
status: ready
---

# Sparse dictionaries

The `FieldRegistry` doesn't know what every test wants. It knows what *most* tests want, encoded as `DefaultValue`. A specific test diverges from that baseline only where the scenario demands. The test's `formData` dictionary records exactly those divergences — and nothing else.

## The contract

The registry's `DefaultValue` is a contract between the framework and the tenant: "for an arbitrary test on this tenant, this is the value that produces a valid, representative quote." Most fields on most pages don't need test-specific values — the registry default is fine. `MergeDataManager.GetSmartFormData` does the layering: user input overrides default; missing keys take default; the page only sees fields it cares about.

The `Pages` tagging is the same pattern applied to *which* fields show up: a field belongs to the pages where it renders. The Field Registry isn't a flat namespace — it's a graph keyed on `(field, page)`. Untagged fields don't iterate. Wrong-page fields don't iterate.

Sparseness is the framework saying: "the test should only express what's different."

## Why this matters

When a test dictionary contains `[FirstName] = "AutoTest"` and the registry default is *also* `"AutoTest"`, three things go wrong:

1. **Intent is hidden.** A reader sees `[FirstName] = "AutoTest"` and assumes the test cares about that value. It doesn't — the default would have produced the same result. The signal-to-noise ratio of the dictionary drops with every redundant entry.
2. **The line silently rots.** Two months later, someone changes the registry default to `"Tester"`. The test still passes — but the test dictionary is now lying. It says the test wants `"AutoTest"`; the framework no longer agrees. Future readers can't tell whether the entry is intentional (the test author wanted to *resist* the new default) or vestigial (it's been wrong for two months and nobody noticed).
3. **The dictionary grows monotonically.** Authors who don't know what the registry holds re-supply every field "to be safe." Tests become 80-line dictionaries with three values that actually matter. Reviewers stop reading them.

The discipline: every key in `formData` is a deliberate divergence from the framework default. If you removed the key, the test would fail (or produce a meaningfully different scenario). If removing the key changes nothing, the key shouldn't be there.

## How the framework supports this

`MergeDataManager.GetSmartFormData(pageType, userInput)` reads the registry, filters by `Pages?.Contains(pageType)`, then merges `userInput` over `DefaultValue` per key. The merge is shallow and predictable:

- Key present in `userInput` with a non-empty value → that value.
- Key present in `userInput` with an empty string → suppressed (skipped entirely; default NOT injected).
- Key absent from `userInput` → `DefaultValue` (or empty string if no default).

Empty-string suppression is the dual operation: it lets a `FillForm` override say "I handled this field out-of-band, don't iterate it again" without un-tagging the registry entry (which would break other tests on the same page).

`DependsOn` + `DependsOnValue` add a third axis: a field only iterates if another field has a specific value. The registry models conditional flow shape declaratively — the agent doesn't need to write imperative ordering code.

### Filling a reveal-gated (parent → child) field via `FillForm`

A checkbox/radio that reveals a hidden child (e.g. a "farm animals or exotic pets" parent that reveals an "exotic pets" child, or a "utilities replaced" parent that reveals per-utility children) is the canonical case where the three axes combine. To drive it through a single `FillForm(overrides)` — rather than a bespoke page-stepping method with explicit `InteractWithField` calls and a manual reveal-wait — the registry must model it:

1. **Tag both parent and child to the page** (`Pages = [typeof(TPage)]`). A `Pages = []` field is invisible to `GetSmartFormData` — it filters by `Pages?.Contains(pageType)` *before* it ever looks at your override, so passing the field in `FillForm`'s dictionary silently does nothing (see the anti-pattern below).
2. **Keep the child's `DefaultValue = "false"`** (or its unchecked value) so a normal page fill doesn't select the knockout.
3. **Gate the child with `DependsOn = <parent>` / `DependsOnValue = "true"`.** This does two jobs at once: `AddWithDependencies` orders the parent *before* the child, and `ShouldFillField` *skips* the hidden child on any fill where the parent isn't set. Without the gate, tagging the child to the page would make every ordinary page fill poke a not-yet-rendered element.

With that in place, a normal `FillForm()` leaves the parent at its false default and skips the child; a test that wants the knockout passes `FillForm(new(){[Parent]="true",[Child]="true"})` and the framework fills parent-first, then the revealed child — no hardcoded locator, no manual wait, no custom method. The knockout *answers* stay in the test (a local `formData` dictionary); the reveal *mechanics* stay in the registry. Verify the reveal timing on the first live run, and re-run an existing test that fills the same page to confirm the new `Pages` tag didn't perturb it.

## The connection to fluent page objects

Sparse dictionaries only work because the framework *consumes intent*. `formData` is a vocabulary of (field name → meaningful value); the page object translates that into clicks and fills. If the page object exposed raw Playwright operations instead, the test would have to specify both the value AND the locator — there'd be nowhere for the registry to inject defaults from.

Sparse dictionaries and fluent page objects are the same principle applied to two different surfaces: the framework holds the mechanics; the test holds the meaning.

## Anti-patterns

- **Re-supplying defaults.** `[FirstName] = "AutoTest"` when the registry default is `"AutoTest"`. Remove it.
- **Setting a field that isn't tagged for the current page.** `MergeDataManager` filters by `Pages?.Contains(pageType)` before reading overrides, so an untagged (`Pages = []`) field passed in `FillForm`'s dictionary is silently dropped — the line is dead code, and the failure is invisible (the form just submits without your value). If you *need* to fill it via `FillForm`, tag it to the page and gate it (see "Filling a reveal-gated field" above); otherwise drive it explicitly by field name.
- **Using `DefaultValue` for per-test randomness.** `DefaultValue` is captured at static init, not per-test. Use inline `formData` for random emails / phones.
- **Setting `DefaultValue` to a tenant-invalid option.** If the dropdown doesn't have "Owned" on KLX, defaulting to "Owned" guarantees a validation failure. Either pick a tenant-valid default or leave `DefaultValue = null` and require the test to supply.

## Cross-references

- [../framework/field-registry.md](../framework/field-registry.md) — the mechanism (sparse dictionaries, Pages tagging, MergeDataManager).
- [../recipes/troubleshooting/default-reinjected.md](../recipes/troubleshooting/default-reinjected.md) — when the default fights you, suppress it via empty string.
- [tests-stay-clean.md](tests-stay-clean.md) — the dictionary stays clean for the same reason the test does.
