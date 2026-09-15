---
topic: troubleshooting:reading-captured-page-source
summary: Which signals in a captured interview page_source are trustworthy — ng-value-label yes, radio attributes and ng-invalid no, ng-select options absent entirely.
status: ready
---

# Reading a captured interview `page_source_*.html`

The failure artifact is the real rendered DOM of an Angular app, not a server-rendered form. Several
obvious-looking greps give confidently wrong answers.

## Trustworthy

- **Question present at all** — `<app-control ... id="PolicyData.<Field>">`. Absent means the question
  did not render for this quote. This is the check that settles conditional-vs-late.
- **ng-select current value** — `<span class="ng-value-label">…</span>` inside the control. Present =
  answered, and it shows the exact label.
- **Required marker** — `<span class="required …">` within the control.
- **The run log**, which beats the DOM for *what the framework did*: `Processing field: X = Y` followed
  by either a `UI Action [Select|Fill|Check]` line or `Element not found but ignoring`.

## Not trustworthy

- **Radio / checkbox state.** Angular does not reflect it as a `checked` attribute. Grepping for
  `checked` matches `aria-checked="false"` and reports every group as answered.
- **`ng-invalid`.** It sits on wrappers and ancestors, so a naive window-scan marks unrelated fields
  invalid. A whole page can read as INVALID while it is fine.
- **`value="…"` on text inputs.** Angular binds the property, not the attribute, so a filled input
  often shows an empty `value`. The `has-value` class on the control container is a better hint, and
  only exists on some control types.
- **ng-select option lists.** The dropdown panel is rendered only while open, so a capture taken after
  the failure contains **zero** `role="option"` nodes. You cannot read the available options out of it
  — see [dropdown-option-missing.md](dropdown-option-missing.md) for the method that works.

## Segment boundaries

Bound each question by the *next* `<app-control … id="…">`, not by a fixed character window. A window
bleeds neighbouring controls in and produces nonsense like one field reporting another's value.

**Cross-references:** [conditional-field-not-rendered.md](conditional-field-not-rendered.md),
[page-validation-timeout.md](page-validation-timeout.md).
