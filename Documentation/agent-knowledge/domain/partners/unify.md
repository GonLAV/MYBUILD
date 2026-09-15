---
topic: partner:unify
summary: Unify — interview frontend; WC market-availability handling for monopolistic states.
status: ready
---

# Partner · Unify (Bolt X) — `UNIFY`

> **API `partner` field:** `Unify (Bolt X)` · **Common abbreviations:** Unify, UNIFY

**Cache representation:** 6 TCs.

## Business model

Multi-tenant carrier-integration platform; subtenants `EXTERNAL`, `marketslib`. Has Platform-API admin paths.

## Entry path

ADBX agent login (subtenant-specific URL — `platformapi-qa-unify.boltqa.com` for Platform API). Some TCs are pure Platform-API tests with no UI (235402).

## Features in cache

Interview (2), GetQuoteAPI (1), Provision (1), carrier-integration phase B2/B3 (2 — these have `feature=(none)`).

## Sub-areas observed

`Unify Marketslib`, `New Carrier Integration B2/B3`, `Platform API`, `Interview V3`.

## Real TCs

- **235414** — Product | Interview V3 | Market availability page | Check monopolistic state message (WC).
- **241435** — Check adding second floor material (HO3 home).
- **235402** — Product | Platform API — Credentials API | Check update wholesaler credentials.
- **239923** — D2C | Product | CL Auto | Progressive DHUB — Remove Invalid VIN Validation.
- **229559** — QA | New Carrier Integration | B3 — Implementation | Mappings.
- **229935** — QA | New Carrier Integration | B2 — Implementation | Interview Relevancy.

## Known quirks

- **Subtenant routing** — `LoginUrl` differs per subtenant; `ScopeContext.Data.UrlDataCollection.FrontEnd?.LoginUrl` resolves it from the (Tenant, Environment) tuple.
- **Platform API uses different host** than ADBX (`platformapi-qa-unify.boltqa.com`).
- **B2/B3 phase tags** reference internal carrier-onboarding milestones — relevant for date-range filters but no special test logic.

## Cross-references

[INDEX.md](INDEX.md), [../lob/wc.md](../lob/wc.md) (TC 235414 monopolistic-state message).
