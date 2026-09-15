---
name: nexus-test-author
description: Implement a manually-written test case (TC) end-to-end as an NUnit test inside the nexus framework, from a numeric ADO TC id, pasted text + screenshots, or a TC file (md/docx/pdf). Drives a parse → map → scaffold → iterate → verify loop using framework idioms (FieldRegistry, FlowType, page objects, IScopeContext) and the agent-knowledge base. Trigger when the user hands you a TC and asks for an automated test that walks it to completion. Do NOT trigger for code review, framework refactors, or debugging an existing failing test (use nexus-test-review / nexus-framework-review / nexus-debug).
---

# nexus-test-author

Turn a manual test case into a working NUnit test that drives the nexus framework end-to-end. Author with the framework's idioms; don't reinvent. This skill *navigates* knowledge; it does not carry it. Read KB leaves on demand with `nexus-agent kb`.

## Doctrine: ask, don't assume

Pausing to ask is the protocol, not a failure. Stop and ask the user when:

- The tenant/partner is unknown (no `kb` hit for it).
- A TC step maps ambiguously to a field/flow and two readings both look plausible.
- You've iterated 3 times on the same symptom without progress.
- Each run advances exactly one page. The symptom changes every run so the 3-strikes rule never
  fires, while the cause never changes. Stop and do the Phase 2 coverage sweep instead of iterating.
- The TC needs real PII, an SSO hardware key, or a captcha (hand the keyboard back).

Use `AskUserQuestion` for quick clarifications; `nexus-agent code wip-stop --note "<where I am>"` to checkpoint a strategic pause.

## CLI commands this skill uses

> **Invoking `nexus-agent`:** if it isn't on PATH, use the built binary directly: `Bolt.Automation.AgentTools/bin/Debug/net10.0/nexus-agent.exe` (build first if missing: `dotnet build Bolt.Automation.AgentTools`). Ignore the project README's "Phase 1 scaffold / not-implemented" note; it's stale. All verbs (`kb`/`tc`/`failure`/`code`/`browser`/`philosophy`/`secrets`) are live.
>
> **Do NOT use the `ado` MCP** (`mcp__ado__*`) to fetch TCs or attachments. It's unoptimized and burns tokens. Pasted TC content is the source of truth; otherwise use the `nexus-logger` MCP (`get_test_case` / `search_test_cases` / `get_bugs_for_test_case`) when connected (no token dance), else `nexus-agent tc fetch`.

| Need | Command |
|---|---|
| Fetch the TC body | `nexus-agent tc fetch <id>` (token via `tc auth set-token`) |
| Look up framework knowledge | `nexus-agent kb lookup --topic <key>` · `kb search "<terms>"` |
| Resolve a UI field to its registry entry | `nexus-agent code field-lookup <Name>` |
| Get a flow's page sequence | `nexus-agent code flow-trace --flow <FlowType>` |
| Find a sibling test/page to copy | `nexus-agent code find-similar --pattern <Symbol>` |
| Checkpoint a pause | `nexus-agent code wip-stop --note "<text>"` |
| Diagnose a run that fails | `nexus-agent failure summarize --test <FQN>` |

## The loop

### Phase 1: parse (build the manifest)

Get the TC body:

1. Numeric id / ADO URL: `nexus-agent tc fetch <id>`. On `no_token`/`unauthorized`, ask the user to run `tc auth set-token --token <jwt>` (Azure AD JWT, ~65 min life), then retry.
2. Pasted text + screenshots: read screenshots with the multimodal Read tool.
3. File: `.docx`/`.pdf`/`.xlsx` via the `anthropic-skills` doc skills; `.md`/`.txt` via Read.

Read first: `kb lookup --topic domain:tc-api`, `domain:tc-conventions`, `domain:glossary`. Extract into a manifest: tenant, product/LOB, flow, entry URL, per-step field interactions, expected results. Show the manifest to the user and ask for corrections before writing code.

### Phase 2: map (TC steps to framework constructs)

- Confirm the tenant is known: `kb search "<tenant>"`. No hit? Stop and ask.
- Resolve the product/LOB and flow: `kb search "<lob>"`, then `code flow-trace --flow <FlowType>` for the ordered pages.
- For each TC field, `code field-lookup <Name>` returns the `FieldRegistry` entry (strategy, FieldType, Pages, DependsOn). Read `kb lookup --topic framework:field-registry` and `framework:ui-field-types` to choose interactions.
- **Before inventing a new field name, check whether an existing entry already covers the page.** `code field-lookup` only resolves a name you already guessed; it won't tell you a generic entry (e.g. a shared address or annual-sales field) is already tagged for your target page type with a broader locator. Grep the registry for the target page type (`Pages = [..., typeof(<YourPage>)`) and for the DOM class substring you're about to encode; if a hit exists, extend its `Pages` array instead of adding a new key (`framework:field-registry` anti-patterns).
- Read `kb lookup --topic framework:flows-executor` (how `Execute<TStart,TEnd>` walks a flow) and `framework:page-objects`.

