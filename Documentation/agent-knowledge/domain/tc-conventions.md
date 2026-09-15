---
topic: domain:tc-conventions
summary: How TCs are titled and structured in ADO — tenant | feature | LOB | scenario fragments.
status: ready
---

# Test case conventions

> **When to read:** Phase 1 (parsing TC titles, descriptions, steps deterministically).

How to read a TC at this org. Title grammar, body conventions, ADO field semantics.

## Title-prefix grammar

Dominant pattern (used by 80%+ of well-formed TCs):

```
PARTNER | FEATURE_or_FLOW | LOB | SUB_AREA | DESCRIPTION
```

with `|` as the separator and 3-7 segments depending on specificity.

### Worked examples (from the cache)

| TC | Title |
|---|---|
| 240782 | `KLX | Interview E2E | CL Auto | old interview, submitting quote` |
| 236743 | `Boltag | Interview | Commercial Auto | 2 drivers + 2 vehicles E2E` |
| 236379 | `Bolt AG | Interview CL | BOP | Result page | Acord carrier` |
| 235615 | `Kraftlake | Agent Interview | Personal Home | Result page | Application forms E2E` |
| 237437 | `KraftlakeX | CL | BOP | Markets results | CL_offline page | Offline request` |
| 145915 | `Comparion | GetQuteAPI | PL | AUTO | Prefill | Fenris` (note typo: "GetQuteAPI") |
| 197386 | `KLX | ADBX to CM | Sales case | Create | PL | Bind request | Dummy quote | Message` |
| 198617 | `KLX | CMAPI | Sales case | KLX login | ADBX | Lead page` |
| 241340 | `PGR | HQX 2.0 | Condo - Overview – Verify correct questions and summary values per section` |
| 242142 | `PGR | HQX 2.0 | HO6 | RoofResponsible enforced when Condominium is selected on Overview` |
| 212329 | `USAA | SSO | Agents | SSO into the interview` |
| 162448 | `USAA | D2C | Auto | Retrieval Process and going back again to make sure no fields lost` |
| 235870 | `Product | D2C | Adding Bristol West OLB flow to the Auto D2C | E-sign` |
| 234237 | `ADBX | Defaults table - Add a new record - Comparion` |
| 200974 | `Comparion | SSO | Interview Adjustment | Generic Kickout Page - error 109` |

### Variant prefixes

| Prefix | Meaning | Example |
|---|---|---|
| `PARTNER \| ...` | Standard tenant-scoped TC | `BOLTAG \| D2C Safeco \| Verify Driver License Masking` (242399) |
| `Product \| ...` | Cross-cutting product work; partner indicated by `partner` field, not title | `Product \| D2C \| Adding Bristol West OLB flow` (235870) |
| `QA \| ...` | QA-team-internal tooling / framework / process work | `QA \| New Carrier Integration \| B3 - Implementation \| Mappings` (229559) |
| `Sanity \| ...` | Sanity-tagged cross-cutting work | `Sanity \| QA \| Platform \| Default tool \| Add audit log details` (218726) |
| `Case Page \| ...` | Older-style title naming a single page | `Case Page \| TCPA No \| Send SMS \| Sending SMS is unable` (235695) |
| Ad-hoc plain English | Older or quickly-written TCs | `Check adding second floor material` (241435) |

When the title doesn't follow `PARTNER | ...`, the `partner` field on the TC record is authoritative. Read it.

### Partner-abbreviation aliases

Tenant names in titles are not normalized:

| Title token | Resolves to `Tenant` |
|---|---|
| `BOLTAG`, `Bolt AG`, `Boltag`, `BoltAG`, `Bolt-AG` | `BOLTAG` |
| `KLX`, `KraftlakeX`, `Kraftlake`, `KL`, `KL (X)` | `KRAFTLAKEX` |
| `Unify`, `UNIFY` | `UNIFY` |
| `PGR`, `Progressive` | `PROGRESSIVEPL` |
| `USAA` | `USAA` |
| `Comparion` | `COMPARION` |
| `LibertyX` | `LIBERTYX` |
| `BoltAccess`, `Bolt Access`, `BOLT ACCES` (typo) | `BOLTACCESS` |

See [partners/INDEX.md](partners/INDEX.md) for fuller mapping.

## Description block

The `description` field (HTML) typically contains:

1. **Preconditions / setup** — required state before steps execute. Example (TC 236062): "A valid Bolt AG user with domain @boltinc exists; user has email-template permissions."
2. **Business rationale** — why this test exists.
3. **Feature scope** — what's being validated.
4. **Optional: data references** — usernames, account IDs, sometimes URLs.

