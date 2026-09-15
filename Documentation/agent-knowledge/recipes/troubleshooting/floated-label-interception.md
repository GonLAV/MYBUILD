---
topic: troubleshooting:floated-label-interception
summary: <app-form-label> intercepts pointer events on ng-select; click .ng-arrow-wrapper instead.
status: ready
---

# Floated label intercepts pointer events on ng-select

**Symptom:** Playwright log:

```
<app-form-label ...> intercepts pointer events
```

followed by the framework's `Select` reporting success but the UI still showing "Please select".

**Likely cause:** KLX renders an `<app-form-label>` overlay above the ng-select's input region. Playwright's standard click targets that center and never reaches the panel-open arrow. (Sometimes accompanied by `ng-select-disabled` class transiently at click time.)

**Fix:** Page-object override. Click `.ng-arrow-wrapper` (right edge of the ng-select, outside the label overlay) to open the panel, then click the option. List-driven helper covers multiple fields per page.

**Cross-references:** [../override-patterns.md](../override-patterns.md) pattern F (floated-label arrow-wrapper); [../../domain/partners/kraftlakex.md](../../domain/partners/kraftlakex.md).
