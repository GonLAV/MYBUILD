---
topic: troubleshooting:conditional-reveal
summary: Field exists in registry but not in DOM until trigger; override FillForm to fill trigger first.
status: ready
---

# Conditional reveal — field appears mid-fill

**Symptom:** A field is registered and tagged, but it doesn't exist in the DOM until *after* another field is filled. The registry iteration tries to fill it before reveal and times out.

**Likely cause:** Angular conditional reveal — clicking a checkbox or selecting a dropdown adds the next field to the DOM after a render cycle.

**Fix:** First try `DependsOn` + `DependsOnValue` + a generous `InteractionOptions.Timeout` on the registry entry — that models the reveal without an override. See [../override-patterns.md](../override-patterns.md) "Override pattern A".

If `DependsOn` isn't expressive enough (popup-triggered reveal, custom retry logic), override `FillForm` on the page object (see [../override-patterns.md](../override-patterns.md) pattern A historical fallback):

- Extract the dependent field from the user dict.
- Call `base.FillForm` for the rest.
- Drive the trigger field, wait for the dependent to appear (long timeout — 30s on KLX Staging), drive the dependent.
- Set `Pages = []` on both fields' registry entries so `MergeDataManager` doesn't try to fill them.
