---
topic: troubleshooting:popup-interception
summary: Click intercepted by overlay label/span; click the overlay element itself.
status: ready
---

# Strict popup interception

**Symptom:** A control's normal click target is intercepted by an overlay `<label>` or `<span>`. Click silently does nothing or hits the wrong element.

**Likely cause:** Angular Material / custom theme renders an overlay above the standard control for styling.

**Fix:** Click the overlay element instead. See `Product_PolicyPage.SelectLossesAnswer` for the pattern:

```csharp
var label = control.Locator("label.switcher-wrapper:has-text('No')");
await label.ClickAsync();
```

For floated-label ng-select interception specifically, prefer the arrow-wrapper helper instead — see [floated-label-interception.md](floated-label-interception.md) and [../override-patterns.md](../override-patterns.md) pattern F.

**Cross-references:** [../override-patterns.md](../override-patterns.md) pattern D, [../../framework/popups.md](../../framework/popups.md).
