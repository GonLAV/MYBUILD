---
topic: lob:home
summary: Homeowners (HO3) — dwelling type, construction, roof, prior claims, dog breeds.
status: ready
---

# LOB · Homeowners (HO3, `LobType.Home`)

**Cache representation:** 6 TCs touch Home (Progressive HQX 2.0 dominates).

## Typical underwriting fields

- Property: dwelling type (HO3 vs HO6 vs HO4), year built, square footage, number of stories, occupancy.
- Construction: roof type/year/responsibility, exterior wall material, foundation, basement type, slab type, second-floor material (TC 241435), heating type, central AC.
- Risk: prior claims, dog breeds (TC 238828), special features (e.g. swimming pool, charging station).

## HO3 vs HO6 product change

A real flow Progressive HQX tests heavily — the user changes product type mid-flow and certain fields appear/disappear (TC 241340-242145 cluster). See [condo.md](condo.md) for HO6-specific fields.

## Phase 2 hint

- `LOB=HO3 + feature=Interview` → `FlowType.InterviewHO3Flow`.
- `LOB=HO6 + feature=D2C + tenant=PROGRESSIVEPL` → typically a Progressive-specific D2C flow (check `Projects/D2C/Flows/`).

## Cross-references

[condo.md](condo.md) (HO6 variant), [../partners/progressivepl.md](../partners/progressivepl.md) (HQX 2.0).
