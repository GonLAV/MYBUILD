---
topic: lob:bop
summary: Business Owners Policy — business contact, property, market appetite, Acord form generation.
status: ready
---

# LOB · Business Owners Policy (`LobType.BusinessOwnersPolicy`)

**Cache representation:** 2 TCs (Bolt AG 236379, KraftlakeX 237437).

## Typical fields

- **Business contact + business profile** (similar to CL Auto: FEIN, NAIC, owner payroll).
- **Property:** building info, occupancy.
- **Coverage:** limits per coverage line.
- **Markets:** appetite-based carrier eligibility (TC 236379 uses AK state appetite).
- **Result page:** Acord forms (125 / 126 / 140) downloadable.

## Phase 2 hint

Look for `InterviewBOPFlow` in `Projects/Interview/Flows/Flows.cs`. KraftlakeX's TC 237437 introduces an offline-request path on `CL_offline page` when no carrier matches.

## Cross-references

[cl-auto.md](cl-auto.md) (shares business profile fields), [gl.md](gl.md) (GL is often bundled into BOP).
