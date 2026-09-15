# nexus-qa-assist — Follow-up User Stories & Roadmap

> **Status baseline (2026-08-05, `feature/qa-assist-skill`).** Shipped and live-tested:
> the `nexus-qa-assist` skill; session verbs `navigate` / `open` / `open-quote`
> (provider + applicant + submit + no-open) / `quote-start` / `fill` / `continue` /
> `record` / `parse-recording` / `raw` / `pause` / `resume` / `close` / `list`;
> `nexus-agent doctor [--fix]` bootstrap with last-known-good build record; checked-in
> `.claude/settings.json` allowlist; scenario files (`.qa-scenarios/`, gitignored);
> exploratory capture with structured parsing, `##…##` verification markers, and
> recording distillation (`recipe:recording-cleanup`). Live-validated on BOLTACCESS
> (MFA), PROGRESSIVEPL (CovMod rates walk, recording round-trip), UNIFY (consumer API
> chain + agent MarketsLib login). **Horizon 1 is complete except 1.2.**
>
> **Known not-yet-validated (pilot targets):** a live `browser fill` / `navigate --data`
> pass (1.2); a marker-in-the-wild recording session; the host's 4h idle-exit.
>
> **Immediate plan:** pilot sessions with real manual QAs; bugs feed a Horizon-1.x
> hardening pass; then Horizon 2 starting with the deterministic runner (2.1).

Stories are grouped into three horizons. Each is written to be liftable into ADO
as-is. Sizes are rough (S ≤ 1 day, M ≤ 3 days, L = a sprint-ish).

---

## Horizon 1 — Pilot hardening (unblock real QAs, low risk)

### 1.1 One-command bootstrap for a non-dev machine — **L, highest priority — ✅ shipped 2026-08-05** (`nexus-agent doctor [--fix]` + checked-in `.claude/settings.json` allowlist)
**As a** manual QA with no dev tooling, **I want** a single guided setup (git checkout,
.NET SDK, Playwright browsers, AWS SSO + secrets, `BOLT_SECRETS_PATH`) **so that** my
first session starts with "help me test X", not an install guide.
- *AC:* a `nexus-agent doctor` (or skill preflight) detects every missing prerequisite
  and either installs it (winget) or walks the QA through the interactive parts
  (`aws configure sso`); re-run is idempotent; documented as the QA onboarding page.
- *AC:* a checked-in `.claude/settings.json` allowlist (nexus-agent, git pull,
  dotnet build) so QAs aren't spammed with permission prompts.

### 1.2 Live `fill` validation + pilot bug intake — **S, do in first pilot**
**As the** automation team, **we want** the one unexercised path (`browser fill` on a
registry page, `navigate --data` overrides) exercised in a pilot session **so that**
the whole verb surface has a live pass before wider rollout.
- *AC:* UNIFY TC 110599 agent-side continuation completed once end-to-end
  (fill `PrimaryPhoneNumber` → continue → rates); defects found filed and fixed.

### 1.3 Session lifecycle polish — **M — ✅ shipped 2026-08-05** (`browser close --session|--all`, `list` state flags, host 4h idle-exit)
**As a** QA, **I want** sessions I'm done with to disappear cleanly **so that** stale
browsers and host state never confuse my next session.
- *AC:* `browser close --session <id>` (dispose scope + browser) and
  `browser close --all`; `list` flags sessions whose page is gone (`about:blank` /
  crashed) instead of showing them as live; host idle-exit after N hours.

### 1.4 Broken-develop resilience — **M — ✅ shipped 2026-08-05** (doctor records last-known-good commit; skill falls back via detached checkout)
**As a** QA, **I want** to keep testing when today's `develop` doesn't build **so that**
my work doesn't depend on the framework team's green streak.
- *AC:* preflight falls back to the last-known-good build output (kept per machine),
  reports "running on yesterday's framework" honestly, and never leaves the checkout
  mid-merge.

### 1.5 Recording UX — single-window flow — **S — ✅ shipped 2026-08-05** (headless-donor enforced in skill; `record` warns on headed donor; also shipped early from 3.4: `##…##` verification markers + `parse-recording`)
**As a** QA recording exploratory testing, **I want** exactly one browser window on
screen **so that** I can't click (or close) the wrong one.
- *AC:* skill always opens the donor session headless when recording is the goal
  (shipped as guidance — enforce in skill flow); `record` prints a one-line
  "interact in the window paired with the Inspector" instruction into chat.

---

## Horizon 2 — Scenario platform (make scenarios first-class)

### 2.1 Deterministic scenario runner — **L**
**As a** QA, **I want** `nexus-agent scenario run <file>` to execute a saved scenario
step-by-step **so that** replays are fast, cheap (near-zero tokens), and identical
every time; the agent supervises only pauses and failures.
- *AC:* runner executes every step type (navigate/open/open-quote/quote-start/fill/
  continue/raw/screenshot), stops at `pause` steps and waits for terminal confirmation
  (or hands back to the agent), emits a structured step-by-step result JSON;
  scenario schema validated before run with clear errors.

### 2.2 Scenario parameterization — **M**
**As a** QA, **I want** named parameters in scenarios (`${ZipCode}`, `${Address}`)
**so that** "same scenario, different state/carrier" is a prompt, not a file edit.
- *AC:* `scenario run --set ZipCode=90210`; defaults declared in the file; the skill
  asks for missing parameters conversationally.

