---
topic: troubleshooting:page-validation-timeout
summary: ValidatePageReadyAsync times out — PageIdentifier substring is too narrow or wrong.
status: ready
---

# Page validation timing out

**Symptom:** `PageHelper.ValidatePageReadyAsync` times out — page validates "URL contains <PageIdentifier>" but the URL is different.

**Likely cause:** `PageIdentifier` is too specific or wrong.

**Fix:** Open `page_source_*.html`, copy the actual URL fragment, pick the smallest **unique** substring. KLX Markets is `MarketResults`, not `_Markets`. KLX `/CL_Start` is `_Start`, not `CL_Start` (so it can be reused for BOLTAG `Start` too).

**Cross-references:** [../../framework/page-objects.md](../../framework/page-objects.md) "PageIdentifier is a substring".