- **Does the test create its quote through the UI** (ADBX New Quote, blank account) rather than
  `CreateApplication`/`GetQuestionnaire`? Then run the pre-flight in
  `kb lookup --topic recipe:ui-only-entry-point-coverage` before the first run. API-seeded siblings
  answer half the interview from their payload, so registry gaps and invalid `DefaultValue`s stay hidden
  until a blank-account test hits them one page at a time. TC 252328 burned five runs learning this.
- **Before concluding a field "has no coverage", check how the sibling tests satisfy that same page.**
  Usually the answer is "their payload seeds it", which changes the fix: an untagged entry driven
  explicitly, not a page-tagged one that would overwrite those payloads.
- **A journey is a `FlowType`, not a pile of `pagesToSkip`/`pagesToAdd`.** If you are describing a real
  entry point (agent-from-ADBX vs consumer-from-API), add a flow to `Flows.cs` and name it in the
  `FlowType` enum. The enum already distinguishes variants (`InterviewCLAutoFlow` vs
  `InterviewBoltAccessCLAutoFlow`, `InterviewHO3Flow` vs `InterviewHO3AgentFlow`).

### Phase 3: scaffold

- Copy the closest sibling: `code find-similar --pattern <SiblingTestOrBase>`; read it. Mirror its base class (`TestBase`/`UITestBase`) and structure.
- **Professional Services / runtime-injected TC?** (new or unonboarded tenant or environment, values supplied as `INJECTED_*` env vars, no static data-store entry): the base is `UIInjectionTestBase`/`InjectionTestBase`, not `UITestBase`. Read `kb lookup --topic framework:injection-tests` FIRST. It covers the env-var contract (`RequiredSections`), the no-`[Tenant]` rule, and the multi-axis `[InjectedParameter]` pattern (one marker per axis, e.g. state x LOB; anchor TestCaseId, Ignore-default, orchestrator fan-out).
- **Prefer a permutation over a new method.** If the closest sibling is a parameterized test (`[TestCaseSource]`) and your TC differs only by data or a single divergent step, add a `TestCaseData` row to its source rather than cloning the method. Push the one divergence into the shared helper or page object (see next point). Cloning a ~70-line method that differs by one line is the wrong default; it duplicates the flow and the two copies drift.
- **Express per-case variation as data, not `if/else` in the test.** When cases differ by carrier, tenant, or state, put the difference in a data map (e.g. a `Dictionary<CarrierEnums, …>` next to the page object) or a helper method, never a branch in the test body (`kb lookup --topic philosophy:tests-stay-clean`). Consume the map by answering only the fields it specifies. Do not route a sparse, all-fields-tagged-to-one-page registry (e.g. carrier-questions) through a full `FillForm`; that also fills defaulted fields and can invalidate the form. (If you find yourself wanting `FillForm` to fill a subset, that's an infrastructure change to raise with the user, not to hack around.)
- **Conform to sibling data/attribute format.** Match how sibling cases already model their data-store values and `[RunIn]`/`[NotRunIn]` gating; don't invent a stricter variant. Example: if sibling carriers use a short host-fragment bridge URL that runs in every env, use the same, not a full URL plus `[NotRunIn(Production)]`.
- Read `kb lookup --topic framework:test-class`; follow `Bolt.Automation.Tests/CLAUDE.md` for attributes and the NUnit parameterized conventions (serializable args only). Every test carries `[Author(Author.<name>)]`. It's a required attribute, not optional; if you don't know who to attribute it to, ask (don't omit it).
- UI field interactions go through `IPageHelper` with string field-name constants. Never `UIElement` objects, never XPath, never `Thread.Sleep`.
- Logging goes in page objects and helpers, not the test body (`kb lookup --topic philosophy:tests-stay-clean`).

### Phase 4: iterate

- `dotnet build Bolt.Automation.sln`, then run the single test (`dotnet test --filter "FullyQualifiedName~<Name>"`).
- **Secrets are a prerequisite for a real UI run.** If `BOLT_SECRETS_PATH` is unset or the run fails with a secret/`IOptions` error (e.g. `LaunchDarkly SDK key is not configured`, connection strings null, or an empty API key, password, or auth header causing a 401), invoke `/nexus-secrets` before iterating. A red run from missing secrets is a setup gap, not a test bug.
- **New credentials never go in source.** If your TC needs a new user, API key, or Twilio value, `UserDataStore`/`TwilioDataStore` hold structural fields only (username, role, phone, URL, SSO). The credential (password, ApiKey, OAuthToken, AgentIdentity, ApiSource, AuthToken, AccountSid) lives in the secrets bundle under `environments.<env>.userSecrets.<TENANT>.<Role>` / `.twilio.<TENANT>` and is overlaid at read-time. Add the structural shape in code and the credential to the bundle; then `/nexus-secrets` (section "Where each secret lives") covers testing the local copy and pushing it to the durable AWS secret. Never hardcode a credential to make a test pass.
- On failure, in this order. Do not form a hypothesis before step 2:
  1. `nexus-agent failure summarize --test <FQN>`, then `failure page-source --test <FQN> --scope form`.
  2. `nexus-agent kb search "<the error text>"`. The leaf usually exists and names the fix
     (`dropdown-option-missing`, `conditional-field-not-rendered`, `field-not-found`,
     `interactwithfield-throws-on-missing`, `reading-captured-page-source`, `default-reinjected`,
     `strict-mode-multimatch`, `conditional-reveal`, …). Skipping this step is what turns a
     two-run fix into a five-run one.
  0. **Element missing? Establish which kind first.** *Conditional* (absent from the DOM for this quote):
     gate with `_pageHelper.ElementExists(field, timeout)` before interacting. *Late* (renders only
     after the answer it depends on): fill it from an AfterFillForm callback. A bigger timeout helps
     only the second kind, and an explicit `InteractWithField` throws on absence either way because both
     overloads force `IgnoreIfNotFound = false` (`troubleshooting:interactwithfield-throws-on-missing`).
  3. Only then reason from the DOM, and read it per
     `kb lookup --topic troubleshooting:reading-captured-page-source`, because radio state and
     `ng-invalid` are not readable from the captured HTML.
- **Element missing is not element late.** Check whether the control exists in the capture at all before
  reaching for a longer timeout; a timeout cannot help a control that never renders, and a skip-if-absent
  guard cannot help one that renders late. Model a real parent-child dependency with `DependsOn` plus an
  `InteractionOptions.Timeout` on the registry entry.
- **Trust the run over static analysis for question-set and expected-data validations.** A live extracted set beats a manual-TC list or a static DOM snapshot. Sub-fields often reveal only *after* their parent is answered (see `troubleshooting:reveal-on-answer-questionset`). Expect to reconcile the expected list after the first run; don't over-invest in static review of "what should render."
- 3 failed iterations on one symptom: stop and ask.

### Phase 5: verify

- **Justify every new registry entry added during Phase 4 iteration.** If a `FieldRegistry`/`FieldNames` entry was added reactively to a failure (not from the original manifest), confirm it's actually exercised: comment it out (or its `Pages` tag) and re-run. If the test still passes, the failure had a different root cause and the entry is dead weight (delete it, don't leave it "just in case"). This caught 3 unused entries on TC 240782, added under failure pressure, never actually required.
- Test passes locally and walks the full TC.
- Attributes correct, including a required `[Author]` (plus `[Tenant]`, `[Category]`, `[TestCaseId]`); method clean (orchestration + business validations only).
- No new logging in untouched files; no secrets logged (`[REDACTED]`).
- Comments are compact: non-obvious *why* only, no step narration. Wide-relevance knowledge went to the KB, not inline (`philosophy:compact-comments`, `philosophy:tests-stay-clean` check 9).
- Summarize what was implemented and any assumptions made.

> **Commit messages: reference the TC so ADO auto-links it.** When you commit a test add/remove/fix, put `TC #<id>` in the message (e.g. `TC #246475 PGR E&S Bamboo coverages display`). Azure DevOps parses `#<id>` on the branch's commits and links them to the work item during the PR, so the automation lands attached to its TC. Use the real numeric id; if the TC id is unknown, ask rather than inventing one. Multiple TCs: list each (`TC #248329, TC #248414`).

## KB topics to open first

`domain:tc-api` · `domain:tc-conventions` · `framework:overview` · `framework:field-registry` · `framework:flows-executor` · `framework:test-class` · `philosophy:tests-stay-clean`. Everything else: `kb search`.

> **Token tip:** `kb lookup --topic <key>` only returns a *pointer* (the primary file path), then you still `Read` it. Two steps. When you already know the topic key, skip `lookup` and `Read` the file directly at `Documentation/agent-knowledge/<area>/<file>.md` (the areas map 1:1 to the topic prefix: `domain:` → `domain/`, `framework:` → `framework/`, `philosophy:` → `philosophy/`, `troubleshooting:` → `recipes/troubleshooting/`). Use `kb search` only when you *don't* know the key.
