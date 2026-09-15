---
name: nexus-debug
description: Diagnose a failing or flaky nexus test, or visually inspect what a flow/page renders for a tenant. Starts from the structured failure record (TRX + page-source + screenshot correlation) and can drive a live headed Playwright session to reproduce. Trigger when the user asks why a test failed, to debug a flaky test, or to "show me what page X looks like for tenant Y". Do NOT trigger for authoring a new test (nexus-test-author) or reviewing code (nexus-test-review / nexus-framework-review).
---

# nexus-debug

Diagnose nexus test failures from evidence, and reproduce live when needed. Navigate the KB on demand with `nexus-agent kb`.

## Doctrine: evidence before theories, ask when blocked

- **Always run `nexus-agent failure summarize` first.** Never read raw `page_source` HTML by hand before the structured summary tells you where to look.
- When the `nexus-logger` MCP server is connected, `get_test_history` / `get_flaky_tests` / `get_consecutive_failures` / `get_failure_groups` answer "is this flaky or newly broken in CI?" before you chase a local repro. The CLI `failure` verbs stay the tool for local artifacts. Never require the MCP server.
- Hand the keyboard back for things only the user can do: SSO with a hardware key, a captcha, real PII. Drive the browser to that point, then `browser pause` + `AskUserQuestion`.
- After 3 dead-end iterations on one symptom, stop and ask.

## CLI commands this skill uses

| Need | Command |
|---|---|
| Structured failure record | `nexus-agent failure summarize --test <FQN>` |
| Scoped rendered DOM at failure | `nexus-agent failure page-source --test <FQN> --scope form\|page\|all` |
| Final screenshot at failure | `nexus-agent failure screenshot --test <FQN>` |
| Match a symptom to a fix | `nexus-agent kb search "<symptom>"` |
| Reproduce live (headed) | `nexus-agent browser navigate --flow <F> --tenant <T> --env <E> --until <Page> --headed` |
| Look at the live page | `nexus-agent browser screenshot --session <id>` · `browser inspect --session <id> --scope form` |
| Cooperative handoff | `nexus-agent browser pause --session <id> --reason "<what you need>"` → then `browser resume --session <id>` |
| List live sessions | `nexus-agent browser list` |

## Flow

### 1. Read the failure

`nexus-agent failure summarize --test <FQN>` returns the outcome, error message, stack, the correlated page-source and screenshot paths, and a nearest-match suggestion. Let it point you.

> **Triage first if the error is credential, secret, or connection-related** (e.g. `Set BOLT_SECRETS_PATH…`, a 401/auth failure, a DB-connection error, or app secrets resolving null). That is likely an environment problem, not a test bug. Check the local secrets cache TTL and refresh if stale: `nexus-agent secrets status`, then `nexus-agent secrets sync --force` (re-run `aws sso login` if creds expired). See the nexus-secrets skill. Don't chase it through page-source or live repro until the cache is confirmed fresh.

### 2. Inspect the evidence

- `failure page-source --test <FQN> --scope form` shows what actually rendered (form scope strips noise). Compare the field the test expected against what's present.
- `failure screenshot --test <FQN>` for the visual state at failure.
- `kb search "<symptom>"` finds the troubleshooting leaf (e.g. locator-not-found, default-reinjected, strict-mode-multimatch, conditional-reveal). Read it.

### 3. Reproduce live (only if evidence is inconclusive)

- `browser navigate --flow <FlowType> --tenant <T> --env <E> --until <Page> --headed` walks the flow and leaves a headed browser open; note the `session_id`.
- `browser inspect --session <id> --scope form` and `browser screenshot --session <id>` to examine the live page.
- If the flow needs an SSO hardware key, captcha, or real login: `browser pause --session <id> --reason "log in with your hardware key, then I'll continue"`, call `AskUserQuestion`, and `browser resume --session <id>` once the user is done.
- A walk can be slow. If `navigate` reports `navigate_timeout`, the browser is left open at the stall point. `inspect` or `screenshot` it to see where it stuck, or re-run with a larger `--timeout`.

### 3b. Discriminate between two theories — cheaper than arguing

When the evidence fits more than one story, spend a run rather than a paragraph.

- **Flip one variable, run alone.** Re-run the same test with only the suspect change reverted. That is
  what tells "my change broke it" from "it was already broken". Both directions matter: a green run with
  the change is worth as much as a red one without it. Never run two suites at once while doing this —
  a starved run fails in ways that look exactly like real regressions.
- **Read the page source for what *else* held.** If dropdowns kept their values and switchers did not,
  the split is by control type and the store is not the culprit. Grouping the surviving state is often
  more decisive than the failure message.
- **Reproduce the DOM, not the app, when the question is about Playwright.** For "would `check()` work on
  this control", build a static page carrying the component's exact markup and CSS and measure each
  strategy against it. Minutes, not a flow walk, and it answers the actionability question exactly.
  Be clear about what it cannot reach: framework re-renders, store round-trips, CI timing.
- **Prove your instrumentation ran.** A diagnostic that logs only on failure cannot distinguish "verified
  and fine" from "never executed". Log the success path too, then check the count.

### 4. Conclude

State the root cause, cite the troubleshooting leaf, and propose the fix (or hand off if it needs the user). Don't edit unrelated files; don't add logging to files the user didn't change.

**If the fix is a new `FieldRegistry` entry, check reuse first.** Grep the registry for the failing page type already appearing in some other entry's `Pages` array with a matching or broader DOM class. A generic field tagged for that page may already cover it, and the real failure is elsewhere (e.g. strict-mode ambiguity, or a step that doesn't need that field at all). Don't add a speculative entry to make a symptom disappear without confirming it's actually exercised: comment it out, re-run. See `framework:field-registry` anti-patterns.

## Constraint

Don't run `dotnet test` while a `browser` session is live. Playwright install/launch state can race. Finish (or shut down) the live session first.

## KB topics to open first

`kb search "<symptom>"` for the troubleshooting index; `framework:popups` and `framework:field-registry` are common culprits.
