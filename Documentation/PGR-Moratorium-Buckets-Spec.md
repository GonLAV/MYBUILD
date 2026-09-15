# PGR PAA Test — Moratorium Bucket Spec

## Problem

PGR's rates page displays a maximum of 4 carriers. PGR applies an external
ranking algorithm — when more than 4 carriers return quotes for a given
address/LOB, lower-ranked carriers are dropped from the page. Several of our
target carriers for PAA-questions and bridge-URL tests are being ranked out,
causing those tests to fail not because of a real defect but because the
target carrier never reaches the page.

## Mitigation Strategy

Use PGR's moratorium tool to **block non-target carriers on a small set of
automation-dedicated zips**, so that only the ≤4 carriers we want to test
remain eligible to display on the rates page.

Buckets below are derived from the test cases in
[PaaTestCases.cs](../Bolt.Automation.Tests/TestData/Progressive/PaaTestCases.cs).
The spec is **zip-centric**: each zip = one moratorium rule. Different LOB
tests can share the same zip because rates queries are naturally LOB-scoped
(a carrier that only writes HO3 doesn't appear in an HO6 query, even if it's
"allowed" in that zip).

## Constraints / assumptions

- **Moratorium is applied globally** in the PGR platform — these rules
  cannot be applied in production without affecting real customer
  traffic. Tests using these buckets must run in non-prod only.
- **Carrier eligibility is opaque** — we don't know in advance the full
  list of carriers PGR will return for a given zip/LOB. The bucket rule is
  expressed as "allow only the listed target carriers; block everything
  else", so it is robust to unknown carriers entering or leaving the zip.
- **Addresses per zip** — Helen will source the specific street addresses
  needed per LOB within each zip (some LOBs need property characteristics
  that not every address in a zip can satisfy).

## Bucket Spec (by zip)

| Zip   | State | LOB | Allow-list (carriers permitted for this LOB in zip) |
| ----- | ----- | --- | --------------------------------------------------- |
| 37027 | TN    | MH  | Assurant, Foremost, AmericanModern                  |
| 37027 | TN    | HO3 | TowerHill, StillWater, ForemostSignature            |
| 37027 | TN    | HO6 | StillWater                                          |
| 37027 | TN    | DF  | Foremost, AmericanModern                            |
| 08833 | NJ    | HO3 | PlymouthRock                                        |
| 08833 | NJ    | HO6 | PlymouthRock                                        |
| 33837 | FL    | HO3 | TowerHill, AII                                      |
| 45504 | OH    | HO3 | Homesite                                            |
| 45504 | OH    | HO6 | Homesite                                            |
| 84401 | UT    | HO3 | Nationwide, ASI, Openly                             |
| 84401 | UT    | HO6 | Nationwide, ASI                                     |
| 90220 | CA    | HO3 | Bamboo                                              |

**Total: 6 unique zips covering 12 (zip, LOB) test scopes and all 30+ test cases.**

AL_Adger and AZ_Tucson buckets are no longer needed — all StillWater,
Foremost (DF/MH), AmericanModern, and ForemostSignature (HO3) tests were
consolidated into the TN bucket.

> **Note:** `CarrierEnums.Foremost` and `CarrierEnums.ForemostSignature` are
> **distinct carriers**. Foremost is used for Dwelling Fire (DF) and
> Manufactured Home (MH); ForemostSignature is used for Homeowners (HO3).

> **Consolidation notes:**
> - Homesite HO6 relocated from AZ_Tucson to OH 45504 (reuses the HO3 address).
> - PlymouthRock relocated from NY_Averill_Park to NJ_LebanonBoro 08833.
> - ASI, Openly, Nationwide tests relocated from AZ_Tucson to UT 84401.
> - StillWater HO3 relocated from AL_Adger to TN 37027.
> - StillWater HO6, Foremost (DF/MH), AmericanModern (DF), ForemostSignature
>   (HO3) all relocated from AZ_Tucson to TN 37027.
> - Result: AZ_Tucson and AL_Adger buckets are now empty and removed.

> If the moratorium tool supports `(carrier, zip, LOB)` granularity, configure
> one rule per row above. If moratorium is only `(carrier, zip)`, the
> effective per-zip allow-list is the **union** across LOBs (e.g., TN 37027 =
> all carriers from its four rows combined).

### Test cases covered per zip

- **37027 (TN)**
  - MH questions: 219572 (Assurant), 219573 (Foremost), 219567 (AmericanModern)
  - MH bridge: 221524 (Assurant)
  - HO3 questions: 227984 (TowerHill), 216030 (StillWater — relocated from AL), 228773 (ForemostSignature — relocated from AZ)
  - HO3 bridge: 221569 (StillWater — relocated from AL)
  - HO6 questions: 219384 (StillWater — relocated from AZ)
  - DF questions: 219595 (Foremost — relocated from AZ), 219593 (AmericanModern — relocated from AZ)
  - DF bridge: 214367 (AmericanModern — relocated from AZ), 214428 (Foremost — relocated from AZ)
