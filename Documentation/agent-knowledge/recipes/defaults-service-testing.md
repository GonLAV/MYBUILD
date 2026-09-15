# Testing the Defaults service

How to get real coverage on field defaults. The short version: **lint the rule table for
invariants, evaluate the rule set through the service, and keep end-to-end quotes for anchoring
only.** Business behaviour of the defaults model itself is KB canon —
`domain:interview/features/field-defaults/docs/overview` in `bolt-knowledge-base`. This leaf is
only about how to test it from nexus.

## Why value assertions are the wrong instinct

The rule table (`dbo.Defaults`, per-tenant Epos DB) is not a lookup of field → value. Each row is
a conditional: `CalculateCondition` and `Value` are Scriban templates, `Feature` gates on flags
with `!` negation, and `FlowType` / `Lobs` / `States` are comma-lists defining an applicability
set. Asserting "row X has value Y" against a shared environment is asserting a snapshot of
mutable data — it fails the first time someone edits a default and never catches a real bug.

Assert **invariants** (shape) and **resolved output** (behaviour) instead.

## Layer 1 — lint the rule table

One connection, read all active rows once, then assert over the in-memory set. No test data, no
cleanup, no browser, runs in any environment in seconds. Wire the DB access by mirroring
`ResultData`: entity under `Database/Entities/Main/`, `ITable<Defaults>` on `MainDbContext`,
`DefaultsQueries : MainQuery`, exposed on `IMainQueries`.

Checks worth having:

| Check | Why it catches something |
|---|---|
| `States` never null/empty | Empty states means the rule **never fires** — the inverse of every other gate |
| State tokens valid | Valid set includes territories (`AS`, `GU`, `MP`, `PR`, `UM`, `VI`); `!`-prefixed lists are negations, not tokens |
| `Target` is a real field | A typo writes to a field nobody reads |
| Identifiers inside `CalculateCondition` are real fields | The silent-never-fires bug |
| `Value` in the target's option set | Enum/bool/numeric mismatches |
| `Feature` tokens are live flags | A dead flag key changes which generation is active |
| No overlapping active rules on one target | Within a group level the winner is **arbitrary** |

The authoritative field and enum catalogue is the Content service's `QuoteEntities` static
resource, surfaced by the Defaults service's own `SearchFields` and `GetValueOptions` endpoints —
so "is this target real / is this value legal" is a service call, not guesswork.

Guard the fixture: assert the loaded set is non-empty in `OneTimeSetUp` and log the resolved
database name. Without that, a mis-mapped connection string makes every invariant pass vacuously
(see the UNIFY → USAA connection-string trap).

`TestBase`, not `UITestBase` — there is no browser in this layer.

## Layer 2 — evaluate through the service

`IDefaultsService.GetDefaults` is the real evaluator and it is **stateless with respect to
quotes**: the `PolicyData` its conditions evaluate against is built entirely from the
`DefaultTargets` you pass in. So you can drive the whole matrix with synthetic data — no quote,
no browser.

```
GetDefaultsRequest  { GroupId, FlowType, Lobs, State, DefaultTargets[] { Target, CurrentValue, Audit } }
GetDefaultsResponse { DefaultValues[] { Target, Value } }
```

Register it like `IPrefillService` — add the `Bolt.Microservice.Defaults.Interfaces` package and
`AddBoltMicroservice<IDefaultsService>()`. You need a real `GroupId`; the service resolves group
ancestry from it.

Matrix axes: **FlowType × Lobs × State × prefilled-vs-bare**. That last axis is mandatory, not
optional — defaults are only considered for targets whose current value is empty, so an
already-answered field means the rule is never evaluated and the test proves nothing.

For a migration (e.g. a feature-gated generation replacing an ungated one), run each cell twice
across the flag and **diff the resolved maps per cell**. Expected diffs are the story; unexpected
diffs are the finding. Never diff aggregate counts — a change that alters flow coverage and state
coverage at once nets out to nothing in a total.

Pin the clock: conditions use `date.now.year`, so windows like year-built and roof age drift
annually. Build synthetic data with relative years, never literals.

Do **not** reimplement the Scriban evaluation in the test. It was a reasonable fallback while the
service looked uncallable; it is strictly worse now — same breadth, lower fidelity.

## Layer 3 — anchor, don't cover

A handful of end-to-end quotes confirming the interview passes the inputs the service expects.
Coverage lives in layer 2.

## Splunk as standing monitoring

The service reports both failure modes itself, so they are queryable in any environment without a
test:

- `GetDefaults encountered multiple relevant rules for a field` — the overlap condition, named
  field and competing rules.
- `GetDefaults warning. Couldn't calculate the following rules` — targets dropped after the
  fixed-point resolver stopped making progress. The quote proceeds without them.

## Anti-patterns

- **Asserting rule-row values.** Snapshot of mutable data; rots on first edit.
- **Driving quotes to cover the matrix.** Layer 2 is a stateless service call — quotes are for
  anchoring only.
- **Omitting the emptiness axis.** Fill-empty-only means a pre-answered field never reaches the
  rule.
- **Expecting the admin UI to exercise conditions.** Create and bulk-upload force
  `CalculateCondition = {{true}}` and clear `Feature`, so ADBX CRUD tests structurally cannot
  reach conditional or feature-gated rules — the ones with real logic.
- **Treating a dropped default as a failure signal.** It is a warning; nothing surfaces to the
  user and the response simply omits the target.