Strip HTML before parsing — every description in the cache uses `<DIV>`, `<P>`, `<BR/>`, `<UL>`, `<LI>`, `<STRONG>`, `<B>`. Use a basic HTML→text strip (regex `<[^>]+>` then unescape `&amp;`, `&nbsp;`, `&quot;`).

Many older TCs have empty descriptions. Don't fail if `description` is empty — fall back to `steps[0].action` for context.

## Step structure

Each entry in `steps[]`:

```json
{
  "action": "<HTML> step instructions </HTML>",
  "expectedResult": "<HTML> outcome assertion </HTML>",
  "attachmentUrl": "https://…",       // optional — screenshot or attached file
  "attachmentFileName": "…"            // optional — original filename
}
```

### Real step example (TC 240782, step 1)

```
action: "<DIV><DIV><P>Access KLX flow, use the user:<BR/>lsp1_aortest@test.com<BR/>group: AOR-TEST</P></DIV></DIV>"
expectedResult: "<BR/>"
```

Stripped: `action="Access KLX flow, use the user: lsp1_aortest@test.com group: AOR-TEST"`, `expectedResult=""`.

### Real step example (TC 240782, step 3 — partial)

```
action: "<DIV><DIV><P>At the page <STRONG>"Business Contact Information"</STRONG>, enter the following values then click Next:</P><UL><LI><STRONG><SPAN>(KLX only field) </SPAN>Certification checkbo…"
```

Stripped: lots of bullet points listing fields and values.

### How to parse steps in Phase 1

1. Strip HTML.
2. Identify the page-name reference (often in **bold** — extract `<STRONG>...</STRONG>` content first, fall back to first sentence).
3. Identify field/value pairs from `<LI>` items or "fieldname: value" patterns in plain text.
4. The `expectedResult` is the assertion target ("user lands on CL start page", "rates appear with at least 1 carrier").

Don't overdo regex — feed the full step text plus the inferred page name to the user as part of the manifest, and let them correct ambiguities. The skill is iterative; precision in Phase 1 isn't worth the cost.

## ADO field semantics

### `state` — lifecycle

Observed values across the corpus + the spec:

| State | Meaning |
|---|---|
| `Design` | TC author drafting; not yet submitted for review. |
| `Pending Review` | TC drafted; awaiting reviewer approval. |
| `Approved` | Reviewer approved; ready for automation work. |
| `Pending Automation` | Approved + assigned to an automation engineer. |
| `Ready` | Variant of "Approved" / "Pending Automation" — observed in older TCs. Treat as automation-eligible. |
| `Automated` | Test code committed; running in CI. |
| `Deprecated` | TC retired; do not automate. |

For the skill, `Pending Automation` and `Ready` are the green-light states. `Automated` means a test already exists — read the linked work items (`links[].rel == "Microsoft.VSTS.Common.TestedBy-Reverse"` or check the linked Task) to find the existing class.

### `priority` — 1 (highest) to 4 (lowest)

Most TCs in the cache are priority 2. Priority 1 means the test must pass on every release.

### `tcType` — test classification

Observed values: `E2E Test`, `API Test`, `UI/UX Test`, `Negative Test`, plus unset for older TCs.

Influences scaffold choice:
- `E2E Test` → `UITestBase` + full flow.
- `API Test` → `TestBase` + Refit clients.
- `UI/UX Test` → `UITestBase` + targeted page assertion (often single-page, no full flow).
- `Negative Test` → expects an error / kickout page; assert the error state.

### `sanityRegression`

`Sanity` = must pass on every release; small set of green-light TCs across each tenant.
`Regression` = broader functional coverage; runs less often.
Empty / unset = older TCs without classification.

Add as `[Category("Sanity")]` or `[Category("Regression")]` on the test class.

### `executionEnv`

Observed values: `QA only`, `QA, UAT`, `QA, UAT, Staging`, `All env (QA,UAT,Prod)`. Drives `[RunIn(Environment.X)]` choice; multiple envs require multiple `[RunIn]` attributes or `[NotRunIn]` exclusions.

### `tcSource`

Observed values: `Feature` (most common), unset for older TCs. Internal classifier; doesn't affect automation.

### `featureFlag`