- **08833 (NJ)**
  - HO3 questions: 216939 (PlymouthRock)
  - HO3 bridge: 221522 (PlymouthRock)
  - HO6 questions: 221193 (PlymouthRock)
- **33837 (FL)** — HO3 questions: 219259 (TowerHill), 219249 (AII)
- **45504 (OH)**
  - HO3 questions: 214136 (Homesite)
  - HO3 bridge: 219602 (Homesite)
  - HO6 questions: 215323 (Homesite — relocated from AZ_Tucson)
- **84401 (UT)**
  - HO3 questions: 219125 (Nationwide), 214941 (ASI — relocated from AZ), 219247 (Openly — relocated from AZ)
  - HO3 bridge: 219606 (Nationwide), 219601 (ASI — relocated from AZ), 221520 (Openly — relocated from AZ)
  - HO6 questions: 221194 (Nationwide — relocated from AZ), 219138 (ASI — relocated from AZ)
- **90220 (CA)** — HO3 questions: 228954 (Bamboo)

### Block list per zip (derived from allow-list)

Below is the inverted view — the carriers the platform team should
moratorium-block per zip. The list is built from the carriers tracked (18 as of 2026-08)
in [CarrierEnums.cs](../Bolt.Automation.Common/Enums/CarrierEnums.cs)
(Homesite, ASI, PlymouthRock, StillWater, Foremost, ForemostSignature,
AmericanModern, Hippo, Progressive, LibertyMutual, Nationwide, Openly,
TowerHill, AII, True, Assurant, Bamboo).

> **Note on Progressive:** Progressive is in the enum but represents the
> platform itself, not a competing carrier on the rates page. Confirm with
> the platform team whether it should be in the block list at all.

> **Caveat:** PGR may have additional carriers beyond our enum (we don't
> track full eligibility). The safer expression is the allow-list rule in
> the next section — use the block lists below as a starting point and
> add any unknown carriers PGR has in that zip.

*(+ Progressive?)* in every row below means: confirm with the platform team
whether Progressive itself should be in the block list.

| Zip   | State | LOB | Carriers to BLOCK (moratorium)                                                                                                  |
| ----- | ----- | --- | ------------------------------------------------------------------------------------------------------------------------------- |
| 37027 | TN    | MH  | Homesite, ASI, PlymouthRock, StillWater, ForemostSignature, Hippo, LibertyMutual, Nationwide, Openly, TowerHill, AII, True, Bamboo *(+ Progressive?)* |
| 37027 | TN    | HO3 | Homesite, ASI, PlymouthRock, Foremost, AmericanModern, Hippo, LibertyMutual, Nationwide, Openly, AII, True, Assurant, Bamboo *(+ Progressive?)* |
| 37027 | TN    | HO6 | Homesite, ASI, PlymouthRock, Foremost, ForemostSignature, AmericanModern, Hippo, LibertyMutual, Nationwide, Openly, TowerHill, AII, True, Assurant, Bamboo *(+ Progressive?)* |
| 37027 | TN    | DF  | Homesite, ASI, PlymouthRock, StillWater, ForemostSignature, Hippo, LibertyMutual, Nationwide, Openly, TowerHill, AII, True, Assurant, Bamboo *(+ Progressive?)* |
| 08833 | NJ    | HO3 | Homesite, ASI, StillWater, Foremost, ForemostSignature, AmericanModern, Hippo, LibertyMutual, Nationwide, Openly, TowerHill, AII, True, Assurant, Bamboo *(+ Progressive?)* |
| 08833 | NJ    | HO6 | Homesite, ASI, StillWater, Foremost, ForemostSignature, AmericanModern, Hippo, LibertyMutual, Nationwide, Openly, TowerHill, AII, True, Assurant, Bamboo *(+ Progressive?)* |
| 33837 | FL    | HO3 | Homesite, ASI, PlymouthRock, StillWater, ForemostSignature, AmericanModern, Hippo, LibertyMutual, Nationwide, Openly, True, Assurant, Bamboo *(+ Progressive?)* |
| 45504 | OH    | HO3 | ASI, PlymouthRock, StillWater, Foremost, ForemostSignature, AmericanModern, Hippo, LibertyMutual, Nationwide, Openly, TowerHill, AII, True, Assurant, Bamboo *(+ Progressive?)* |
| 45504 | OH    | HO6 | ASI, PlymouthRock, StillWater, Foremost, ForemostSignature, AmericanModern, Hippo, LibertyMutual, Nationwide, Openly, TowerHill, AII, True, Assurant, Bamboo *(+ Progressive?)* |
| 84401 | UT    | HO3 | Homesite, PlymouthRock, StillWater, Foremost, ForemostSignature, AmericanModern, Hippo, LibertyMutual, TowerHill, AII, True, Assurant, Bamboo *(+ Progressive?)* |
| 84401 | UT    | HO6 | Homesite, PlymouthRock, StillWater, Foremost, ForemostSignature, AmericanModern, Hippo, LibertyMutual, Openly, TowerHill, AII, True, Assurant, Bamboo *(+ Progressive?)* |
| 90220 | CA    | HO3 | Homesite, ASI, PlymouthRock, StillWater, ForemostSignature, AmericanModern, Hippo, LibertyMutual, Nationwide, Openly, TowerHill, AII, True, Assurant *(+ Progressive?)* |

