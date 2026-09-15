---
topic: lob:renters
summary: Renters (HO4) — personal property, deductible, tenant-only fields.
status: ready
---

# LOB · Renters (HO4, `LobType.Renters`)

**Cache representation:** No TC explicitly covers Renters in the current sample. `FlowType.InterviewHO4Flow` and `D2CRentersFlow` exist.

## Typical fields

Personal property value, deductible, address, prior claims. Lighter than HO3 (no roof/dwelling structure).

## Phase 2 hint

- `LOB=HO4 + feature=Interview` → `FlowType.InterviewHO4Flow`.
- `LOB=HO4 + feature=D2C` → `D2CRentersFlow`.

## Cross-references

[INDEX.md](INDEX.md), [home.md](home.md) (sibling personal-line property LOB).
