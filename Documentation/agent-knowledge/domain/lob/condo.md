---
topic: lob:condo
summary: Condo (HO6) — HO3 fields plus high-rise toggle, roof responsibility, floor numbers.
status: ready
---

# LOB · Condo (HO6, `LobType.Condo`)

**Cache representation:** Heavy in Progressive sample (TC 241340, 242142, 242143, 242144, 242145).

## Typical fields

All the HO3 fields ([home.md](home.md)) plus condo-specific:

- **High-rise yes/no** (TC 242143-242144 — floor fields hidden when "High Rise Condo" not selected).
- **Roof responsibility** (Master / HOA / Self) — TC 242142 enforces this when Condominium selected.
- **Floor numbers** (TC 242144).

## Cross-references

[home.md](home.md) (HO3 base fields), [../partners/progressivepl.md](../partners/progressivepl.md) (HQX 2.0 product switching).
