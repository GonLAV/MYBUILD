# Nexus Agent Knowledge Base

> **Generated mirror of [`index.yml`](index.yml).** Regenerate via `nexus-agent kb generate-index` (Phase 9 debug command). Do not edit by hand.
>
> **For agents:** use `nexus-agent kb lookup --topic <key>` to fetch a topic, `nexus-agent kb search <terms>` for keyword search, and `nexus-agent kb describe --file <path>` to inspect a leaf's headings.
>
> **Leaf status:** every file currently has `status: stub` frontmatter. Content is migrated in Phase 2b-2e of the agent-extension plan.

## Framework — how the framework hangs together

| Topic | Summary | File |
|---|---|---|
| `framework:overview` | How the framework wires up — DI flow, IScopeContext, page factories, base classes. | [framework/overview.md](framework/overview.md) |
| `framework:field-registry` | FieldRegistry mechanism — sparse dictionaries, Pages tagging, DefaultValue injection, MergeDataManager. | [framework/field-registry.md](framework/field-registry.md) |
| `framework:ui-field-types` | Taxonomy of UI field types (Input, Dropdown, NgSelect, Switcher, Checkbox) and registry strategy patterns. | [framework/ui-field-types.md](framework/ui-field-types.md) |
| `framework:page-objects` | PageBase contract, FillForm + ClickContinue, override patterns for custom fields. | [framework/page-objects.md](framework/page-objects.md) |
| `framework:flows-executor` | FlowType enum, FlowInitializer, PlaywrightExecutor — how flows chain pages from start to end. | [framework/flows-executor.md](framework/flows-executor.md) |
| `framework:test-class` | TestBase / UITestBase skeleton, `[Tenant]`/`[Category]` attributes, IScopeContext lifecycle, data builders. | [framework/test-class.md](framework/test-class.md) |
| `framework:injection-tests` | Runtime-injected (Professional Services) tests — InjectionTestBase/UIInjectionTestBase, INJECTED_* env var contract, multi-axis injected-parameter fan-out (state x LOB), InjectedParameterCatalog, D2CLobCatalog. | [framework/injection-tests.md](framework/injection-tests.md) |
| `framework:popups` | PopupBase + PopupFactory, dismiss/answer patterns, integration with PageHelper. | [framework/popups.md](framework/popups.md) |
| `framework:logging` | IAutomationLogger — StartStep, LogApiCallAsync, LogBusinessRule, LogDataValidation. | [framework/logging.md](framework/logging.md) |
| `framework:navigation-waits` | WaitForNavigationOrUrlContainsAsync returns bool but throws on timeout — never returns false; assert the URL, not the bool. | [framework/navigation-waits.md](framework/navigation-waits.md) |
| `framework:test-design` | Principles for designing tests — what to assert, how to scope, when to split. | [framework/test-design.md](framework/test-design.md) |
| `framework:accessibility` | axe-core scanning, keyboard traversal and screen-state enumeration — why a screen is not one scannable unit. | [framework/accessibility.md](framework/accessibility.md) |

## Domain — insurance + product vocabulary

| Topic | Summary | File |
|---|---|---|
| `domain:glossary` | Insurance + framework vocabulary — PA/HO3/HO6, FrontEnd, LOB, partner vs tenant, FieldRegistry, Flow, ADBX. | [domain/glossary.md](domain/glossary.md) |
| `domain:tc-conventions` | How TCs are titled and structured in ADO — tenant \| feature \| LOB \| scenario fragments. | [domain/tc-conventions.md](domain/tc-conventions.md) |
| `domain:tc-api` | TC fetch API semantics — NexusLogger endpoints, auth token, response shape. | [domain/tc-api.md](domain/tc-api.md) |
| `domain:product-areas` | Product areas — ADBX vs Interview vs D2C vs Partner Portal — which FrontEnd, which flows, which tenants. | [domain/product-areas.md](domain/product-areas.md) |

### Lines of business (one per `LobType` enum value)

