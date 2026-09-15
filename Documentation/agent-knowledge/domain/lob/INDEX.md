---
topic: lob:index
summary: LobType enum reference, title-fragment decoding map, cross-LOB patterns (bundles, CovMod, result-page forms).
status: ready
---

# Lines of business — index

> **When to read:** Phase 1 (decoding the LOB segment of a TC title) and Phase 2 (mapping LOB to flow + identifying expected fields per LOB).

## `LobType` enum

Defined in `Bolt.Automation.Common/Enums/LobType.cs`. Full inventory:

| Enum | Human label | Insurance category | Leaf |
|---|---|---|---|
| `Auto` | Personal Auto (PA) | Personal automotive | [auto.md](auto.md) |
| `Home` | Homeowners (HO3) | Standard owner-occupied dwelling | [home.md](home.md) |
| `Renters` | Renters (HO4) | Tenant rental dwelling | [renters.md](renters.md) |
| `Condo` | Condo (HO6) | Condominium / co-op owner | [condo.md](condo.md) |
| `DF` | Dwelling Fire | Non-standard residential (rental property, vacant) | [dwelling-fire.md](dwelling-fire.md) |
| `Umbrella` | Umbrella | Personal liability excess | [umbrella.md](umbrella.md) |
| `WorkersCompensation` | Workers Compensation (WC) | Commercial employee coverage | [wc.md](wc.md) |
| `GeneralLiability` | General Liability (GL) | Commercial liability | [gl.md](gl.md) |
| `CommercialAuto` | Commercial Auto (CL Auto) | Commercial vehicle / fleet | [cl-auto.md](cl-auto.md) |
| `BusinessOwnersPolicy` | Business Owners Policy (BOP) | Bundled commercial coverage | [bop.md](bop.md) |
| `Flood` | Flood | NFIP / private flood | [flood.md](flood.md) |
| `Earthquake` | Earthquake | Seismic damage | [earthquake.md](earthquake.md) |
| `Motorcycle` | Motorcycle | Powersports | [motorcycle.md](motorcycle.md) |
| `DeviceProtection` | Device Protection | Electronics | [device-protection.md](device-protection.md) |
| `Pets` | Pets | Pet liability / medical | [pets.md](pets.md) |

## How LOB shows up in TC titles

Title segment varies by tenant convention:

| LOB | Common title fragment | Example title |
|---|---|---|
| Personal Auto | `Auto`, `PL Auto`, `PL \| AUTO` | "Comparion \| GetQuteAPI \| PL \| AUTO \| Prefill \| Fenris" (145915) |
| HO3 | `HO3`, `Home`, `Personal Home` | "Kraftlake \| Agent Interview \| Personal Home \| Result page" (235615) |
| HO4 (Renters) | `Renters` | (FlowType.D2CRentersFlow) |
| HO6 (Condo) | `HO6`, `Condo`, `Condominium` | "PGR \| HQX 2.0 \| Condo — Overview" (241340), "PGR \| HQX 2.0 \| HO6 \| RoofResponsible" (242142) |
| Commercial Auto | `CL Auto`, `Commercial Auto` | "Boltag \| Interview \| Commercial Auto \| 2 drivers + 2 vehicles E2E" (236743), "KLX \| Interview E2E \| CL Auto \| old interview" (240782) |
| BOP | `BOP` | "Bolt AG \| Interview CL \| BOP \| Result page \| Acord carrier" (236379), "KraftlakeX \| CL \| BOP \| Markets results" (237437) |
| WC | `WC` | "Product \| Interview V3 \| Market availability page \| Check monopolistic state message" (235414, body) |
| Pets | `Pets`, dog-breed references | "PGR \| Consumer \| Adding 'Cane Corso' to Dog Breed dropdown list" (238828) |

## Cross-LOB patterns

- **Bundle flows** combine Auto + Home: `InterviewBundleFlow`, `D2CAutoHomeFlow`, `D2CHomeAutoFlow`, `D2CCondoAutoFlow`. Order matters — `AutoHome` and `HomeAutoCondoAuto` are different page sequences.
- **Result page form-download** TCs (235615, 236379) test that the bind packet (Acord forms or carrier-branded application) generates correctly.
- **Coverage modifications** (CovMod) — Plymouth Rock has carrier-specific overrides controlled by feature flags (TC 240777). When a TC mentions `CovMod`, expect feature-flag-driven branching. See [../partners/progressivepl.md](../partners/progressivepl.md).

## Anti-patterns

- **Treating "PL" as Personal Auto only.** "PL" is Personal Lines — covers Auto + Home + Renters + Condo. Read the next title segment to disambiguate.
- **Picking a generic flow when a carrier-specific flow exists.** `D2CAutoFlow` won't drive Bristol West's OLB pages — `USAAAutoFQFlow` (or whichever Bristol West-specific flow) does.
- **Assuming Interview vs D2C from LOB.** Same LOB can be Interview (agent) or D2C (consumer); the `feature` field is authoritative.
- **Forgetting state-monopoly rules for WC.** A WC test in OH/ND/WA/WY is testing the *block message*, not the quote. See [wc.md](wc.md).