When present, indicates the TC requires a specific LaunchDarkly flag enabled. Example: `ba_commercial-lines-new-flow` (TC 236379). Reflect in the test:
- Check `LaunchDarklyHelper` (or equivalent) for the flag value before the assertion.
- If the test only matters when the flag is on, gate the test with `Assert.Inconclusive` if it's off.

### `tags`

Comma-separated free-form. Common tag groups:

- **Feature areas:** `ADBX`, `CRM`, `GetQuoteAPI`, `D2C`, `Interview`, `Quoting`.
- **Governance:** `Sanity`, `Regression`, `Production`, `QA Only`.
- **Domain:** `SMS`, `TCPA`, `lead`, `Case`, `Prefill`, `default`, `ESign`, `Account`.

Add the most relevant tags as `[Category("X")]` attributes.

### `links` — related work items

Each link entry: `{ rel, url, title, id, state, workItemType }`. Common `rel` values:

| `rel` | Meaning |
|---|---|
| `System.LinkTypes.Hierarchy-Reverse` | Parent (the TC's epic / feature work item). |
| `System.LinkTypes.Hierarchy-Forward` | Child (sub-TCs). |
| `System.LinkTypes.Related` | Related TCs (same feature, sibling assertions). |
| `System.LinkTypes.Dependency-Forward` | Task-to-automate this TC (often linked to an automation Story / Bug). |
| `Microsoft.VSTS.Common.TestedBy-Reverse` | Bugs that this TC catches. |
| `AttachedFile` | Files (request payloads, screenshots, evidence). |

When a TC is `state="Automated"`, find the test code by following `Dependency-Forward` to the Task and reading the linked PR / commit, or by searching the codebase for `[TestCaseId(<id>)]`.

## Sprint and area-path conventions

### Iteration path (`Epos\<sprint>`)

Sprint number is a 4-digit code (year + sprint within year). Examples in cache:

| Iteration path | Meaning |
|---|---|
| `Epos\2607` | Sprint 2607 (most recent in cache — May 2026 work) |
| `Epos\2606` | Previous sprint |
| `Epos\2605`, `Epos\2602`, `Epos\2512` | Earlier sprints |
| `Epos\RnD Archive\Sprint Backlog` | Uncommitted backlog |
| `Epos` (root) | Pre-sprint or ad-hoc |

### Area path (`Epos\RnD\<TeamLead>'s Team`)

Teams observed in the cache:

| Team | Lead | TCs in cache |
|---|---|---|
| `Epos\RnD\Abigail's Team` | Abigail | 5 |
| `Epos\RnD\Rina's Team` | Rina | 9 |
| `Epos\RnD\Pavlo's Team` | Pavlo | 4 |
| `Epos\RnD\Dmytro's Team` | Dmytro | 4 |
| `Epos\RnD\Nikita's Team` | Nikita | 1 |
| `Epos\RnD\Keren's Team` | Keren | 3 |
| `Epos\RnD\Automation Team` | (Automation team) | 1 |
| `Epos` (root only) | — | 7 |

When parsing the team out of `areaPath`, match `RnD\([^\\]+)`.

## TC-as-input checklist for Phase 1

Given a TC ID, the skill should produce a manifest with:

- [ ] Tenant (`partner` field → `Tenant` enum via [partners/INDEX.md](partners/INDEX.md))
- [ ] Environment (default `QA`, but check `executionEnv`)
- [ ] LOB (parse from title segment 3, see [lob/](lob/))
- [ ] Feature → FrontEnd (see [product-areas.md](product-areas.md))
- [ ] Flow candidate (Phase 2 confirms; this is a Phase 1 first-guess)
- [ ] Entry path (parse from `description` + `steps[0]`)
- [ ] Per-step page + fields (parse from `steps[]`)
- [ ] Expected outcome (from `steps[last].expectedResult` typically)
- [ ] Attributes mapping: `[Tenant]` ← partner, `[RunIn]` ← executionEnv, `[Author]` ← assignedTo (closest enum match), `[TestCaseId]` ← id, categories from tags + sanityRegression.

## Anti-patterns

- **Trusting the title to be exhaustive.** Some TCs are vague; the description and steps fill in the rest.
- **Treating `Pending Review` like `Pending Automation`.** Don't automate something that hasn't been reviewer-approved. If the user asks anyway, flag it.
- **Ignoring the `featureFlag` field.** A TC that depends on a flag fails inexplicably when the flag is off; always check.
- **Assuming the `assignedTo` person is the test author.** It's the current owner. If the TC has been reassigned, you don't get the original author. Use `createdDate` / `changedBy` for history.
