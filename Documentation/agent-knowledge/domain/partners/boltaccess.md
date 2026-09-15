---
topic: partner:boltaccess
summary: BoltAccess — admin tooling and case management surfaces.
status: ready
---

# Partner · BoltAccess — `BOLTACCESS`

> **API `partner` field:** `BoltAccess` · **Common abbreviations:** BoltAccess, Bolt Access, BOLT ACCES (typo seen in 194175)

**Cache representation:** 0 TCs in current sample. (TC 194175 references "KL, BOLT ACCES, UNIFY" — typo for BoltAccess — but is filed under Kraftlake's partner field.)

## Business model

Bolt Access — agents-focused product variant.

## Quirks

TBD when first dedicated TC arrives.

## Pattern: cross-tenant TCs

Some TCs apply to multiple tenants (TC 194175 explicitly: "KL, BOLT ACCES, UNIFY"). When a TC mentions multiple tenants:

- Run it once per tenant via NUnit `[TestCaseSource]` parameterization (see root `CLAUDE.md` NUnit Parameterized Test Conventions).
- Pass each tenant as a `string` argument and resolve via `ScopeContext` inside the test.

## Cross-references

[INDEX.md](INDEX.md).
