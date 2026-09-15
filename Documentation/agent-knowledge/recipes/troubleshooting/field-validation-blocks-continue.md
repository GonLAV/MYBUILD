---
topic: troubleshooting:field-validation-blocks-continue
summary: Continue blocked by red validation — DefaultValue placeholder failed tenant validation; supply valid value inline.
status: ready
---

# Field validation error blocks Continue

**Symptom:** Clicking Continue does nothing; a red validation message appears next to a field that "looks fine."

**Likely causes:**

- Tenant validates field format more strictly than expected (FEIN must be 9 digits, phone must match a specific pattern).
- `DefaultValue` is a placeholder (`"4785X"`) that fails validation.
- `DefaultValue` is a value the target tenant doesn't expose (KLX `CLLengthVehicleOwnership` doesn't have "Owned"; the test should supply a real option like `"Less than 1 month"` via the inline `formData` dict).

**Fix:**

- Use a valid value (FEIN: `"478500001"`; KLX VehicleOwnerShip: `"Less than 1 month"` or `"At least 1 year but less than 3 years"`).
- Update `DefaultValue` in the registry if every test on that page-tenant combo needs the same value, or set it inline in the test's `formData` dictionary if only this test needs it.
- Inspect the dropdown's actual options in `page_source_*.html` and pick a real one.

**Cross-references:** [../../framework/field-registry.md](../../framework/field-registry.md) "Setting DefaultValue to a value that doesn't exist on the target tenant" anti-pattern.