| Topic | Summary | File |
|---|---|---|
| `lob:index` | LobType enum reference, title-fragment decoding map, cross-LOB patterns (bundles, CovMod, result-page forms). | [domain/lob/INDEX.md](domain/lob/INDEX.md) |
| `lob:auto` | Personal Auto (PA) — VIN decode, driver/vehicle fields, carrier-specific D2C flows (Bristol West, Safeco). | [domain/lob/auto.md](domain/lob/auto.md) |
| `lob:home` | Homeowners (HO3) — dwelling type, construction, roof, prior claims, dog breeds. | [domain/lob/home.md](domain/lob/home.md) |
| `lob:renters` | Renters (HO4) — personal property, deductible, tenant-only fields. | [domain/lob/renters.md](domain/lob/renters.md) |
| `lob:condo` | Condo (HO6) — HO3 fields plus high-rise toggle, roof responsibility, floor numbers. | [domain/lob/condo.md](domain/lob/condo.md) |
| `lob:dwelling-fire` | Dwelling Fire (DF) — non-standard residential (rental property, vacant); no TCs in current sample. | [domain/lob/dwelling-fire.md](domain/lob/dwelling-fire.md) |
| `lob:umbrella` | Personal liability excess coverage stacked over Auto/Home; no TCs in current sample. | [domain/lob/umbrella.md](domain/lob/umbrella.md) |
| `lob:wc` | Workers Compensation — business profile, class codes, payroll; monopolistic-state blocks in OH/ND/WA/WY. | [domain/lob/wc.md](domain/lob/wc.md) |
| `lob:gl` | General Liability — typically bundled into BOP; standalone TCs rare. | [domain/lob/gl.md](domain/lob/gl.md) |
| `lob:cl-auto` | Commercial Auto — FEIN, NAIC code, business + operator + vehicle fields; KLX-specific Cert / DeclinationReason. | [domain/lob/cl-auto.md](domain/lob/cl-auto.md) |
| `lob:bop` | Business Owners Policy — business contact, property, market appetite, Acord form generation. | [domain/lob/bop.md](domain/lob/bop.md) |
| `lob:flood` | Flood — property + flood zone + elevation cert; NFIP vs private. | [domain/lob/flood.md](domain/lob/flood.md) |
| `lob:earthquake` | Earthquake — seismic damage coverage; no TCs in current sample. | [domain/lob/earthquake.md](domain/lob/earthquake.md) |
| `lob:motorcycle` | Motorcycle — powersports flow; no TCs in current sample. | [domain/lob/motorcycle.md](domain/lob/motorcycle.md) |
| `lob:device-protection` | Device Protection — electronics coverage; no TCs in current sample. | [domain/lob/device-protection.md](domain/lob/device-protection.md) |
| `lob:pets` | Pets — species, breed, age, bite history; Progressive dog-breed TCs (Cane Corso etc.). | [domain/lob/pets.md](domain/lob/pets.md) |

## Partner tenants