### Rule format the platform team should configure

For each zip in the table above:

> In zip `<zip>`, block **all carriers except** the carriers listed in the
> Allow-list column.

Equivalent phrasing (if the platform UI is block-only):

> In zip `<zip>`, apply moratorium to **every PGR carrier not in this list:
> <allow-list>**.

### Risk: zip 37027 (TN) — four LOBs share one zip

After consolidation, TN 37027 hosts MH, HO3, HO6, and DF tests. Because
moratorium is `(carrier, zip)` only, each LOB query in this zip will return
whichever allow-listed carriers write that LOB there. Specifically:

- **MH** targets (Assurant, Foremost, AmericanModern) and **DF** targets
  (Foremost, AmericanModern) share Foremost and AmericanModern. Both LOBs
  also allow TowerHill, StillWater, and ForemostSignature in the zip — if
  any of those writes MH or DF in 37027 and ranks above a target, that
  test fails.
- **HO3** targets (TowerHill, StillWater, ForemostSignature) and **HO6**
  target (StillWater) share StillWater. Both LOBs also allow Foremost/
  AmericanModern in the zip — same cross-LOB risk.
- One-time check recommended: confirm that none of the allow-listed
  carriers cross-write into a LOB where they aren't targets and outrank
  a real target.

**Mitigation option** if cross-LOB ranking interference is observed: move
specific LOBs to a separate TN zip and split the allow-list.

## Addresses required (Helen to source)

For each zip below, we need a specific street address that supports the
listed LOB(s). Note property-type prerequisites: HO6 needs a condo, MH needs
a manufactured home, DF needs a non-owner-occupied dwelling, HO3 needs a
single-family home.

| Zip   | State | LOBs needed | Notes                                                       |
| ----- | ----- | ----------- | ----------------------------------------------------------- |
| 37027 | TN    | MH, HO3, HO6, DF | Using "5840 Sterling Oaks Dr" for **all LOBs** (per consolidation decision). Covers Assurant/Foremost/AmericanModern (MH), TowerHill/StillWater/ForemostSignature (HO3 — ForemostSignature relocated from AZ), StillWater (HO6 — relocated from AZ), Foremost/AmericanModern (DF — relocated from AZ). |
| 08833 | NJ    | HO3, HO6     | Using "133 Conover Ter, Lebanon Boro, NJ 08833" (HO3). Need HO6 condo address in same zip. |
| 33837 | FL    | HO3          | "319 Tomelloso Way" already configured.                     |
| 45504 | OH    | HO3, HO6     | Currently using "318 Victory Rd" (HO3). Need HO6 condo address in same zip for Homesite HO6 (relocated from AZ_Tucson). |
| 84401 | UT    | HO3, HO6     | Using "4063 S 3700 W" (HO3 — Nationwide/ASI/Openly relocated from AZ). Need HO6 condo address in same zip for Nationwide/ASI HO6 (relocated from AZ). |
| 90220 | CA    | HO3          | "311 W Raymond St" already configured.                      |

## Maintenance

- Re-review this spec when a new PAA test case is added — may require a
  carrier added to an existing zip's allow-list or a new zip.
- If a test starts failing with "target carrier not on page" despite the
  bucket existing, PGR likely onboarded a new competitor in that zip/LOB.
  Add the new carrier to the block list (or confirm the rule is still
  expressed as an allow-list and re-apply).
- Each PAA test case should reference the zip it's bound to, so drift is
  easier to detect.

## Out of scope

- The moratorium tool itself and its UI/API — owned by the PGR platform
  team.
- Production parity — these tests cannot run in prod under this strategy.
  If prod smoke coverage is required, a separate read-only sanity strategy
  is needed.
