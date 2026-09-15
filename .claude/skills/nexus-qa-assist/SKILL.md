---
name: nexus-qa-assist
description: Assist a manual QA with semi-manual testing by driving a live headed browser through the Nexus framework, so the QA never writes code or learns the framework. The QA describes in plain language where they need to get, what to prefill, and what they want to verify by hand; the agent walks the flow, fills fields via the FieldRegistry, pauses for manual verification, and advances page by page. Also captures exploratory testing of new functionality via the native Playwright recorder and distills the recording (dropping noise like on/off toggling) into a clean scenario. Saves sessions as replayable local scenario files. Trigger when a manual QA (or product-team member) asks to get to a page with data prefilled, to speed up repetitive manual testing, to record/capture their exploratory steps, to clean up a recording, to replay a saved scenario, or asks "help me test X manually". Do NOT trigger for authoring an automated NUnit test (nexus-test-author), debugging a failing automated test (nexus-debug), or reviewing code (nexus-test-review / nexus-framework-review).
---

# nexus-qa-assist

Turn a manual QA's plain-language description into a live, prefilled, headed browser
session and a replayable scenario file, using the framework the automation team
already maintains. The conversation is the interface. The user never sees code, never
runs a command, never needs to know what a FlowType is.

## Doctrine: the QA owns the verification, you own the plumbing

- **Speak QA, not framework.** Say "the vehicle details page", not `D2C_VehicleDetailsPage`;
  "I'll fill the VIN", not `InteractWithField`. Translate silently in both directions.
  Never ask the user for an enum value. Offer the options you found, in plain words.
- **Never verify on the QA's behalf.** When the scenario reaches a checkpoint, pause and
  hand over. What they check and how long they look is their call. Never auto-confirm a
  pause, never assert "looks correct" about something the QA wanted to inspect.
- **`AskUserQuestion` is the handover instrument. Use it proactively.** Every pause ends
  in an `AskUserQuestion` with concrete outcomes ("Verified, continue" / "Something's
  wrong" / …), and the same tool carries the manifest confirmation, ambiguity choices,
  and failed-fill decisions. Plain "let me know when done" text is not a handover. The
  QA should always have buttons to answer with.
- **Everything runs headed.** The QA watches the browser; it is their session. Between
  your commands the browser is fully theirs. Clicking around manually is fine; ask them
  to tell you when they've changed page so you can re-sync (`browser list` shows the URL).
- **Self-service the plumbing silently.** Pull, build, secrets, Playwright install, stale
  hosts: fix these yourself; report only "getting the framework ready (first time can
  take a few minutes)". Never ask the QA to run a command or paste an error into a shell.
- **Ask, don't assume** (house doctrine). Unknown tenant? Ask. Two plausible flows? Show
  both in plain words and ask. Three failed attempts on one symptom? Stop and explain honestly.

## CLI commands this skill uses

> **Invoking `nexus-agent`:** if not on PATH, use the built binary:
> `Bolt.Automation.AgentTools/bin/Debug/net10.0/nexus-agent.exe`
> (build first if missing: `dotnet build Bolt.Automation.AgentTools`).

| Need | Command |
|---|---|
| Get framework knowledge (tenant, LOB, product) | `nexus-agent kb search "<terms>"` |
| List/inspect flows to pick the right one | `nexus-agent code flow-trace --flow <FlowType>` |
| Resolve a field the QA described | `nexus-agent code field-lookup <Name>` |
| Open a session: walk a flow to a page | `nexus-agent browser navigate --flow <F> --tenant <T> --env <E> --until <Page> --headed [--data <file> \| --set F=V ...]` |
| Open a session: no-flow area (ADBX/admin), optional framework login | `nexus-agent browser open --tenant <T> --env <E> [--user <User>] [--login] [--url <u>] --headed` |
| Open a session: jump straight to a quote questionnaire | `nexus-agent browser open-quote --tenant <T> --env <E> --quote-file <file> --headed` |
| Open a session: PGR consumer quote by address key | `nexus-agent browser quote-start --tenant <T> --env <E> --address <AddressKey> --headed` |
| Fill only the fields the QA named | `nexus-agent browser fill --session <id> --set Field=Value ... [--data <file>]` |
| Advance exactly one page | `nexus-agent browser continue --session <id> [--page <P>] [--expect <P>]` |
| Record exploratory testing (native Playwright codegen) | `nexus-agent browser record --session <id> [--output <file.cs>]` |
| Parse a recording into structured actions | `nexus-agent browser parse-recording --file <recording.cs>` |
| Replay a recorded step with no registry field yet | `nexus-agent browser raw --session <id> --action <a> --selector <sel> [--value <v>]` |
| Close a session cleanly | `nexus-agent browser close --session <id>` · `browser close --all` |
| Environment diagnosis / bootstrap | `nexus-agent doctor [--fix]` |
| Manual-verification checkpoint | `nexus-agent browser pause --session <id> --reason "<what to check>"` → `AskUserQuestion` → `browser resume --session <id>` |
| Show the QA evidence / see the page | `nexus-agent browser screenshot --session <id>` · `browser inspect --session <id> --scope form` |
| Session bookkeeping | `nexus-agent browser list` |
| Secrets state (preflight) | `nexus-agent secrets status` (details: nexus-secrets skill) |