| Topic | Summary | File |
|---|---|---|
| `partner:index` | Partner tenants overview — which partners exist, what FrontEnds they use, where data lives in TestDataStore. | [domain/partners/INDEX.md](domain/partners/INDEX.md) |
| `partner:boltag` | BoltAG — primary white-label tenant; supports most products across Interview and D2C. | [domain/partners/boltag.md](domain/partners/boltag.md) |
| `partner:kraftlakex` | KraftlakeX (KLX) — CL/BOP focused; Cert/DeclinationReason quirks, class collapsing, app-form-label overlay. | [domain/partners/kraftlakex.md](domain/partners/kraftlakex.md) |
| `partner:unify` | Unify — interview frontend; WC market-availability handling for monopolistic states. | [domain/partners/unify.md](domain/partners/unify.md) |
| `partner:progressivepl` | ProgressivePL (PGR) — HQX 2.0 flows, HO3/HO6 product switching, Plymouth Rock CovMod. | [domain/partners/progressivepl.md](domain/partners/progressivepl.md) |
| `partner:pgr-quote-status-polling` | PGR CheckQuoteStatus — why QuoteStatusWithPollingAsync makes a following status assert tautological; read until settled instead. Plus MsgStatusCd / redirect-bool / Polly traps. | [domain/partners/pgr-quote-status-polling.md](domain/partners/pgr-quote-status-polling.md) |
| `partner:pgr-covmod-wind-hail-na` | PGR CovMod — when a carrier returns Wind/Hail as Not-Applicable it mirrors the Standard deductible and renders no editable dropdown (flag ba-homesite-wind-hail-na). | [domain/partners/pgr-covmod-wind-hail-na.md](domain/partners/pgr-covmod-wind-hail-na.md) |
| `partner:usaa` | USAA — SSO-driven, hardware-key login, Bristol West Auto carrier. | [domain/partners/usaa.md](domain/partners/usaa.md) |
| `partner:comparion` | Comparion — Personal Lines (Auto/Home); Fenris prefill flow. | [domain/partners/comparion.md](domain/partners/comparion.md) |
| `partner:libertyx` | LibertyX (LMX) — partner portal entry, agent-driven quoting. | [domain/partners/libertyx.md](domain/partners/libertyx.md) |
| `partner:boltaccess` | BoltAccess — admin tooling and case management surfaces. | [domain/partners/boltaccess.md](domain/partners/boltaccess.md) |

## Recipes — pattern libraries

| Topic | Summary | File |
|---|---|---|
| `recipe:locator-recipes` | Pattern library for locator strategies — input vs ng-select, label-vs-control, XPath OR for tenant variants. | [recipes/locator-recipes.md](recipes/locator-recipes.md) |
| `recipe:override-patterns` | Page-object override patterns A-G — conditional reveal, click-overlay, JS-fill, arrow-wrapper, first-if-missing. | [recipes/override-patterns.md](recipes/override-patterns.md) |
| `recipe:test-review-rubric` | Ten-dimension rubric for reviewing automation test PRs (forward-port from V1 test-review skill). | [recipes/test-review-rubric.md](recipes/test-review-rubric.md) |
| `recipe:qa-scenario-format` | Declarative YAML scenario files for semi-manual QA sessions — schema, local-only storage, replay semantics, one step type per browser CLI verb. | [recipes/qa-scenario-format.md](recipes/qa-scenario-format.md) |
| `recipe:recording-cleanup` | Distill a Playwright codegen recording of exploratory QA testing into a clean replayable scenario — noise heuristics (toggle pairs, refills, dead ends), registry mapping, and the mandatory intent interview. | [recipes/recording-cleanup.md](recipes/recording-cleanup.md) |
| `recipe:defaults-service-testing` | How to test field defaults — lint the rule table for invariants, evaluate the matrix through the stateless GetDefaults endpoint, anchor with a few quotes, monitor two Splunk warnings. | [recipes/defaults-service-testing.md](recipes/defaults-service-testing.md) |
| `recipe:ui-only-entry-point-coverage` | A test that starts from a blank UI account must answer every field the API-seeded flows get from their payload — pre-flight the registry against the seed data. | [recipes/ui-only-entry-point-coverage.md](recipes/ui-only-entry-point-coverage.md) |

### Troubleshooting — symptom → cause → fix recipes

