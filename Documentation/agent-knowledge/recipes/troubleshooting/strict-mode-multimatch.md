---
topic: troubleshooting:strict-mode-multimatch
summary: "Playwright strict mode violation: locator resolves to N elements; use .First or scope to container."
status: ready
---

# Strict-mode multi-match

**Symptom:** Playwright throws "strict mode violation: locator … resolved to N elements."

**Likely causes:**

- `Loader` selector matches multiple elements (`.loader, .loading-overlay, .spinner` are comma-separated and any single one may be present multiple times).
- `ng-dropdown-panel .ng-option` at page scope matches options from multiple open dropdowns.
- Two switcher labels on the page have overlapping text.

**Fix:**

- `.First` on the locator: `Page.Locator(Loader).First`.
- Scope to the control container: `controlContainer.Locator("ng-dropdown-panel .ng-option").First`.
- Tighter text match: `normalize-space(.)='Yes'` instead of `contains(.,'Yes')`.

**Cross-references:** [../locator-recipes.md](../locator-recipes.md).
