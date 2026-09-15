# CoverageModificationTestData

Static test data for coverage-modification (CovMod) scenarios: which coverage/deductible options each carrier offers per state, and the verbiage/display data the UI should show.

## Files

| File | Holds |
|---|---|
| `CarrierCoverageData.cs` | Per-carrier coverage option matrix: `Carrier(CarrierEnum, Name, Coverages[])` → `Coverage(CoverageEnum, StateValues[])` → `CoverageValue(Type, Value)`. State groups are comma-separated lists (`"AZ, IL, MO"`, or `"ALL"`); values are dollars (`CoverageValueType.Dollar`), percent-of-Coverage-A (`PercentOfCovA`), or `None` |
| `CoverageData.cs` | `CoverageDefinitions` / `DeductibleDefinitions` specs with per-LOB overrides, and `CoverageDataProvider` (`GetCoverages(lob)`, `FindCoverage(lob, coverage)`, `GetCoverageVerbiage(lob, coverage, state)`) |
| `CoverageDisplayData.cs` | Expected display strings |
| `KeyPointsData.cs` | Coverage key-points content |
| `CarrierInformationStatementData.cs` | Carrier information statements |

## Public API (`CarrierCoverageData`)

```csharp
// Values a carrier offers for a coverage type in a state
CoverageValue[] values = CarrierCoverageData.GetCoverageValues(
    CarrierEnums.Homesite, CoverageEnums.AllPerils, state: "NJ");

// All coverage types a carrier participates in
CoverageEnums[] types = CarrierCoverageData.GetCoverageTypesForCarrier(CarrierEnums.Hippo);
```

## Adding data

Add or extend an entry in the `Carriers` array using the builder shorthands: `S("state, list")` for state groups, `D(500)` for dollar values, `P(50)` for percent-of-CovA, `N()` for none. Group states that share the same value list into one `StateValues` row.

Partner-specific CovMod behavior (e.g. PGR Wind/Hail Not-Applicable) is documented in the KB: `Documentation/agent-knowledge/domain/partners/`.
