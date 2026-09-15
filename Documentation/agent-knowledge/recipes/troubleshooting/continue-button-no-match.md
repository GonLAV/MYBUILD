---
topic: troubleshooting:continue-button-no-match
summary: "ClickContinueButton: no selector matches; add tenant-specific button class to InterviewBase or override ClickContinue."
status: ready
---

# Continue button — no selector matches

**Symptom:** `ClickContinueButton` throws `PageElementException`: "Could not find or click any continue button on page: <url>".

**Likely cause:** The page uses a non-standard button class (`button.Get.quotes` for KLX Markets) not in `InterviewBase.ContinueButton`.

**Fix:**

- **Preferred:** add the selector to the `ContinueButton` comma-separated list in `InterviewBase` if it's broadly applicable.
- **If the page also needs custom timing** (waits for a lookup before the button is fully clickable): override `ClickContinue` on the page object with explicit waits and timeouts (see [../override-patterns.md](../override-patterns.md) pattern C).

**Cross-references:** [../../framework/page-objects.md](../../framework/page-objects.md) "InterviewBase essentials".