### 2.3 Team scenario library — **M**
**As a** QA team, **we want** to share proven scenarios **so that** one person's setup
work speeds up the whole team — without scenarios leaking into the test repo.
- *AC:* a dedicated share location (separate small repo or artifact feed — decision
  needed) with owner + description metadata; skill can list/pull shared scenarios;
  local `.qa-scenarios/` stays personal and gitignored.

### 2.4 Scenario → automated test pipeline — **M**
**As the** automation team, **we want** a scenario that became a regression staple to
be promotable **so that** manual replay effort converts into real coverage.
- *AC:* `nexus-test-author` accepts a scenario file as its TC input (documented
  workflow); scenario carries an optional ADO TC id for traceability; promoted
  scenarios are marked so QAs know automation now covers them.

### 2.5 Scenario health sweep — **M**
**As the** automation team, **we want** a bulk validation run over shared scenarios
**so that** framework/app changes that break scenarios are found by us, not by a QA
mid-testing.
- *AC:* headless `scenario run --validate` mode (skips pauses, stops before
  irreversible steps); summary of broken scenarios with failing step + probable cause
  (field renamed / page split / selector rot).

---

## Horizon 3 — Coverage, capture depth, and integration

### 3.1 FieldRegistry coverage for ADBX/admin surfaces — **L (framework work)**
**As a** QA testing admin flows (org management, MFA settings, dashboards), **I want**
`fill`/`continue` to work there **so that** those sessions aren't open-then-all-manual.
- *AC:* ADBX/STS pages get registry entries (or `continue` learns `IBase` pages);
  the MFA scenario (TC 225488) can be driven step-by-step, not just to the login.

### 3.2 Entry-path verbs for remaining tenants — **M per tenant**
**As a** QA on Comparion / LibertyX / KLX…, **I want** the same one-command entry my
PGR/UNIFY colleagues have **so that** the skill isn't a two-tenant tool.
- *AC:* inventory of entry paths per tenant (quote-start-like API? portal login?
  deeplink?); one verb or verb-flag per distinct mechanism; KB `partner:*` leaves
  updated with the entry story.

### 3.3 Raw-step promotion report — **S**
**As the** automation team, **we want** to see which raw selectors QAs use repeatedly
**so that** new-functionality fields get registry entries while they're hot.
- *AC:* a report (CLI or periodic) aggregating `raw:` steps across shared scenarios;
  each entry links the scenario and the page; feeding the registry backlog.

### 3.4 Verification capture during recording — **L — ◐ partially shipped 2026-08-05**
**As a** QA, **I want** to mark "check this value/element" moments *while* recording
**so that** replays pause exactly where I verified, with the expected value remembered.
- *Shipped:* the `##text##` marker convention — typed into any input mid-recording,
  surfaced by `parse-recording`, distilled into `pause` steps at the exact position.
- *Remaining:* capturing **expected values/elements** (not just the pause text) so
  markers can become assert steps — e.g. `##check <selector-or-field> = <expected>##`
  grammar, or an Inspector pick-locator spike.

### 3.5 Evidence bundle for found bugs — **M**
**As a** QA who found a product bug mid-session, **I want** a one-command evidence
bundle **so that** my bug report writes itself.
- *AC:* `browser evidence --session <id>` collects screenshot, URL, application/
  friendly ids from scope, and the captured 4xx/5xx network correlation ids
  (`PageHelper.GetNetworkErrors`) into a folder + paste-ready markdown summary.

### 3.6 ADO execution feedback — **M**
**As a** QA lead, **I want** scenario runs linked to ADO test cases **so that** manual
test execution status stops being tribal knowledge.
- *AC:* scenario carries `tc_id`; completing a run (all pauses confirmed) can mark the
  ADO test point outcome via the nexus-logger proxy (needs proxy write endpoint —
  coordinate with that team).

### 3.7 Usage telemetry — **S**
**As the** automation team, **we want** lightweight usage stats (sessions, scenarios
run, verbs used, failures) **so that** we can show time saved and target investment.
- *AC:* opt-in local counter file or Mongo reporting hook; a monthly one-pager.

### 3.8 Skill eval suite — **S, recurring**
**As the** automation team, **we want** scripted evals for the skill's workflows
**so that** skill/KB edits don't silently regress behavior.
- *AC:* per architecture-doc cadence: ≥3 prompts per workflow (drive, replay,
  record-distill), including at least one where the correct behavior is to STOP and
  ask; run after skill/KB changes.

---

## Explicit non-goals (hold the line)

- **Scenarios are not a shadow test suite.** Anything regression-worthy graduates via
  2.4; scenarios stay local/shared-lightweight, never CI-executed.
- **No NUnit generation from recordings.** Recording → scenario → (human decision) →
  `nexus-test-author`. Skipping the middle steps produces unmaintainable tests.
- **No unattended replay of pause steps.** The manual verification *is* the product;
  automating past it silently converts a manual test into a bad automated one.

## Suggested sequencing

~~1.1 → 1.3/1.5 → 1.4~~ *(shipped 2026-08-05)* → **Pilot (now) → 1.2** → pilot-bug
hardening pass → 2.1 (deterministic runner) → 2.2 → 2.3 → 2.4/2.5 → Horizon 3 by
pilot-demand signal (3.5 evidence bundles and 3.3 raw-step promotion tend to pay off
earliest; 3.4's remaining assert-capture builds directly on the shipped markers).
