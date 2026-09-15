---
topic: partner:boltag
summary: BoltAG — primary white-label tenant; supports most products across Interview and D2C.
status: ready
---

# Partner · Bolt AG (`BOLTAG`)

> **API `partner` field:** `Bolt AG` · **Common abbreviations:** BOLTAG, Boltag, BoltAG, Bolt-AG

**Cache representation:** 6 TCs.

## Business model

Direct Agent (the canonical / baseline tenant). Has every feature area and most LOBs.

## Entry path

Standard agent login (email ends with `@boltinc.com`); no SSO; uses ADBX agent dashboard then drills into Interview / D2C / quote.

## Features in cache

Interview (3), ADBX (2), Payment (1), D2C (Bristol West, Safeco — heavy use elsewhere).

## Sub-areas observed

`D2C Safeco` (242399, 242404, 242405, 242406, 242407 — Safeco-specific FQ flow tests), `Interview CL` (236379 BOP, 236743 CL Auto), `ADBX Default tool` (241459).

## Real TCs

- **236743** — Boltag Interview Commercial Auto, 2 drivers + 2 vehicles E2E.
- **236379** — Bolt AG Interview CL BOP — Result page Acord carrier (125/126/140 forms).
- **236062** — Bolt AG ADBX email template send with attachment.
- **241459** — ADBX Default tool / Enable additional users with CRUD permissions.
- **241722** — Payment site Sanity (Production).

## Known quirks

None that require special handling beyond the standard framework patterns. Treated as the canonical baseline — most fields and flows are designed against BoltAG first.

- **No class collapsing** — registry entries with `PolicyData.X` work directly.
- **Standard popups** — Update-info popup on existing accounts; New Quote popup follows the registry pattern.
- **No KLX-style cert reveal** — fields render synchronously.

When in doubt, BOLTAG is the canonical pattern.

## Cross-references

[INDEX.md](INDEX.md), [../lob/cl-auto.md](../lob/cl-auto.md), [../lob/bop.md](../lob/bop.md).
