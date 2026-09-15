---
topic: lob:auto
summary: Personal Auto (PA) — VIN decode, driver/vehicle fields, carrier-specific D2C flows (Bristol West, Safeco).
status: ready
---

# LOB · Personal Auto (`LobType.Auto`)

**Cache representation:** 6 TCs touch Personal Auto.

## Typical underwriting fields (from TC step bodies)

- VIN — typed manually or decoded via API; results in Year/Make/Model/BodyStyle.
- Driver info: DOB, license number/origin/state, gender, marital status, relationship to applicant, SR-22 status, accidents/violations history.
- Vehicle coverage: Comprehensive deductible, Collision deductible, Rental reimbursement, Fire & Theft, current vehicle value, Roadside assistance.
- Address (garaging) and miles driven.

## Multi-driver / multi-vehicle TCs

Common (TC 236743 explicitly tests "2 drivers + 2 vehicles").

## Carrier-specific D2C variants

- **Bristol West** (USAA) — `USAAAutoFQFlow`.
- **Safeco** (Bolt AG / USAA) — `SafecoAutoFQFlow`.

Each has its own FlowType.

## Phase 2 hint

- `LOB=PA + feature=Interview` → `FlowType.InterviewAutoFlow`.
- `LOB=PA + feature=D2C + carrier=Safeco` → `SafecoAutoFQFlow`.

## Cross-references

[../partners/usaa.md](../partners/usaa.md) (Bristol West carrier), [../../recipes/troubleshooting/vin-decode-flake.md](../../recipes/troubleshooting/vin-decode-flake.md), [../../recipes/override-patterns.md](../../recipes/override-patterns.md) pattern B (VIN decode handshake).
