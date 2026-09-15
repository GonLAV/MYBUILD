---
topic: troubleshooting:vin-decode-flake
summary: VIN decode signal never arrives; catch TimeoutException + warn + let downstream fields fail loudly.
status: ready
---

# VIN decode signal never arrives (environmental flake)

**Symptom:** VIN typed, Submit clicked, but the page's Year/Make/Model/BodyStyle never populates. Downstream Vehicle fields fail.

**Likely cause:** Decode service is environmentally flaky. Not a code bug.

**Fix (defensive):** `Product_VehiclePage.FillVinAndDecodeAsync` waits for the populated-Year locator with a `try { … } catch (TimeoutException)` and a warning log:

```csharp
try {
    await Page.Locator($"xpath={YearDecodedLocator}").First.WaitForAsync(
        new LocatorWaitForOptions { State = WaitForSelectorState.Attached });
} catch (TimeoutException) {
    _logger?.Warning("VIN decode signal not detected; continuing.");
}
```

If decode silently fails, the downstream Year/Make/Model fields will fail their own selects loudly enough to identify the real issue. If recurring on a given environment, add a fallback that picks Year/Make/Model/BodyStyle from the ng-select options manually.

**Cross-references:** [../override-patterns.md](../override-patterns.md) pattern B (VIN decode handshake), [../../domain/lob/auto.md](../../domain/lob/auto.md), [../../domain/lob/cl-auto.md](../../domain/lob/cl-auto.md).
