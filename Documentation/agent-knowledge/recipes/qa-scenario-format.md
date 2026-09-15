---
topic: recipe:qa-scenario-format
summary: Declarative YAML scenario files for semi-manual QA sessions — schema, storage rules (local-only, never committed), replay semantics, one step type per browser CLI verb.
status: ready
---

# QA scenario files — format & replay

> **When to read:** Running the `nexus-qa-assist` skill — saving a repeatable semi-manual
> testing scenario, or replaying one a QA saved earlier.

## What a scenario is

A scenario is a **declarative data file, not code**. It captures a semi-manual testing
session — get to a page, prefill fields, stop for human verification, advance — in terms
of **FieldRegistry field names and page type names**, never selectors. The framework
resolves the actual locators at replay time, so a scenario written before a UI redesign
keeps working after `git pull` as long as the field names still exist.

Scenarios are interpreted by the agent: each step maps 1:1 to a `nexus-agent browser`
verb (or a cooperative pause). There is no standalone runner — replay is "the agent reads
the file and issues the commands", which keeps the format forgiving and the errors
conversational.

## Storage — local only, never committed

- Scenarios live in **`.qa-scenarios/`** at the repo root (gitignored — nothing under it
  can be committed). One file per scenario: `<kebab-case-name>.yml`.
- They are personal work products. Sharing one is fine (send the file); versioning them
  in the repo is deliberately not supported — that's what real automated tests are for.
  If a scenario proves valuable enough to keep forever, hand it to the automation team
  as input for `nexus-test-author`.

## Schema

```yaml
name: usaa-auto-vin-decode-check          # kebab-case, matches the file name
description: >                            # one or two sentences — what the QA verifies
  Prefill driver + vehicle for a USAA auto quote, stop on the vehicle page to
  manually verify the VIN decode banner, then continue to coverages.
tenant: USAA                              # Tenant enum value
env: QA                                   # environment name
flow: D2CAutoFlow                         # FlowType enum value (from `code flow-trace`)
front_end: D2C                            # optional — FrontEndType override

steps:
  # 1. Walk the flow to a page, filling with framework defaults + optional overrides.
  - navigate:
      until: D2C_VehicleDetailsPage       # page type name
      data:                               # optional — merged over flow default data
        ZipCode: "33101"

  # 2. Fill ONLY the named fields on the current page (no default injection).
  - fill:
      Vin: "1HGCM82633A004352"
      AnnualMileage: "12000"

  # 3. Stop for the human. The agent pauses the session, relays this text to the QA,
  #    and resumes only when the QA confirms.
  - pause: >
      Verify the VIN decoded to a 2003 Honda Accord and no error banner is shown.

  # 4. Advance exactly one page. `expect` is optional (defaults to the flow's next page).
  - continue:
      expect: D2C_CoveragesPage

  # 5. Optional evidence capture at any point.
  - screenshot: {}

  # 6. Raw Playwright action — ONLY for elements not yet in the FieldRegistry
  #    (typically distilled from a `browser record` exploratory capture).
  - raw:
      action: click                        # click | fill | check | uncheck | select | press
      selector: role=button[name="Add driver"]
      # value: "..."                       # for fill / select / press
```

Step types and their CLI mapping:

| Step | CLI command | Notes |
|---|---|---|
| `navigate` | `browser navigate --flow <flow> --tenant <t> --env <e> --until <page> --headed [--data <file>]` | First `navigate` creates the session; overrides go via a temp JSON file or `--set` pairs. |
| `open` | `browser open --tenant <t> --env <e> [--user <User>] [--login] [--url <u>] --headed` | Session opener for areas with no registered flow (ADBX, admin surfaces). `user` resolves a named test user (its `LoginUrl` is the default destination); `login: true` performs the framework's STS login — credentials never appear in the scenario. |
| `open_quote` | `browser open-quote --tenant <t> --env <e> --quote-file <file>` | Alternative session opener — jumps straight to a questionnaire from a quote payload. `quote_file` is a path relative to `.qa-scenarios/`. |
| `quote_start` | `browser quote-start --tenant <t> --env <e> --address <AddressKey> [--user Consumer] --headed` | Progressive consumer entry: Platform QuoteStart with full prefill for a named `AddressKey`, opens the deeplink (settles on Overview; prefilled pages then auto-advance with `continue`). |
| `fill` | `browser fill --session <id> --set Field=Value ...` | Map of field → value. Order matters (parent/trigger fields first). |
| `pause` | `browser pause --session <id> --reason <text>` + `AskUserQuestion` | The QA's manual-verification checkpoint. Resume with `browser resume` after confirmation. |
| `continue` | `browser continue --session <id> [--page <p>] [--expect <p>]` | One page forward, page-object `ClickContinue` semantics. |
| `raw` | `browser raw --session <id> --action <a> --selector <sel> [--value <v>]` | One raw Playwright action (`click`/`fill`/`check`/`uncheck`/`select`/`press`) for **new functionality not yet in the FieldRegistry** — typically distilled from a `browser record` capture (see `recipe:recording-cleanup`). Prefer `role=`/`text=` selectors; never XPath. Upgrade to `fill:` once the field is registered. |
| `screenshot` | `browser screenshot --session <id>` | Evidence; path is reported back to the QA. |
| `inspect` | `browser inspect --session <id> --scope form` | Rarely in saved scenarios; mostly an authoring-time tool. |

## Replay semantics

1. **Preflight first** (repo up to date, solution built, secrets fresh) — see the
   `nexus-qa-assist` skill; a scenario must not fail because of a stale checkout.
2. Execute steps **in order**, always `--headed` — the QA watches and owns the browser.
3. On a `pause` step: pause the session, put the pause text in front of the QA verbatim,
   and wait for their confirmation before resuming. Never skip or auto-confirm a pause.
4. On a failed `fill` field (`status: failed` in the result): report which field failed
   and ask the QA whether to retry, skip it, or stop — a field may legitimately be gone
   after a UI change. Update the scenario file if the QA says the change is permanent.
5. On `navigate`/`continue` timeout: the browser stays open at the stall point —
   screenshot it and show the QA where it stopped instead of tearing down.
6. **Parameterized replay:** the QA can override any `data`/`fill` value at replay time
   ("run it with ZIP 90210") — apply the override for the run; offer to save it back only
   if the QA asks.

## Authoring rules (for the agent)

- Resolve every field the QA mentions with `code field-lookup <name>` **before** writing
  it into a scenario; resolve the flow with `code flow-trace --flow <name>`. Never guess
  names into a file.
- Values are strings; quote ZIP codes and other leading-zero-prone values.
- Keep one scenario per verification intent — "fill and check X" — not mega-scripts.
  A scenario longer than ~10 steps is a smell; split it.
- Write `description` for the *next* reader: what is being verified, not how.
