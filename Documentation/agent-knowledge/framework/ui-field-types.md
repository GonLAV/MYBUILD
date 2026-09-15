---
topic: framework:ui-field-types
summary: Taxonomy of UI field types (Input, Dropdown, NgSelect, Switcher, Checkbox) and registry strategy patterns.
status: ready
---

# UIFieldType taxonomy

> **When to read:** Phase 2 (classify each TC field), Phase 3 (write the registry entry).
>
> **Companion file:** [../recipes/locator-recipes.md](../recipes/locator-recipes.md) — per-control locator recipes (15 patterns).

## The `UIFieldType` → `ElementAction` mapping

Source: `UIElement.GetActionForFieldType(UIFieldType, string value)` — `Bolt.Automation.FrontEnds/FormData/Models/UIElement.cs`.

| `UIFieldType` | Resolved `ElementAction` | Notes |
|---|---|---|
| `Input` | `Fill` | Plain text input. |
| `Dropdown` | `Select` | ng-select / native select / styled. |
| `MultiDropdown` | `MultiSelect` | Multi-value ng-select. |
| `Radio` | `Check` | Strict Playwright `CheckAsync` — needs `<input type="radio">`. |
| `Checkbox` | `Check` if `bool.TryParse(value)==true` else `Uncheck` | Strict Playwright `CheckAsync` — needs `<input type="checkbox">`. |
| `DatePicker` | `Fill` | Fills the visible input; mat-datepicker calendars get bypassed. |
| `Button` | `Click` | Use this for switcher labels, list items, checkbox-wrappers — anything that's "click this exact element." |
| `Link` | `Click` | Same as Button mechanically; semantic only. |
| `SearchDropdown` | `SearchSelect` | Typeahead; expects `input[role='combobox']` inside the container. |

**Trap:** `ElementAction.Check` calls Playwright's `CheckAsync`, which **only works on real `<input type="checkbox|radio">` elements**. Many ng-select / styled controls render the input as hidden and overlay a `<label class="checkbox-wrapper">` for clicking. For those, use `FieldType = Button` with `Click` against the label, not `Checkbox`/`Radio` with `Check`. KLX cert is the canonical example.

**Second trap: `Click` asserts nothing.** `ClickAsync` returns successfully whether or not the control
took the value, so a click that lands on a subtree Angular has just re-rendered leaves the field
unanswered and the run none the wiser. `ElementInteractionHelper` therefore verifies any `Click` that
resolves **exactly one** `input[type=radio]` under the clicked element (or under its nearest ancestor
`<label>`): poll for a second, then warn, repeat the click that worked, and throw a `PageElementException`
if it still has not taken. Radios only — a `Click` on a checkbox is a toggle, so "checked" is not its
expected post-condition, and anything else passes through untouched. A clicked element wrapping a whole
yes/no *pair* is also passed through: nothing there says which radio the click was aiming at, and guessing
the first would fail every "No" answer. See ADR-006.

**Do not point `Radio` at a visually-hidden input**, even though Playwright now reports it as visible.
Measured against interviewv3's switcher markup (`width:1px; height:1px; clip-path: inset(50%)`):
`check()` aimed at the input **times out after 30s** — the 1×1 box is "visible" to Playwright, but the
hit-target check loses to the covering sibling `<span>`. `click(label)`, `check(label)`,
`click(span)`, `dispatchEvent`, and either forced variant all work. `check(label)` verifies too, but it
skips the click when it believes the control is already in the wanted state, which turns a lost click
into a false pass — hence click-then-verify. Recipe:
[../recipes/troubleshooting/switcher-answer-not-recorded.md](../recipes/troubleshooting/switcher-answer-not-recorded.md).

## `ElementInteractionOptions`

```csharp
public class ElementInteractionOptions {
    public string? Value { get; set; }
    public IReadOnlyList<string>? Values { get; set; }
    public bool IgnoreIfNotFound { get; set; } = true;   // soft-skip when locator misses
    public bool ClearField { get; set; }                  // clear input before fill
    public int Timeout { get; set; }
    public bool PressTab { get; set; } = true;            // tab off after fill (commit value)
    public bool ForceInteractionIfNotVisible { get; set; }
    public bool UseSequentialTyping { get; set; } = false;
}
```

Default `IgnoreIfNotFound = true` means a missing locator is not fatal — it just logs and moves on. For TC-required fields this is usually fine; if the locator should be required, set it to `false` explicitly.

## Locator hygiene checklist

Before committing a new registry entry, verify:

- [ ] `Strategy` matches the locator string format (`XPath` for `//`, `CSS` otherwise).
- [ ] `FieldType` resolves to the action you actually want (`Button` for label clicks, not `Checkbox`).
- [ ] `Pages` is the smallest set that includes every page the field appears on. Don't over-tag.
- [ ] `DefaultValue` is a value the target tenant actually exposes (don't default to "Owned" if the dropdown has only ranges).
- [ ] `{0}` substitution: the value at runtime will be a real DOM-matchable string; `PreserveCasing = true` if case-sensitive.
- [ ] `not(contains(...))` exclusions for any sibling field with a shared class prefix.
- [ ] `InteractionOptions.Timeout` raised from the default if the field is on a slow tenant (KLX Staging needs 10–15s for many fields).
