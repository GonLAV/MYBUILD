---
topic: domain:glossary
summary: Insurance + framework vocabulary — PA/HO3/HO6, FrontEnd, LOB, partner vs tenant, FieldRegistry, Flow, ADBX.
status: ready
---

# Domain glossary

> **When to read:** Phase 1 (decoding TC titles + step text), and any time the user uses an acronym you don't recognize.

A future agent reading "KLX | Interview E2E | CL Auto | old interview, submitting quote" should resolve every token without asking. This file is the lookup.

## Insurance vocabulary

| Term | Expansion | Example | Source TC |
|---|---|---|---|
| LOB | Line of Business | "Select 'Commercial Auto' lob and proceed to 'Applicant and Drivers'" | 236743 |
| PA | Personal Auto | (LobType.Auto) — see [lob/auto.md](lob/auto.md) | — |
| HO3 | Homeowners 3 (standard dwelling form) | "Start new HO3 Quote" | 242142 |
| HO4 | Homeowners 4 (Renters / tenant form) | (LobType.Renters) | — |
| HO6 | Homeowners 6 (Condo / Townhome) | "Start a new Consumer HO6 quote for tenant PROGRESSIVEPL" | 241340 |
| BOP | Business Owners Policy | "Select 'BOP' lob and press next" | 236379 |
| CL | Commercial Lines | "Login to BoltAG and start CL quote" | 236743 |
| WC | Workers Compensation | "select WC LOB and proceed to the Market availability page" | 235414 |
| GL | General Liability | (commercial liability LOB; see [lob/gl.md](lob/gl.md)) | — |
| Auto | Personal automobile insurance | "Adding Bristol West OLB flow to the Auto D2C" | 235870 |
| Flood | Flood insurance | (federal/private; LobType.Flood) | — |
| Umbrella | Personal liability umbrella policy | (LobType.Umbrella) | — |
| FQ | Full Quote (price-bindable, after rate selection) | "BOLTAG \| D2C Safeco \| Verify Driver License Masking for All Drivers (FQ)" | 242399 |
| OLB | Online Bind / Online Enrollment | "Adding Bristol West OLB flow to the Auto D2C" | 235870 |
| NB | New Business | (lifecycle term — opposite of renewal) | (no example in current sample) |
| RW | Rewrite / Renewal | (lifecycle term) | (no example in current sample) |
| Acord | ACORD insurance form standard (e.g. ACORD 125, 126, 140) | "Press on 'ACORD' tab… ACORD 125, ACORD 126, ACORD 140" | 236379 |
| CovMod | Coverage Modification (carrier-specific overrides) | "ABTest-cvg-mod-exp flag… to control the Plymouth Rock Cov Mod" | 240777 |
| VIN | Vehicle Identification Number | "VIN was not found. Please verify the VIN number is correct" | 239923 |
| NAIC | National Association of Insurance Commissioners (industry classification code) | "Naic (Industry) 4411000 — New Car Dealers" | 236743 |
| FEIN | Federal Employer Identification Number (9 digits) | (CL Auto Business page; see [partners/kraftlakex.md](partners/kraftlakex.md)) | 240782 |
| TCPA | Telephone Consumer Protection Act (US consent law) | "Applicant with the TCPA No — Check the Send SMS Disabled" | 235695 |
| CCPA | California Consumer Privacy Act | (no example in current sample) | — |
| FNOL | First Notice of Loss | (no example in current sample) | — |
| AOP | All Other Perils (deductible category) | (no example in current sample) | — |
| W/H | Wind / Hail (deductible category) | (no example in current sample) | — |
| SR22 | SR-22 form (high-risk driver filing) | (Operator page field) | (no example in current sample) |
| FCRA | Fair Credit Reporting Act | (no example in current sample) | — |

## Bolt-specific platform jargon