| Topic | Summary | File |
|---|---|---|
| `troubleshooting:index` | Troubleshooting recipe book — symptom → likely cause → fix; jump to matching section after page_source review. | [recipes/troubleshooting/INDEX.md](recipes/troubleshooting/INDEX.md) |
| `troubleshooting:field-not-found` | "Could not find UIElement for field X" — registry alias / wrong FrontEnd / Pages doesn't include current page. | [recipes/troubleshooting/field-not-found.md](recipes/troubleshooting/field-not-found.md) |
| `troubleshooting:locator-mismatch` | Page validates but field fails to fill — tenant class scheme differs, sibling lookalike, wrong element type. | [recipes/troubleshooting/locator-mismatch.md](recipes/troubleshooting/locator-mismatch.md) |
| `troubleshooting:default-reinjected` | Field shows registry default after you set a value — MergeDataManager injected it. | [recipes/troubleshooting/default-reinjected.md](recipes/troubleshooting/default-reinjected.md) |
| `troubleshooting:strict-mode-multimatch` | Playwright strict mode violation: locator resolves to N elements; use `.First` or scope to container. | [recipes/troubleshooting/strict-mode-multimatch.md](recipes/troubleshooting/strict-mode-multimatch.md) |
| `troubleshooting:conditional-reveal` | Field exists in registry but not in DOM until trigger; override FillForm to fill trigger first. | [recipes/troubleshooting/conditional-reveal.md](recipes/troubleshooting/conditional-reveal.md) |
| `troubleshooting:continue-button-no-match` | `ClickContinueButton`: no selector matches; add tenant-specific class to `InterviewBase` or override `ClickContinue`. | [recipes/troubleshooting/continue-button-no-match.md](recipes/troubleshooting/continue-button-no-match.md) |
| `troubleshooting:page-validation-timeout` | `ValidatePageReadyAsync` times out — `PageIdentifier` substring is too narrow or wrong. | [recipes/troubleshooting/page-validation-timeout.md](recipes/troubleshooting/page-validation-timeout.md) |
| `troubleshooting:popup-interception` | Click intercepted by overlay label/span; click the overlay element itself. | [recipes/troubleshooting/popup-interception.md](recipes/troubleshooting/popup-interception.md) |
| `troubleshooting:typeahead-focus-race` | ng-select typeahead reverts after click; let the next field's click commit the previous typeahead. | [recipes/troubleshooting/typeahead-focus-race.md](recipes/troubleshooting/typeahead-focus-race.md) |
| `troubleshooting:disabled-button-click` | Click does nothing because button is disabled — press Tab to blur input and run validators. | [recipes/troubleshooting/disabled-button-click.md](recipes/troubleshooting/disabled-button-click.md) |
| `troubleshooting:tab-switch-ordering` | After ADBX popup adds a tab, switch to last tab + set FrontEnd before `CreatePage<T>`. | [recipes/troubleshooting/tab-switch-ordering.md](recipes/troubleshooting/tab-switch-ordering.md) |
| `troubleshooting:field-validation-blocks-continue` | Continue blocked by red validation — `DefaultValue` placeholder failed tenant validation. | [recipes/troubleshooting/field-validation-blocks-continue.md](recipes/troubleshooting/field-validation-blocks-continue.md) |
| `troubleshooting:vin-decode-flake` | VIN decode signal never arrives; catch `TimeoutException` + warn + let downstream fields fail loudly. | [recipes/troubleshooting/vin-decode-flake.md](recipes/troubleshooting/vin-decode-flake.md) |
| `troubleshooting:logger-method-naming` | `_logger?.Warn` doesn't exist — use `Warning`. Same for Info/Error/Debug/Fatal. | [recipes/troubleshooting/logger-method-naming.md](recipes/troubleshooting/logger-method-naming.md) |
| `troubleshooting:test-body-logging-missing-steps` | Test body uses `Info()` instead of `StartStep` — report has no step scopes. | [recipes/troubleshooting/test-body-logging-missing-steps.md](recipes/troubleshooting/test-body-logging-missing-steps.md) |
| `troubleshooting:break-fix-break-loop` | Two unrelated fixes interfere with each other; revert, apply one fix per iteration, commit between. | [recipes/troubleshooting/break-fix-break-loop.md](recipes/troubleshooting/break-fix-break-loop.md) |
| `troubleshooting:div-not-supported-dropdown` | "tag `div` not supported dropdown type" — locator ends on inner div; switch end-anchor to `//ng-select`. | [recipes/troubleshooting/div-not-supported-dropdown.md](recipes/troubleshooting/div-not-supported-dropdown.md) |
| `troubleshooting:dropdown-option-missing` | Dropdown opens but option text not found — option varies by carrier; use `firstIfMissing` fallback. | [recipes/troubleshooting/dropdown-option-missing.md](recipes/troubleshooting/dropdown-option-missing.md) |
| `troubleshooting:interactwithfield-throws-on-missing` | Callback dies on a field FillForm skipped happily — InteractWithField forces `IgnoreIfNotFound = false`. | [recipes/troubleshooting/interactwithfield-throws-on-missing.md](recipes/troubleshooting/interactwithfield-throws-on-missing.md) |
| `troubleshooting:reading-captured-page-source` | Which signals in a captured interview page_source are trustworthy — radio attributes and `ng-invalid` are not. | [recipes/troubleshooting/reading-captured-page-source.md](recipes/troubleshooting/reading-captured-page-source.md) |
| `troubleshooting:all-tests-skip-vpn` | Every test skips, even ungated ones — corporate VPN is down; check DNS before debugging `RunIn`. | [recipes/troubleshooting/all-tests-skip-vpn.md](recipes/troubleshooting/all-tests-skip-vpn.md) |
| `troubleshooting:floated-label-interception` | `<app-form-label>` intercepts pointer events on ng-select; click `.ng-arrow-wrapper` instead. | [recipes/troubleshooting/floated-label-interception.md](recipes/troubleshooting/floated-label-interception.md) |
| `troubleshooting:mat-datepicker-fillasync` | `FillAsync` rejected on mat-datepicker hidden input; JS set + dispatch input/change/blur events. | [recipes/troubleshooting/mat-datepicker-fillasync.md](recipes/troubleshooting/mat-datepicker-fillasync.md) |
| `troubleshooting:conditional-field-not-rendered` | Override skip log "container not present" — field is conditionally rendered, order trigger fields first. | [recipes/troubleshooting/conditional-field-not-rendered.md](recipes/troubleshooting/conditional-field-not-rendered.md) |
| `troubleshooting:reveal-on-answer-questionset` | Question-set validation shows 'extra' sub-fields; expected list undercounts because children reveal only after a parent is answered — trust the live run, not static DOM or the manual TC list. | [recipes/troubleshooting/reveal-on-answer-questionset.md](recipes/troubleshooting/reveal-on-answer-questionset.md) |

