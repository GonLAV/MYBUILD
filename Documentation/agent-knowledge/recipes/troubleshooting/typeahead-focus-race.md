---
topic: troubleshooting:typeahead-focus-race
summary: ng-select typeahead reverts after click; let the next field's click commit the previous typeahead.
status: ready
---

# Tab/focus race after typing into ng-select typeahead

**Symptom:** Industry typeahead types the value, you click an option, and the value reverts to the placeholder.

**Likely cause:** ng-select typeahead requires focus to move *away* (Tab or click outside) to commit the selection.

**Fix:** After clicking the option, click an unrelated element (or the next field's container). Better: let `base.FillForm` continue with the next field — that click commits the previous typeahead. See `Product_StartPage.FillForm` for the order: industry typeahead first, then `base.FillForm`.

**Cross-references:** [../locator-recipes.md](../locator-recipes.md) recipe 4 (SearchDropdown / typeahead).
