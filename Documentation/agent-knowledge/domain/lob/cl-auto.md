---
topic: lob:cl-auto
summary: Commercial Auto — FEIN, NAIC code, business + operator + vehicle fields; KLX-specific Cert / DeclinationReason.
status: ready
---

# LOB · Commercial Auto (`LobType.CommercialAuto`)

**Cache representation:** 2 TCs in the current sample (KLX 240782 + Bolt AG 236743).

## Typical underwriting fields

- **Business contact:** address, business name, contact person.
- **Industry:** NAIC code (e.g. 4411000 — New Car Dealers).
- **Business profile:** legal entity (Corporation/LLC), FEIN (9 digits required — see [../partners/kraftlakex.md](../partners/kraftlakex.md)), business start year, annual payroll, owner involvement, towing/hauling.
- **Vehicle:** VIN (with decode handshake on KLX — see [../partners/kraftlakex.md](../partners/kraftlakex.md)), body style, sub-category, average miles driven, daily trips, primary use, garaging address.
- **Operator:** gender, marital status, DOB, license info, SR-22, exclusion, accidents/violations.
- **Policy:** effective date, current insurance, coverages.
- **Applicant:** email, mailing address, credit-check permission.

## KLX-only quirks

Cert checkbox + DeclinationReason ng-select (Farmers declination) — see [../partners/kraftlakex.md](../partners/kraftlakex.md).

## Phase 2 hint

`LOB=CL Auto + feature=Interview + tenant=KRAFTLAKEX` → `FlowType.InterviewCLAutoFlow` (the old-interview flow). For BoltAG CL Auto on the new interview, check whether a different flow exists.

## Cross-references

[bop.md](bop.md) (sibling commercial LOB), [../partners/kraftlakex.md](../partners/kraftlakex.md), [../partners/boltag.md](../partners/boltag.md).