## Philosophy — design principles guiding the framework

| Topic | Summary | File |
|---|---|---|
| `philosophy:index` | Design principles guiding the framework — read first when reviewing changes that touch shared abstractions. | [philosophy/INDEX.md](philosophy/INDEX.md) |
| `philosophy:sparse-dictionaries` | Why FieldRegistry is sparse and Pages-tagged — agent fills only what's relevant, defaults handle the rest. | [philosophy/sparse-dictionaries.md](philosophy/sparse-dictionaries.md) |
| `philosophy:logging-where-work-happens` | Logging lives in page objects + API clients, never in test methods — keeps tests as orchestration. | [philosophy/logging-where-work-happens.md](philosophy/logging-where-work-happens.md) |
| `philosophy:fluent-page-objects` | Page objects expose intent (FillField, ClickContinue), not Playwright primitives — tests stay readable. | [philosophy/fluent-page-objects.md](philosophy/fluent-page-objects.md) |
| `philosophy:tests-stay-clean` | Test methods are high-level step orchestration + business validations; no Playwright calls, no DOM grep. | [philosophy/tests-stay-clean.md](philosophy/tests-stay-clean.md) |
| `philosophy:compact-comments` | Comments explain the non-obvious why, compactly; no step narration; wide-relevance knowledge goes to the KB, not inline. | [philosophy/compact-comments.md](philosophy/compact-comments.md) |
| `philosophy:when-to-automate` | Deciding which TCs are worth automating — value, churn, flake risk, manual cost. | [philosophy/when-to-automate.md](philosophy/when-to-automate.md) |
| `philosophy:design-decisions` | ADR placeholder — records the why behind major framework decisions (append-only). | [philosophy/design-decisions.md](philosophy/design-decisions.md) |
