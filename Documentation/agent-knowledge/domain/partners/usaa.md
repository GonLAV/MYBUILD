---
topic: partner:usaa
summary: USAA — SSO-driven, hardware-key login, Bristol West Auto carrier.
status: ready
---

# Partner · USAA — `USAA`

> **API `partner` field:** `USAA` · **Common abbreviations:** USAA

**Cache representation:** 5 TCs.

## Business model

SSO-only for agents. Direct-to-Consumer for members. Carrier integrations (Bristol West).

## Entry paths

- **Agent:** SSO via STS with USAA-specific Issuer (`http://usaa`) + Audience (`https://sts-qa-usaa.boltqa.com`). No password — `UserTestData.Sso = new SsoUserData { ... }`.
- **D2C:** Consumer-direct via D2C URL; sometimes API-driven retrieval flow.

## Features in cache

STS & Login (1), D2C (3), Account (1).

## Sub-areas observed

`D2C | Auto`, `D2C Bristol West`, `SSO Agents`, `Account update`.

## Real TCs

- **212329** — USAA | SSO | Agents | SSO into the interview.
- **162448** — USAA | D2C | Auto | Retrieval Process and going back again to make sure no fields lost.
- **237026** — Product | D2C | Adding Bristol West OLB flow to the Auto D2C | E2E with Payment (15+ steps including DocuSign).
- **235870** — Product | D2C | Adding Bristol West OLB flow to the Auto D2C | E-sign.
- **83094** — USAA | Account | Update | Update existing account with existing externalID.

## Known quirks

- **All agent flows are SSO**; `STS_LoginPage.Login()` reads `ScopeContext.CurrentUser.Sso` instead of password.
- **Bristol West OLB E-sign uses DocuSign** — D2C flow ends with payment + signature.
- **The retrieval process** (TC 162448) is a specific use-case where the consumer pauses + resumes — fields must persist across the gap.
- Categories typically include `[Category("SSO")]`.

Documented examples: any test in `Bolt.Automation.Tests/Tests/Usaa/`. (Look at `UsaaSsoAgentSsoToInterview` if it exists in the current branch.)

## Cross-references

[INDEX.md](INDEX.md), [../lob/auto.md](../lob/auto.md) (Bristol West carrier).
