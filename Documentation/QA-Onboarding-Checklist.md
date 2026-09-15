# QA Onboarding Checklist — nexus-qa-assist

> Target: a manual QA on a **clean Windows machine with only Claude Code installed**,
> ending with a working first semi-manual testing session. The design principle: the QA
> runs **five commands ever** (three installs, two AWS logins) — everything else the
> agent does. Est. total time: 45–90 min, most of it downloads/build.

---

## A. Before day 1 — team lead prep (per QA, ~15 min)

- [ ] **ADO access**: the QA's account can read the `bolt_automation_nexus` repo
      (`BoltCollection/Epos`) **and** the private Bolt NuGet feed (restore fails without it).
- [ ] **VPN access**: `azure.devops.boltx.us` must be reachable from their machine.
- [ ] **AWS Identity Center**: assign the QA to account `992382389570` with the
      `AutomationTeam` permission set (read-only secrets access — see `nexus-secrets`).
- [ ] **Claude subscription seat** with enough headroom for browser-driving sessions.
- [ ] Tell them which **tenant/env** their first session will target (e.g. PROGRESSIVEPL/QA)
      and have one concrete test task ready — onboarding ends with a real session, not a demo.

## B. QA machine — the only manual terminal steps (~10 min + downloads)

Open **PowerShell** and run, in order (VPN connected):

- [ ] 1. Install git:
```bash
winget install --id Git.Git --accept-source-agreements --accept-package-agreements
```
- [ ] 2. Install the .NET 10 SDK:
```bash
winget install --id Microsoft.DotNet.SDK.10 --accept-source-agreements --accept-package-agreements
```
- [ ] 3. **Reopen the terminal** (PATH refresh), then clone (a browser sign-in window
      will appear — use your work account):
```bash
git clone https://azure.devops.boltx.us/BoltCollection/Epos/_git/bolt_automation_nexus
```

## C. Hand the machine to the agent (~20–40 min, mostly hands-off)

- [ ] 4. Open **Claude Code** in the cloned `bolt_automation_nexus` folder and say:

> *"I'm a new manual QA on a fresh machine — set everything up so I can run
> semi-manual testing sessions."*

The agent (via the `nexus-dev-setup` + `nexus-secrets` skills and `nexus-agent doctor`)
will: restore + build the solution, install Playwright Chromium, install the AWS CLI,
set `BOLT_SECRETS_PATH`, and create the gitignored runsettings. The repo ships a
permissions allowlist (`.claude/settings.json`), so prompt spam is minimal — trust the
workspace when asked.

- [ ] 5. The **two interactive AWS steps are yours** (the agent will tell you exactly
      when and dictate every answer — SSO start URL, region, account, role):
```bash
aws configure sso
```
```bash
aws sso login
```
      Both open a browser; sign in with your work account and click **Allow**.

- [ ] 6. Setup is done when the agent shows **`nexus-agent doctor` fully green**
      (repo ✓ build ✓ playwright ✓ aws ✓ secrets ✓ env var ✓ allowlist ✓).

## D. First session (same day — this is the point)

- [ ] 7. Describe a real task in plain words, e.g.:
      *"Get me to the rates page of a Progressive homeowners quote for an Idaho address —
      I want to check the Custom coverage dropdowns."*
      The agent opens a visible browser, prefills, and **pauses for you to verify** —
      the pauses are yours; take your time, click around, then answer the question it asks.
- [ ] 8. Try **exploratory recording** once: ask *"record me while I poke at X"*.
      Rules of thumb: interact only in the **new window paired with the Inspector**;
      type `##what to check##` into any text box to mark a verification moment;
      close that window when done. The agent turns the recording into a replayable scenario.
- [ ] 9. End by **saving a scenario** ("save this so I can rerun it") and replaying it
      once — that's the daily loop: describe → verify → save → replay.

## E. Sign-off criteria (team lead)

- [ ] `nexus-agent doctor` green on the QA's machine.
- [ ] One scenario saved in the QA's `.qa-scenarios/` and successfully replayed.
- [ ] One recording captured and distilled with the QA confirming the intent interview.
- [ ] The QA knows the three golden rules:
      **1)** the pause is yours — nothing advances until you say so;
      **2)** never type real customer PII — test data only;
      **3)** when something looks wrong, say so — a found bug is a successful session.

## Ongoing (no action needed until it bites)

- Secrets refresh is automatic via the agent; when the AWS token expires (~daily),
  the agent will ask you to re-run `aws sso login` — that's normal.
- If today's `develop` is broken, the agent will **ask** before switching you to the
  last known-good version — saying yes is safe and reversible.
- Weird state (two browsers, stuck session)? Just tell the agent — `browser list` /
  `browser close` cleanup is its job, not yours.
