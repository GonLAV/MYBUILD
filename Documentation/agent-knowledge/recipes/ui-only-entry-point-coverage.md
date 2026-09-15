---
topic: recipe:ui-only-entry-point-coverage
summary: A test that starts from a blank UI account must answer every field the API-seeded flows get from their payload — diff the seed data against registry Pages tags before the first run.
status: ready
---

# UI-only entry points: pre-flight the field coverage

**When this applies:** the test creates its quote through the **UI** (ADBX New Quote → blank account,
consumer landing page) instead of `CreateApplication` / `GetQuestionnaire`. TC 252328 was the first
UNIFY test to do this for the agent interview.

**Why it bites.** Almost every interview test seeds a full payload
(`PersonalLineDataProvider.GetPersonalHomeData(...)` → `HomeDetailsData.GetBaseHomeDetails()`), so the
interview renders with those questions **already answered**. The `FieldRegistry` never has to supply
them, and a missing entry or a wrong `DefaultValue` stays invisible for years. Start from a blank
account and every one of those fields arrives unanswered — the registry is suddenly load-bearing.

On TC 252328 this produced five separate blockers, each surfacing one page at a time, one run apiece:

| Field | Seeded by payload at | Registry state |
|---|---|---|
| `DistanceToFireHydrant` | `HomeDetailsData.cs` `_0_500ft` | default `"Less than 1000 ft"` — not a valid option |
| `ArchitectureStyle` | `HomeDetailsData.cs` `Contemporary` | default `"Traditional"` — not a valid option |
| `NumberOfMortgagees` | `HomeDetailsData.cs` `0` | **no entry at all** |
| `IsMailAddress` | `HomeDetailsData.cs` `false` | **no constant, no entry** |
| `AnyAdditionalInsured` | `PolicyDetailsData.cs` `false` | **no entry at all** |

## Pre-flight, before the first run

1. List the flow's pages (`nexus-agent code flow-trace --flow <FlowType>`).
2. List what the equivalent API-seeded test sends — start at `GetBaseHomeDetails()` and the
   `PolicyDetailsData` / `HomeFeaturesData` profile the sibling test passes.
3. For each seeded field, check the registry: is there an entry, is it tagged to a page in your flow,
   and is its `DefaultValue` a real option (see
   [troubleshooting/dropdown-option-missing.md](troubleshooting/dropdown-option-missing.md) for how to
   resolve real option labels)?
4. Anything missing is a blocker you can fix **before** burning a 5-minute run on it.

**Loop-shape tripwire:** if each run advances exactly one page, stop iterating and do this sweep. The
symptom changes every run while the cause does not, so a "3 strikes on the same symptom" rule never
fires.

## Fixing a gap without breaking the seeded flows

Do **not** reflexively tag a new entry to the page. The API-seeded tests answer these fields from their
payload, so a page-tagged entry makes the UI fill overwrite the payload value — silent today only
because every profile happens to use the same values (`0` / `false` / `false`). Prefer:

- **Untagged entry (`Pages = []`) + drive it explicitly** from the test that actually sees it blank.
- **Fix a wrong `DefaultValue`** to the value the seed data already uses, so the UI and API paths agree
  and the seeded flows see no behaviour change.

Note that correcting a default is not free: these selects were *failing* before, which made them
accidental no-ops wherever prefill had answered the field. Once valid, they fire — on TC 252328
`ArchitectureStyle` now overwrites the `Ranch` that property prefill returns.

**Cross-references:** [../framework/field-registry.md](../framework/field-registry.md),
[troubleshooting/interactwithfield-throws-on-missing.md](troubleshooting/interactwithfield-throws-on-missing.md),
[troubleshooting/dropdown-option-missing.md](troubleshooting/dropdown-option-missing.md).
