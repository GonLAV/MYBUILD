---
topic: partner:pgr-covmod-wind-hail-na
summary: PGR CovMod — when a carrier returns Wind/Hail as Not-Applicable, it mirrors the Standard deductible and renders no editable dropdown (flag ba-homesite-wind-hail-na).
status: ready
---

# PGR Custom package — Wind/Hail "Not-Applicable"

In the Progressive HQX 2.0 Custom (coverage-modification) package, a carrier may return **no editable Wind/Hail deductible** for a given quote. When it does, Wind/Hail is **Not-Applicable**:

- Its displayed value **mirrors the selected Standard (All Perils) deductible**.
- It renders as a **static row**, **not** an editable `ng-select` dropdown.

This is a permanent, expected carrier behavior (feature flag `ba-homesite-wind-hail-na`), not a defect — so a CovMod dropdown test must validate the N/A *contract* (not-editable + mirrors-Standard) rather than checking Wind/Hail against the carrier/state option list.

## Detecting it (backend)

The signal is the carrier request/response attachment (type 2), which carries the offer-shaping `XWindFlag` and, when Wind/Hail IS offered, one or more `WindHailDeductible` values:

- **N/A** ⟺ `XWindFlag == false` (Wind/Hail not excluded) **AND** no `WindHailDeductible` element present (the deductible "did not return at all").
- When a `WindHailDeductible` IS returned, Wind/Hail is a genuine editable offer even though the coverage XML flags it `CoverageCustomType=NA` — that coverage-XML flag alone does **not** distinguish the two states, so read the request attachment.

`ResultDataHelper.IsWindHailNotApplicableAsync` implements this (returns `null` when it can't be determined — no attachment / unparseable).

## Reading it (UI)

The coverage editor renders progressively and the Wind/Hail `ng-select` can flicker (present one moment, collapsed the next) under parallel load — a single point-in-time visibility read is flaky. Poll the editable state until it is **stable** (identical across several consecutive samples) before asserting. In the N/A layout the value may live in a read-only `ng-select` label **or** a static display row, so read both (fall back from the label to the row's info-amount).

## Cross-references

- [progressivepl.md](progressivepl.md) — PGR HQX 2.0 / CovMod overview.
- [../lob/home.md](../lob/home.md) — Homeowners coverages.
