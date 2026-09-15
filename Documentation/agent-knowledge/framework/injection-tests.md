---
topic: framework:injection-tests
summary: Runtime-injected (Professional Services) tests — InjectionTestBase/UIInjectionTestBase, INJECTED_* env var contract, and the multi-axis injected-parameter fan-out pattern (state x LOB, INJECTED_PS_STATE/INJECTED_PS_LOB, InjectedParameterCatalog, D2CLobCatalog).
status: ready
---

# Runtime-injected tests (Professional Services)

> **When to read:** the TC targets a **new/unonboarded tenant or environment** — anything the
> static `(Tenant, Environment)`-keyed data stores don't cover. The Professional Services team
> tests fresh onboardings constantly, so their values **cannot be hardcoded**: everything that
> changes per engagement is supplied as `INJECTED_*` environment variables at run time (locally
> via `.runsettings`, or per-job from the orchestrator's Professional Services wizard).

## When to use which base

| Situation | Base | Tenant/data source |
|---|---|---|
| Normal test, onboarded tenant | `TestBase` / `UITestBase` | `[Tenant]` attribute → static data stores |
| PS / new tenant or env, API | `InjectionTestBase` | `INJECTED_*` env vars only |
| PS / new tenant or env, UI | `UIInjectionTestBase` | `INJECTED_*` env vars only |

Injection tests live in `Bolt.Automation.Tests/Tests/ProfessionalServices/`, carry
`[Category("ProfessionalServices")]`, and take **no `[Tenant]` attribute** — tenant comes from
`INJECTED_TENANT` (the discovery rule RSLN003 exempts these bases). Siblings to copy:
`ProfessionalServicesD2CTests`, `ProfessionalServicesAdbxTests`,
`ProfessionalServicesPartnerPortalTests`, `ProfessionalServicesAdbxAndGetQuoteApiTests`.

## The env var contract

`InjectedTestConfig` (Bolt.Automation.Common/Configuration/InjectedConfig) declares every
variable with a verbatim `[EnvVar]` name — no prefix conventions, the attribute string IS the
contract QAs type into the orchestrator:

| Env var | Config property | Notes |
|---|---|---|
| `INJECTED_TENANT` | `Tenant` | required, top-level |
| `INJECTED_ENVIRONMENT` | `Environment` | required, top-level — drives appsettings selection, secrets bundle, and the Mongo run-environment label (NOT `ASPNETCORE_ENVIRONMENT`) |
| `INJECTED_PS_STATE` | `State` | optional, top-level — one axis of [InjectedParameter] fan-out (see below) |
| `INJECTED_PS_LOB` | `Lob` | optional, top-level — second fan-out axis; unset means `D2CLobCatalog.DefaultLob` (Auto) |
| `INJECTED_ADBX_LOGIN_URL/_USERNAME/_PASSWORD` | `Adbx.*` | ADBX section |
| `INJECTED_PARTNER_PORTAL_URL/_USERNAME/_PASSWORD/_SOURCE` | `PartnerPortal.*` | Partner Portal section |
| `INJECTED_D2C_URL` | `D2C.Url` | D2C section |
| `INJECTED_GETQUOTE_API_KEY/_AGENT_IDENTITY` | `GetQuoteApi.*` | GetQuote API section |

Validation is **opt-in per section**: override `RequiredSections` in the test class so only the
sections your test consumes gate it (missing vars fail fast with one aggregated error):

```csharp
private static readonly HashSet<string> _requiredSections = new() { nameof(InjectedTestConfig.D2C) };
protected override IReadOnlySet<string> RequiredSections => _requiredSections;
```

Local run — put the vars in a `.runsettings` `<EnvironmentVariables>` block (see the commented
example at the bottom of `ProfessionalServicesD2CTests.cs`).

## Injected-parameter pattern (multi-axis fan-out)

One test method that runs **once per combination** of the values chosen at job-creation time.
Each `[InjectedParameter]` marker declares one **axis**; the orchestrator fans the user's
selection into one work item per combination, and each worker process receives a single value
per axis, so the test never parses collections:

```csharp
[Test]
[Category("ProfessionalServices")]
[TestCaseId(24024701)]                        // ← discovery-side row id (see below)
[TestCaseSource(nameof(StatesAndLobsToVerify))]
[InjectedParameter("INJECTED_PS_STATE")]      // ← one marker per axis; discovery reads BOTH
[InjectedParameter("INJECTED_PS_LOB")]
public async Task MyTest_ByStateAndLob(AddressKey addressKey, string lob)
{
    var address = AddressData.GetAddress(addressKey);
    var lobFlow = D2CLobCatalog.Get(lob);
    // ... drive the flow with this address and LOB ...
}

private const string ByStateAndLobTestCaseId = "24024701"; // a STRING, never int (see below)

private static IEnumerable<TestCaseData> StatesAndLobsToVerify()
{
    var config = InjectedConfigLoader.Load();
    var state = config.State.Trim();
    var lob = D2CLobCatalog.Canonicalize(
        string.IsNullOrWhiteSpace(config.Lob) ? D2CLobCatalog.DefaultLob : config.Lob);

    if (lob is null)
    {
        yield return new TestCaseData(AddressData.ResolveStateKey("TX"), D2CLobCatalog.DefaultLob)
            .SetProperty("TestCaseId", ByStateAndLobTestCaseId)
            .Ignore($"INJECTED_PS_LOB '{config.Lob}' is not a supported line of business. " +
                    $"Supported: {string.Join(", ", D2CLobCatalog.SupportedLobInputs())}");
        yield break;
    }

    if (string.IsNullOrEmpty(state))
    {
        // Exactly ONE Ignore()d default case (never zero): keeps the test discoverable in the
        // orchestrator's env-less discovery cache without silently running values nobody chose.
        yield return new TestCaseData(AddressData.ResolveStateKey("TX"), lob)
            .SetProperty("TestCaseId", ByStateAndLobTestCaseId)
            .Ignore("INJECTED_PS_STATE is not set — supply it locally or run per-state via the orchestrator");
        yield break;
    }

    var addressKey = AddressData.ResolveStateKey(state);

    yield return new TestCaseData(addressKey, lob)
        // Display names must be the ARGUMENT values, not the raw inputs: the composite TestId is
        // built from NUnit's rendered arg list, so `state` here ("TX") would silently produce
        // "24024701_TX__Home" instead of "24024701_TX_Crowley__Home".
        .SetArgDisplayNames(addressKey.ToString(), lob)
        .SetProperty("TestCaseId", ByStateAndLobTestCaseId);
}
```

Why each piece exists:

- **`[InjectedParameter(envVar)]`** — matched *by attribute name* by the orchestrator's discovery
  tool, which records **every** marker's env-var name and fans one work item per combination of
  the selected values. The attribute is `AllowMultiple`; anything reading it must use
  `GetCustomAttributes` (**plural**) — `GetCustomAttribute<T>()` throws `AmbiguousMatchException`
  once a method carries two. It also gates `TestMetadataResolver`'s composite TestId
  (`"{TestCaseId}_{argsSignature}"`, e.g. `24024701_TX_Crowley__Home` — NUnit's `", "` argument
  separator sanitizes to `__`) so per-combination runs in one job don't collide on a single Mongo
  run record. Tests without the attribute keep plain numeric TestIds — no churn.
- **Parameter order in the method signature is part of the reporting contract.** `argsSignature`
  is positional, so swapping `(AddressKey, string)` renames every composite TestId and orphans the
  Mongo trend line and S3 artifact paths for that test.
- **Each test only multiplies by the axes it declares.** The orchestrator narrows every requested
  combination to a test's own axes and deduplicates, so a job selecting 2 states × 3 LOBs runs the
  older state-only test twice (once per state), not six times. Nothing is needed in the test for
  this — but it is why a single-axis test can safely sit in a multi-axis job.
- **The anchor id is declared TWICE, and both values must match.** The method-level
  `[TestCaseId(n)]` is what the orchestrator's *discovery* records as the row id (per-case
  properties don't survive discovery for `Ignore()`d `TestCaseData`). The per-case
  `.SetProperty("TestCaseId", "n")` — **a string, never an int** — is what NUnit's runtime
  `TestCaseId=` filter and the Mongo run-record key read; a method attribute does NOT propagate
  into generated case properties, and an int property value makes NUnit's `PropertyFilter` throw
  for every test in a filtered run. The id must be a **dedicated** ADO TC — duplicate ids across
  two test methods block orchestrator job creation.
- **`AddressData.ResolveStateKey(input)`** — accepts a bare code (`TX` → alphabetically-first
  mapped `TX_*` variant, deterministic) or a full `AddressKey` name (`TX_Crowley`, exact,
  case-insensitive). Throws with a clear message for unsupported inputs.
- **An unresolvable value must `Ignore()`, never fall back.** Running a different product than the
  one the user picked is worse than not running — hence `Canonicalize` returning `null` yields a
  visible skip naming the supported inputs.

### Adding a new fan-out axis

Routing new axes through one catalog is what makes this need **no orchestrator release**:

1. Add the env var to `InjectedTestConfig` with its verbatim `[EnvVar("INJECTED_...")]`.
2. Add one entry per method to `InjectedParameterCatalog`
   (`Bolt.Automation.Tests/TestExtension/Helpers/InjectedParameterCatalog.cs`):
   `ParameterChoices()` — what the wizard OFFERS (short, canonical, human-facing);
   `ParameterAcceptedValues()` — the wider superset that VALIDATES a submitted value (aliases,
   saved-run spellings); and `ParameterCanonicalValues()` — alias → canonical for every accepted
   value, canonical values mapping to themselves. Skip the third only for an axis with no aliases,
   where accepted and canonical are the same list.
3. Put `[InjectedParameter("INJECTED_...")]` on the test method and take the value as a parameter.

On the next discovery cache build the orchestrator renders a picker for the axis, validates
against the accept set, and fans work items out across it. An env var marked on a method but
absent from the catalog gets no picker — add both together.

**`INJECTED_PS_STATE` is the one exception.** It predates the catalog and is NOT in it: the
orchestrator reads it from its own probe of `AddressData.SupportedStateInputs()` into the report's
`supportedStates` field, and the wizard collapses the city-level `AddressKey` names to bare codes
for display. That path works end-to-end and was deliberately left untouched rather than migrated
for symmetry. Adding a new **state** is therefore still just an `AddressKey` enum member plus an
`AddressData` entry in `_addressesByKey` — nothing to add to the catalog.

**Cross-repo name contract.** The discovery tool reflects, by exact type + method name:

| Nexus surface | Purpose |
|---|---|
| `InjectedParameterAttribute` (by attribute NAME) | which axes a test declares |
| `InjectedParameterCatalog.ParameterChoices()` | values offered, per non-state axis |
| `InjectedParameterCatalog.ParameterAcceptedValues()` | values accepted, per non-state axis |
| `InjectedParameterCatalog.ParameterCanonicalValues()` | alias → canonical, per non-state axis — its own probe (map-shaped) |
| `AddressData.SupportedStateInputs()` | the state axis's accept set — its own dedicated probe |

Do not rename or move any of them without updating `packages/discovery` in
automation-orchestrator.

Adding a new **LOB** is an entry in `D2CLobCatalog` — `CanonicalLobs()` (offered),
`SupportedLobInputs()` (accepted) and `LobCanonicalMap()` (alias → canonical) all derive from it,
so it surfaces automatically. Canonical names match the `LobType` enum and the product's own
wording; policy codes (`HO3`, `HO4`, `HO6`, `DP3`), spelled-out forms (`Condominium`,
`Dwelling Fire`) and the older `HomeAuto` spelling stay accepted as aliases but are not offered,
because one product must never appear as several choices.

**Why the accept set needs the map.** Aliases are many-to-one on purpose, so a consumer that
dedupes requested values by spelling turns `['Bundle', 'HomeAuto']` into two work items that run
the same flow and resolve to one composite TestId — the second run record collides on the unique
`idx_runId_testId` index and the two pods overwrite each other's outcome. The orchestrator folds
requested values through `ParameterCanonicalValues()` before fan-out (`resolveRequestedAxes` in
`packages/logger/src/services/work-items-builder.ts`), so the two spellings collapse to the one
work item the caller meant. `INJECTED_PS_STATE` has the same shape (`TX` vs `TX_Crowley`) but no
map, because it is not in this catalog — it still fans out twice.

Currently mapped:

| Canonical | Flow | Notes |
|---|---|---|
| `Auto` | `D2CAutoFlow` | the pre-LOB-picker default |
| `Home` | `D2CHomeFlow` | conditional roof step |
| `Renters` | `D2CRentersFlow` | shortest flow; nothing to skip |
| `Home+Auto` | `D2CHomeAutoFlow` | bundle; conditional roof step |
| `Condo` | `D2CCondoFlow` | needs `PLTypeOfDwelling=Condominium`; **no** roof page in this flow |
| `Condo+Auto` | `D2CCondoAutoFlow` | bundle; same form data; no roof page |
| `DF` | `D2CHomeFlow` | needs `IsPrimaryResidence=No`; PropertiesUsage must NOT be skipped |
| `DF+Auto` | `D2CHomeAutoFlow` | bundle; same form data and skip caveat |

Two things the catalog had to grow to express these:

- **`FormData`** — the product derives the line of business from what the interview is *told*, not
  from the flow alone. Condominium is the Condo flow **plus** `PLTypeOfDwelling`, which otherwise
  defaults to `PersonalHome` and would quote a house. Dwelling Fire has no flow of its own at all:
  it is the Home flow with `IsPrimaryResidence=No`, and the app derives `DwellingFire` from that.
  Do **not** use `FormData` to pin fields that drive conditional pages (see the `PLYearBuilt` note
  on `HasConditionalRoofStep`) — it is for values a user would genuinely answer.
- **A per-entry skip set that can invert.** Every other property LOB skips `PropertiesUsage`
  because `IsPrimaryResidence` defaults to `Yes`. DF answers `No`, so that page renders *because*
  of the answer and is where `DwellingUsage` is filled — skipping it would strand the flow.

## Running

- **Locally:** one value per axis per run — set `INJECTED_PS_STATE=TX` (or `TX_Crowley`) and,
  for the two-axis test, `INJECTED_PS_LOB=Home` alongside the other `INJECTED_*` vars.
  `INJECTED_PS_STATE` unset → the single default case reports as Skipped; `INJECTED_PS_LOB`
  unset → Auto; `INJECTED_PS_LOB` unrecognised → Skipped, never a fallback.
- **Orchestrator:** the Professional Services wizard (`/professional-services` in the logger)
  lists `ProfessionalServices`-category tests, takes the `INJECTED_*` values (or a saved
  variable collection) and a per-axis selection, and queues one work item per COMBINATION —
  each independently dispatched, retried, and reported (`…ByStateAndLob(TX_Crowley,Home)` rows).
  Every requested combination is projected onto the axes a test actually declares and then
  deduplicated, so a 3-state x 2-LOB selection queues six items for the two-axis test but only
  three for a state-only one. Per-item values arrive as env overrides layered on the job's
  `envVariables` at pickup, so the `+` in `Home+Auto` reaches the test verbatim.
  Templates/scheduled runs do not fan out.

## Cross-references

- [test-class.md](test-class.md) — the normal (static-data) test skeleton this replaces.
- [../../../Bolt.Automation.Tests/CLAUDE.md](../../../Bolt.Automation.Tests/CLAUDE.md) — NUnit parameterized conventions (serializable args, `.SetProperty` ids).
- [../philosophy/tests-stay-clean.md](../philosophy/tests-stay-clean.md) — injection tests follow the same orchestration-only body rules.
