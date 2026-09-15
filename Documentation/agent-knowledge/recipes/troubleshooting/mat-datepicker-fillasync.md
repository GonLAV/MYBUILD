---
topic: troubleshooting:mat-datepicker-fillasync
summary: FillAsync rejected on mat-datepicker hidden input; JS set + dispatch input/change/blur events.
status: ready
---

# FillAsync rejected on mat-datepicker hidden input

**Symptom:** Playwright error on a date field that visibly renders an `MM/DD/YYYY` input:

```
element is not visible, enabled, or editable
```

**Likely cause:** The visible input is a façade rendered by Angular Material; the actual `matinput` underneath has `display: none`. `FillAsync` is targeting the hidden one.

**Fix:** Page-object override using JS that sets `el.value` and dispatches the events Angular's two-way binding listens for:

```csharp
await input.EvaluateAsync(@"(el, v) => {
    el.value = v;
    el.dispatchEvent(new Event('input',  { bubbles: true }));
    el.dispatchEvent(new Event('change', { bubbles: true }));
    el.dispatchEvent(new Event('blur',   { bubbles: true }));
}", value);
```

KLX `EffectiveDate` (CL Policy) and `OperatorDateOfBirth` both need this.

**Cross-references:** [../override-patterns.md](../override-patterns.md) pattern E (mat-datepicker DOM events); [../../domain/partners/kraftlakex.md](../../domain/partners/kraftlakex.md).
