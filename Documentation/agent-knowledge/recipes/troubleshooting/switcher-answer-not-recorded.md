---
topic: troubleshooting:switcher-answer-not-recorded
summary: Switcher/radio click reports success but the answer is not recorded; Next stays disabled and the control is not ng-invalid.
status: ready
---

# Switcher clicked, answer not recorded

**Symptom:** The log shows `UI Action [Click]` on a `label.switcher-wrapper` with no error, the flow then
fails to leave the page, and the failure reads *"Next/Continue button Could not find or click any continue
button"*. In the captured page source the Next button **exists but is `disabled="true"`**, and the
unanswered question is **not** `ng-invalid`.

**Likely cause:** The click landed on a subtree Angular re-rendered underneath it. `generateIdentifier`
folds `disabled`/`visible` into `attributes.formId`, which is the `@for` track key, so any rule flip
destroys and recreates the control. `ClickAsync` reports success whether or not the option took, so the
question is silently left unanswered.

**Discriminate before fixing.** In the same page source, list which controls hold a value:

- **Dropdowns hold values, switchers do not** → the interaction did not take. That is this recipe.
- **Some switchers hold values, others were cleared** → suspect the app clearing answers on a rule
  evaluation, not the click.

A clean example (TC 237634, `CL_CommercialAutoCoverages`): 9 of 9 ng-selects held their committed value —
including two set *after* the switchers — while 0 of 7 switcher groups did. The split follows control
type, so it is the interaction, not the store.

**Fix, in order. Check the field type first — it is the more common cause.**

**1. Is the entry `FieldType = Button`?** If it is `Radio`, that is the bug, not the race.
`Radio` resolves to `ElementAction.Check`, and
[../../framework/ui-field-types.md](../../framework/ui-field-types.md) has prescribed `Button`
for switcher labels all along. On 2026-09-03 a sweep found **39 entries in
`FieldRegistryInterview` still typed `Radio` on a switcher locator** and converted them all.
Two shapes, two different failures:

- `label[...]` — does *not* throw. Playwright retargets a `<label>` to its associated control,
  so it resolves to the hidden input, and then `check()` skips the click when it believes the
  control is already in the wanted state. A lost click becomes a false pass.
- `label[...]/span` — throws. A `<span>` is not a label, so it gets no retargeting, and
  `check()` requires a real checkbox or radio input.

Either way `FillForm` swallows the outcome, so both are silent. Find any that come back:

```bash
# parse line-by-line on "[Name] = new UIElement" — a regex spanning entries mis-pairs
# names with bodies. Flag any entry with UIFieldType.Radio AND a label[contains locator
# AND no //input.
```

**2. If the type is already `Button`, it is the re-render race.** `ElementInteractionHelper`
verifies every `Click` that resolves exactly one `input[type=radio]` under the clicked element
(or under its nearest ancestor `<label>`): it polls the checked state for a second, then warns,
repeats the click that worked, and throws a `PageElementException` if the option still is not
selected. Look for these lines:

```
Clicked [<locator>] and the option is selected          <- verified, took first time
Clicked [<locator>] but the option did not take - retrying once.   <- the race, caught
```

Note the throw does **not** fail a run reached through `FillForm`, which swallows it
(`IgnoreIfNotFound = true`, then `ProcessField`'s catch). There the log line is the deliverable —
it names the control before the disabled Next button does.

**Do not "fix" this by pointing `FieldType.Radio` at the input.** Measured against the real markup
(`scratchpad/pwtest`, and see [../../framework/ui-field-types.md](../../framework/ui-field-types.md)):

| Strategy | Result |
|---|---|
| `click(label)` — what the registry does | answered |
| `check(label)` — retargets and verifies | answered |
| `check(input)` | **NOT answered — 30s timeout** |
| `check(input, force)` / `click(input, force)` / `dispatchEvent` / `click(span)` | answered |

The input is `width:1px; height:1px; clip-path: inset(50%)`, so Playwright counts it as *visible* (1×1
bounding box) but the hit-target check loses to the covering sibling `<span>`. `check()` on the label
does work and verifies, but it skips the click when it believes the control is already in the wanted
state — which would turn this exact failure into a false pass. Click, then verify.

**A zero count is not a pass.** If a run logs no `and the option is selected` lines at all, the test did
not reach those fields — it does not mean there was nothing to verify. The same goes for a field whose
locator wraps a yes/no *pair* rather than one option: the verification skips it deliberately (nothing
identifies which radio the click meant), so it stays as silent as it was before ADR-006.

**Cross-references:** [../../framework/ui-field-types.md](../../framework/ui-field-types.md),
[../../philosophy/design-decisions.md](../../philosophy/design-decisions.md) ADR-006,
[reading-captured-page-source.md](reading-captured-page-source.md),
[disabled-button-click.md](disabled-button-click.md).