## Phase 0: preflight (silent, every session)

Run before the first browser command; tell the user only that you're "getting ready".

1. **Diagnose in one shot.** `nexus-agent doctor` checks repo, build freshness,
   Playwright browsers, AWS CLI + SSO profile, secrets bundle, `BOLT_SECRETS_PATH`, and
   the permissions allowlist, each with the exact fix. `doctor --fix` applies the
   non-interactive repairs (Playwright install, user-level env var). Interactive fixes
   (first-time `aws configure sso` / `aws sso login`) are the QA's to run; walk them
   through per the nexus-secrets skill.
2. **Repo current.** `git fetch`, then fast-forward the checkout branch (normally `develop`).
   If offline or the pull fails, say you're running on the last local version and continue.
   Never leave the QA's checkout mid-merge. If the tree is dirty or the pull conflicts,
   skip updating and continue on what's there.
3. **Build.** `dotnet build Bolt.Automation.sln` (the browser host reflects over the Tests
   output and refuses stale DLLs). If today's develop doesn't build, `doctor` reports
   the last-known-good commit (recorded whenever repo+build checks pass on a clean tree).
   The fallback, `git checkout <last-good-commit>` (detached HEAD, reversible with
   `git checkout develop`), moves the QA's checkout, so ASK FIRST (`AskUserQuestion`:
   "today's framework doesn't build; switch to the last good version from <date>, or
   stop?"), and never do it on a dirty working tree. On a yes: switch, build, and remind
   them they're on yesterday's framework. Never silently mutate repo state; never fail
   the session without offering the fallback.
4. **Secrets.** Covered by doctor; refresh per the nexus-secrets skill when stale.
5. **Stale host / dead sessions.** `browser list`, then close `unreachable` sessions with
   `browser close --session <id>` (or `--all`). A `blank_window_closed` session is
   usually a closed window, but a just-opened session that hasn't navigated yet also
   shows blank, so re-list after a few seconds and only close it if still blank. Only
   `taskkill /IM nexus-agent.exe /F` when the host itself is wedged. The host self-exits
   after 4 idle hours with no reachable sessions.
6. **Environment health (optional).** When the `nexus-logger` MCP server is connected,
   check `get_environment_health` / `get_platform_status` for the target env. A
   degraded env is worth telling the QA about before driving a browser through it. Skip
   silently when the server isn't available; never require it.

## Phase 1: elicit the scenario (plain language to manifest)

Get from the QA, conversationally (most give it all in one message):

- **Where.** Tenant/partner, environment (default QA), product/LOB, and the page they
  need to reach, in their words ("the coverages page of a USAA auto quote").
- **What to prefill.** The values they care about; everything else gets framework defaults.
- **What they'll verify by hand.** This becomes the pause checkpoint(s).

Map it with the CLI (`kb search`, `code flow-trace`, `code field-lookup`). Never guess a
tenant, flow, or field name into a command. Then play the plan back in plain language
("I'll open a USAA auto quote on QA, fill ZIP 33101 and this VIN, and stop on the vehicle
page so you can check the decode banner. Sound right?") and get a yes before driving.
Unknown tenant, no flow hit, or ambiguous field: ask, with options.

## Phase 2: drive (fill, pause, continue cadence)

1. `browser navigate … --headed` (with `--set`/`--data` overrides) to the checkpoint page,
   or `browser open-quote` when the target is a quote questionnaire and a quote payload
   exists. Long walks are normal (30–60s+); warn the QA it takes a moment.
2. `browser fill --session <id> --set …` for the fields they named. Report per-field
   results honestly: filled vs failed. On a failed field, show a screenshot and ask how to
   proceed (retry / they fill it by hand / skip).
3. At each checkpoint: `browser pause --reason "<their verification, their words>"`,
   then `AskUserQuestion` with options like "Looks good, continue" / "Something's wrong".
   On "wrong": screenshot for the record, ask what they see, and either iterate or stop.
   A found bug is a successful session. On "good": `browser resume`, then
   `browser continue` to advance one page, and repeat.
4. On `navigate_timeout` / `continue_timeout`: the browser stays open at the stall point.
   Screenshot it, show the QA where it stopped, and decide together.
5. Only the QA can do SSO hardware keys, captchas, or real-PII entry. Drive up to that
   point, pause, hand the keyboard over, resume when they're done. USAA is SSO-driven;
   expect this.

**Constraint:** don't run `dotnet test` while a browser session is live (Playwright state
races). Finish the session first.

