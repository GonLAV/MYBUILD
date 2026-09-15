---
topic: troubleshooting:dropdown-option-missing
summary: Dropdown opens but option text not found — option varies by carrier; use firstIfMissing fallback.
status: ready
---

# Dropdown opens but option text not found

**Symptom:** Framework error:

```
Could not find option [X] in [...//ng-select] dropdown.
```

**Likely cause:** The text you tried to select isn't a real `<ng-option>` for the carrier/page combination — option lists vary per carrier.

**Fix path A (resolve the real option label):** Ask the GetQuote data model — it is the source the
dropdown is generated from, and it is environment-independent:

```
mcp__ado__get_getquote_field_definitions(field_keys=["PolicyData.DistanceToFireHydrant"])
```

It returns `valid_values` (API codes, e.g. `_0_500ft`) **and** `display_labels` (the UI text, e.g.
`0-500 Feet`). The registry `DefaultValue` and any test override need the **label**, not the code.

> Do **not** try to read the options out of `page_source_*.html`. An `ng-select` renders its panel only
> while open, so a capture taken after the failure contains zero `role="option"` nodes. See
> [reading-captured-page-source.md](reading-captured-page-source.md).

For an ADBX popup dropdown (a real `<select>`, not covered by the GetQuote model), enumerate it live
instead — temporarily log `pageHelper.GetFieldDropdownListValues(<FieldName>)` and delete the probe
afterwards. That is how TC 252328 found the account Source option was `"Sales Environment"`, not the
`"Sales environment"` its manual steps specified.

**Fix path B (carrier-varying options):** Add the field to a page-object override list with `firstIfMissing = true` so the arrow-wrapper helper falls back to the first option when the requested text isn't present. TC 240782 used this for `CL_BIPD`, `CombinedUninsuredUnderinsuredMotorist`, `UninsuredMotoristPropertyDamage`.

**A registry `DefaultValue` can itself be an invalid option.** It stays invisible while every flow
seeds the field via API payload, then blocks the first UI-only test — both `ArchitectureStyle`
("Traditional") and `DistanceToFireHydrant` ("Less than 1000 ft") were wrong for years this way. When
you correct one, set it to the value the seed data already uses so the API-seeded flows are unaffected;
see [../ui-only-entry-point-coverage.md](../ui-only-entry-point-coverage.md).

**Cross-references:** [../override-patterns.md](../override-patterns.md) pattern G (first-option fallback).
