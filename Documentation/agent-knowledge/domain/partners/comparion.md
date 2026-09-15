---
topic: partner:comparion
summary: Comparion — Personal Lines (Auto/Home); Fenris prefill flow.
status: ready
---

# Partner · Comparion — `COMPARION`

> **API `partner` field:** `Comparion` · **Common abbreviations:** Comparion

**Cache representation:** 4 TCs.

## Business model

SSO-only platform; multi-tenant under the hood. Has Fenris prefill integration.

## Entry path

SSO via STS with Comparion-specific Issuer/Audience. No password in QA/UAT.

## Features in cache

GetQuoteAPI (1), STS & Login (1), ADBX (1), Platform tools (1).

## Real TCs

- **145915** — Comparion | GetQuteAPI | PL | AUTO | Prefill | Fenris (Fenris prefill, VIN decode validation, Splunk events).
- **200974** — Comparion | SSO | Interview Adjustment | Generic Kickout Page — error 109 (validates SSO error page).
- **234237** — ADBX | Defaults table — Add a new record — Comparion.
- **218726** — Sanity | QA | Platform | Default tool | Add audit log details for the defaults in the UI.

## Known quirks

- **Fenris prefill vendor** — Auto prefill returns vehicle/driver data via the Comparion path; Splunk events validate the call.
- **Error 109** is the canonical SSO kickout error — any TC mentioning `SSO error` likely uses this code.
- **ADBX defaults table** is per-tenant — same UI but data scoped to Comparion.

## Cross-references

[INDEX.md](INDEX.md), [../lob/auto.md](../lob/auto.md), [../lob/home.md](../lob/home.md).