## Phase 2b: exploratory capture (QA drives, you distill)

For new functionality there is nothing to script yet. The QA explores it by hand and
you turn the exploration into a scenario. This inverts the usual roles: they drive, you
watch the recording afterwards.

1. **Position the session** at the exploration start with any opener (`quote-start`,
   `open`, `navigate`). Omit `--headed` when the plan is to record: the session's
   only job here is creating the quote/auth and donating storage state, and keeping it
   headless makes the recorder the only window the QA sees (codegen always launches
   its own browser; it cannot attach to the session's). Use a headed session only when
   the QA decides to record mid-way through an interactive walk.
2. **Start the recorder:** `browser record --session <id>`. This opens a new browser
   window (Playwright's native codegen, inheriting the session's login/quote state) and
   streams every action to a C# file under `.qa-scenarios/recordings/` (a `.meta.json`
   provenance sidecar is written next to it). The recorder launches the donor session's
   browser channel automatically; for a session-less `record --url`, pass
   `--channel chrome|msedge` on machines without Playwright's bundled Chromium. Tell the QA two things:
   *"Explore in the new window that just opened. Do whatever you need, including things
   you're just poking at. When you reach a moment you'd want a replay to stop and let
   you verify, type `##what to check##` into any text box and keep going. Close the
   window when you're done."* The command blocks until they close it; don't run other
   browser commands meanwhile. (The result warns if the donor session was headed:
   two look-alike windows.)
   Window-confusion guidance, learned live: when two look-alike windows are open, tell
   the QA the recorder is the one paired with the Inspector panel. The recorder
   re-enters at the SPA's own landing page (e.g. `/overview`), not necessarily where the
   session was. Closing the *session* window by mistake blanks it (`record` then
   refuses with `about:blank`; open a fresh session). The Inspector's visible log can
   look empty while the file records fine, so always read the output file before concluding
   the capture failed.
3. **Distill.** `browser parse-recording --file <recording.cs>` gives the structured
   action list (targets, values, `##…##` markers, replay-ready selector suggestions).
   Clean it per `recipe:recording-cleanup`: apply the noise heuristics (toggle
   pairs, refills, dead ends), turn markers into `pause:` steps, then run the
   mandatory intent interview via `AskUserQuestion`. The QA decides what was
   exploration and what is the scenario.
4. **Map and save.** Registry fields become `fill:` steps, flow advances become `continue:`,
   unregistered new-functionality elements become `raw:` steps (recorded selector, prefer
   `role=`/`text=`, never XPath). Save per the scenario format; keep the raw recording
   as provenance until a clean replay is confirmed.
5. **Validate.** Offer an immediate headed replay with the QA watching; the first
   replay is the review. Recurrent `raw:` failures mean the new fields need FieldRegistry
   entries. Hand that to the automation team.

## Phase 3: save and replay scenarios

- After a successful session, offer: *"Want me to save this so you can rerun it anytime?"*
  Write the scenario per `recipe:qa-scenario-format`
  (`kb lookup --topic recipe:qa-scenario-format`) into `.qa-scenarios/<name>.yml`.
  Local and gitignored, never committed, no code in it, only field names and page names.
- **Replay** ("run my vin-decode scenario again", "same but ZIP 90210"): list
  `.qa-scenarios/*.yml` if the name is fuzzy, apply any spoken overrides, run Phase 0
  preflight, then execute the steps per the format doc's replay semantics. Pauses always
  stop and wait for the QA; failures are reported, not papered over.
- If a saved scenario breaks because the app changed (field renamed, page split), fix the
  mapping with `code field-lookup` / `flow-trace`, confirm with the QA, and update the
  file. If it breaks because the *framework* lacks something, that's a handoff to the
  automation team; say so plainly. When the `nexus-logger` MCP server is connected, the
  Agent Inbox (`create_inbox_item`) is the async handoff channel for such requests
  (and for found-bug evidence). Follow the server's own inbox workflow; never put
  secrets in items.
- When a scenario has clearly become a regression staple, suggest the QA hand it to the
  automation team as input for a real automated test (`nexus-test-author` takes it from
  there). Scenarios are a stepping stone, not a shadow test suite.

## What this skill is not

- **Not test authoring.** No NUnit code is ever written here (that's `nexus-test-author`,
  for the automation team).
- **Not a bug-diagnosis engine.** When the QA finds a product bug, capture evidence
  (screenshot, URL, scenario file) so they can file it; don't root-cause the app.
- **Not unattended automation.** Never run a scenario headless or skip pauses to "save
  time". The human verification IS the point.

## KB topics to open first

`recipe:qa-scenario-format` · `domain:product-areas` (which FrontEnd/flows per product) ·
`domain:glossary` (their words to framework words). Tenant/LOB specifics via `kb search`.
