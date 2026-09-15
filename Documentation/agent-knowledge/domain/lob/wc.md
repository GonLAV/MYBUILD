---
topic: lob:wc
summary: Workers Compensation — business profile, class codes, payroll; monopolistic-state blocks in OH/ND/WA/WY.
status: ready
---

# LOB · Workers Compensation (`LobType.WorkersCompensation`)

**Cache representation:** 1 TC (Unify 235414 — Interview market-availability page checking monopolistic-state messages).

## Typical fields

- Business profile + state.
- Class codes (industry-specific WC class codes).
- Annual payroll per class.
- Owner / officer inclusion / exclusion.

## State note — monopolistic blocks

WC is monopolistic in **OH, ND, WA, WY** — agents are blocked from quoting in those states (TC 235414 verifies the message).

## Phase 2 hint

`FlowType.InterviewWCFlow`. Different page set from CL Auto (Locations + Employee pages instead of Vehicle + Operator).

## Cross-references

[../partners/unify.md](../partners/unify.md), [bop.md](bop.md), [cl-auto.md](cl-auto.md).
