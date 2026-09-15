---
topic: troubleshooting:disabled-button-click
summary: Click does nothing because button is disabled — press Tab to blur input and run validators.
status: ready
---

# Click on disabled button does nothing

**Symptom:** Clicked Submit (or Continue) but no event fires. Page sits.

**Likely cause:** Button is `disabled` at click time — Angular validation hasn't run because the input wasn't blurred.

**Fix (input):** Press Tab after `FillAsync` to blur the input and run validators:
```csharp
await input.PressAsync("Tab");
```

**Fix (verification):** Wait for the `disabled` attribute to be removed before clicking:
```csharp
await Page.WaitForFunctionAsync(
    "(xpath) => { var n = document.evaluate(xpath, document, null, " +
    "XPathResult.FIRST_ORDERED_NODE_TYPE, null).singleNodeValue; " +
    "return !!n && !n.disabled && !n.hasAttribute('disabled'); }",
    SubmitButtonXPath);
await submit.ClickAsync();
```
