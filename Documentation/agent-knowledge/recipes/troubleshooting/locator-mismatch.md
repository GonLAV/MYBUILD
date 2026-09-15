---
topic: troubleshooting:locator-mismatch
summary: Page validates but field fails to fill — tenant class scheme differs, sibling lookalike, wrong element type.
status: ready
---

# Locator wrong — page validation passes but field doesn't fill

**Symptom:** Test reaches the page but the locator times out or fills nothing. DOM snapshot shows the field exists but the registry locator doesn't match.

**Likely causes:**

- Tenant uses a different class scheme (KLX class collapsing).
- Sibling field has a class name that's a substring of yours (`Year` matches `YearOfDriving`).
- The actual element type differs from the registry strategy (registry says `//div[contains(@class,'ng-value-container')]` but the page renders an `<ng-select>`).

**Fix:**

- Open `page_source_*.html`, grep for the class name, copy the actual hierarchy.
- Add an XPath OR for the tenant-specific scheme (see [../../framework/field-registry.md](../../framework/field-registry.md) "XPath OR" section).
- Add `not(contains(@class,'SiblingPrefix'))` to exclude the lookalike.
- Switch the strategy: `//ng-select` for ng-select dropdowns, `//input` for inputs, `//label` for switcher/checkbox-wrapper buttons.

**Cross-references:** [../locator-recipes.md](../locator-recipes.md), [div-not-supported-dropdown.md](div-not-supported-dropdown.md).