| Term | Expansion | Example | Source TC |
|---|---|---|---|
| ADBX | Application Data Box (agent dashboard / back-office) | "go to ADBX(unify qa), log in as agent" | 235414 |
| CRM | Customer Relationship Management (ADBX sub-area: leads, accounts) | tags: "ADBX; CRM; default" | 241459 |
| D2C | Direct to Consumer (consumer-facing quote flow) | "Adding Bristol West OLB flow to the Auto D2C" | 235870 |
| Interview | Agent-facing guided quote interview UI | "BOLTAG \| Interview \| Commercial Auto" | 236743 |
| GQ / GetQuoteAPI | Backend quote-generation API | "from GQ send PL auto request" | 145915 |
| HQX | Homeowner Quick Quote (UI generation) — Consumer + Agent variants | "PGR \| HQX 2.0 \| HO6 \| …" | 241340 |
| MPQ3 | Multi-Page Quote v3 (Renters quick-quote variant in HQX Consumer) | (FlowType reference) | (no example in current sample) |
| KLX | Kraftlake (Bolt X) — partner shorthand | "Access KLX flow, use the user lsp1_aortest@test.com" | 240782 |
| LSP | Licensed Service Provider (agent business model) | "QA User: LSP_B3Ag1Aor1@tes.com" | 237437 |
| AOR | Area of Responsibility (LSP group identifier) | "group: AOR-TEST" | 240782 |
| INET | Internet (consumer access channel — Progressive context) | "PGR \| INET \| Accessibility — Zooming/Fluid UI response" | 238832 |
| DHUB | Carrier integration hub (Progressive's carrier-side service) | "Run attached request on DHUB — CL — AUTO (UNIFY)" | 239923 |
| MarketsLib | Markets library (carrier/product configuration database) | "Go to ADBX (Unify Marketslib)" | 241435 |
| Default tool | Admin feature for tenant-default values (ADBX) | "ADBX \| Default tool \| Enable additional users with CRUD Permissions" | 241459 |
| Provision | Platform-side credential / wholesaler setup | "Product \| Platform API — Credentials API \| Check update wholesaler credentials" | 235402 |
| Sales case | ADBX case-management entity (post-quote) | "KLX \| ADBX to CM \| Sales case \| Create" | 197386 |
| CM / CMAPI | Case Manager / Case Manager API | "KLX \| CMAPI \| Sales case \| KLX login" | 198617 |
| ESign / E-sign | Electronic signature step (DocuSign) in D2C bind flow | "Bristol West OLB flow… E-sign" | 235870 |
| SSO | Single Sign-On (USAA, Comparion) | "USAA \| SSO \| Agents \| SSO into the interview" | 212329 |
| STS & Login | Security Token Service / login feature area | (feature name in 200974, 212329) | 200974 |
| Appetite | Carrier rules for which states/risks are eligible | (CL flow; varies per state, see 236379 AK appetite) | 236379 |
| Application forms | The Acord-format printable bind packet | "Kraftlake \| Agent Interview \| Personal Home \| Result page \| Application forms E2E" | 235615 |
| Offline request | Submit quote offline when carrier markets are unavailable | "KraftlakeX \| CL \| BOP \| Markets results \| CL_offline page \| Offline request" | 237437 |
| PaaS | Progressive PaaS (carrier integration) | (carrier-helper code reference) | (no example in current sample) |
| ABTest | A/B test feature flag prefix (e.g. `ABTest-cvg-mod-exp`) | (Plymouth Rock CovMod flag) | 240777 |

## Carriers / vendors named in TC titles

| Name | Role | Source TC |
|---|---|---|
| Bristol West | Auto carrier (D2C OLB) | 235870, 237026 |
| Plymouth Rock | Personal-line carrier (covered by CovMod flags) | 240777 |
| Safeco | Auto carrier (D2C FQ) | 242399 |
| Stillwater | Home carrier (D2C) | (FlowType.StillwaterHomeFQFlow) |
| Lemonade | Renters/Pets carrier (D2C) | (FlowType.LemonadeFQFlow) |
| USAA | Auto + bundle carrier (SSO + D2C) | 212329, 162448 |
| Fenris | Prefill vendor (Comparion uses for Auto prefill) | 145915 |
| DocuSign | E-signature provider (D2C bind step) | 237026 |
| Acord | Standardized commercial application forms (used in BOP result) | 236379 |

## Process / Azure DevOps

| Term | Meaning | Notes |
|---|---|---|
| TC | Test Case (ADO work item type) | Universally used |
| Sprint (Epos\NNNN) | Iteration in `Epos\<sprint-number>` format (e.g. `Epos\2607`) | Sprint number is a 4-digit code, generally year+number |
| Sprint Backlog | `Epos\RnD Archive\Sprint Backlog` — uncommitted backlog | Ad-hoc parking |
| Area path | `Epos\RnD\<TeamLead>'s Team` — team ownership | Teams in current sample: Abigail's, Rina's, Pavlo's, Dmytro's, Nikita's, Keren's, Automation |
| Sanity | Core happy-path coverage; must pass on every release | Field `sanityRegression="Sanity"` — TC 240782, 241459 |
| Regression | Broader functional coverage | Field `sanityRegression="Regression"` — TC 197386, 241340 |
| tcType | Test classification | Observed values: `E2E Test`, `API Test`, `UI/UX Test`, `Negative Test` |
| State lifecycle | TC progress through approval+automation | Observed states (corpus): `Design`, `Pending Review`, `Approved`, `Pending Automation`, `Automated`, `Ready`. Spec also lists `Deprecated`. See [tc-conventions.md](tc-conventions.md) for transitions. |
| executionEnv | Where the TC must run | Observed values: `QA only`, `QA, UAT`, `QA, UAT, Staging`, `All env (QA,UAT,Prod)` |
| tcSource | Origin classifier | Observed: `Feature` (most common), unset for older TCs |
| Feature flag | Runtime toggle (`featureFlag` field) | Example: `ba_commercial-lines-new-flow` (TC 236379) |
| TestCaseId | The numeric ADO work item ID — the same number used in `[TestCaseId(N)]` attribute on the NUnit test | TC 240782 → `[TestCaseId(240782)]` |

## Carrier-integration phases

Some titles reference phase codes:

| Phase | Meaning (inferred from titles) | Example |
|---|---|---|
| B2 | Carrier integration — phase 2 (Interview relevancy) | "QA \| New Carrier Integration \| B2 — Implementation \| Interview Relevancy" (229935) |
| B3 | Carrier integration — phase 3 (mappings) | "QA \| New Carrier Integration \| B3 — Implementation \| Mappings" (229559) |

These appear only on Unify (Bolt X) carrier-integration TCs in the current sample.

## Anti-patterns when reading the glossary

- **Don't trust unfamiliar acronyms blindly.** If a TC title uses a term not on this list (e.g. a new carrier or product code), open the TC's `description` and `steps` first to disambiguate. Add the term here when you confirm it.
- **Don't conflate similar names.** "Kraftlake" and "KraftlakeX" both refer to the same partner enum value (`KRAFTLAKEX`). "PGR" and "Progressive" are the same. "BOLTAG" and "Bolt AG" and "Boltag" all map to `BOLTAG`. The variation is purely cosmetic in titles.
- **Don't assume an LOB from the partner.** Progressive has personal lines and consumer flows; Bolt AG has commercial. Always read the LOB segment of the title or the `description`.
- **Don't take "TCPA Yes/No" as a personal choice.** It refers to whether the applicant gave TCPA consent — drives whether SMS/auto-call channels can be used.

## Adding terms to this file

When you encounter a new acronym in a TC, add it here with: term → expansion → real example sentence (cite the TC ID) → source-of-truth pointer if applicable. Don't add definitions without an example unless the term is in `Bolt.Automation.Common/Enums.cs`.
