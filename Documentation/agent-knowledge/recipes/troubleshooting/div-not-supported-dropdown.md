---
topic: troubleshooting:div-not-supported-dropdown
summary: "'tag div not supported dropdown type' — locator ends on inner div; switch end-anchor to //ng-select."
status: ready
---

# Framework rejects div locator on a Dropdown field

**Symptom:** Framework error in the test log:

```
Element [...//div[contains(@class,'ng-value-container')]] with tag 'div'
is not a supported dropdown type. Supported types: 'select', 'ng-select'.
```

**Likely cause:** The registry entry's locator targets a descendant `<div>` of the ng-select host, not the host itself. The framework's `SelectDropdown` handler only operates on `<select>` or `<ng-select>` elements.

**Fix:** Change the locator's end-anchor from `//div[contains(@class,'ng-value-container')]` to `//ng-select`. TC 240782 hit this on `CL_BIPD`, `CL_MedPay`, `CurrentBopCarrier`, `CL_TransportationExpense` — all "Dropdown" entries that originally terminated on the inner div.

**Cross-references:** [../locator-recipes.md](../locator-recipes.md) recipe 2 trap; [../../framework/ui-field-types.md](../../framework/ui-field-types.md).
