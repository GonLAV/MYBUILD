---
topic: troubleshooting:default-reinjected
summary: Field shows registry default after you set a value — MergeDataManager injected it; suppress via empty string or Pages=[].
status: ready
---

# Default re-injected — field has wrong value or fights the page

**Symptom:** A field you set to a specific value (or removed entirely) shows up with the registry default. Or a cascading dropdown like Year/Make/Model fights the VIN-decode result.

**Likely cause:** `MergeDataManager` re-injects `DefaultValue` whenever the user dictionary doesn't have a non-empty value for a field tagged on the current page.

**Fix:**

- **Suppress** — set the field to an empty string in your `FillForm` override's filtered dict:
  ```csharp
  filtered[VIN] = "";
  ```
  `ProcessField` skips empty values, so the default isn't injected.
- **Or untag** — set `Pages = []` on the registry entry. Useful when the field is only ever driven out-of-band (Cert, DeclinationReason).

When in doubt: suppress via empty string. It's reversible and keeps the registry entry valid for other tests.

**Cross-references:** [../../framework/field-registry.md](../../framework/field-registry.md) "Empty-string suppression"; [../../philosophy/sparse-dictionaries.md](../../philosophy/sparse-dictionaries.md).
