---
topic: troubleshooting:field-not-found
summary: "'Could not find UIElement for field X' — registry alias / wrong FrontEnd / Pages doesn't include current page."
status: ready
---

# Could not find UIElement for field X

**Symptom:** The test's `formData` dictionary includes `[FieldNames.X] = "value"` but at runtime the field is silently skipped or throws.

**Likely causes:**

- Field-name alias mismatch — registry has `VIN`, you wrote `VehicleVIN`.
- Wrong FrontEnd registry — field is in `FieldRegistryADBX` but `ScopeContext.FrontEnd == Interview` (or vice versa).
- `Pages` doesn't include the page type the test is currently on.

**Fix:**

1. Grep `Bolt.Automation.FrontEnds/` for the constant name. Confirm it's defined in `FieldNames(Interview|ADBX|Common)` partials.
2. Confirm the registry entry's `Pages` includes `typeof(<CurrentPage>)`.
3. Confirm `ScopeContext.Set(ctx => ctx.FrontEnd, FrontEndType.X)` was called for the right FrontEnd before `Executor.Execute*`.

**Cross-references:** [../../framework/field-registry.md](../../framework/field-registry.md) — registry mechanism + canonical-name convention.
