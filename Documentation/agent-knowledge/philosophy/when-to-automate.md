---
topic: philosophy:when-to-automate
summary: Deciding which TCs are worth automating — value, churn, flake risk, manual cost (forward-port from V1 automation-triage).
status: ready
---

# When to automate

> **When to read:** Before opening a TC for automation — decide whether this one earns its keep.
>
> **Forward-ported from:** V1 `automation-triage` skill (`.skill-explore/agent-kit/.claude/skills/automation-triage/SKILL.md`).

## Input

A manual test case, a feature description, or an ADO work item tagged `automate`.

## Decision framework

**Automate now** if:
- Regression risk is high (touches money flow, eligibility, tenant scoping, or a customer-visible quoting/policy path).
- The test is repetitive — runs every sprint or every release.
- Behavior is deterministic given inputs (no human judgment required).
- The feature is stable — not actively being redesigned.

**Don't automate** if:
- The feature is still changing shape week-to-week (UX/contract churn will invalidate the test before it returns ROI).
- The test requires subjective judgment (visual polish, ambiguous "does this feel right").
- It's a one-time validation (pre-launch sanity, data migration spot-check).
- The cost of automating exceeds the cost of running it manually for the feature's expected lifetime.

**Defer** if:
- The feature is worth automating but not stable enough yet. Tag the ADO work item `automate-later` and re-evaluate next quarter.

## Layer recommendation

Map the behavior to a layer and give the reason:

- **Unit** — behavior is pure C# logic in a helper or utility, no external deps.
- **API** — behavior is observable via the API contract (response shape, status, validation, downstream effect). Cheapest reliable layer for most contract checks.
- **UI** — behavior is only observable in the browser (rendering, role-gated visibility, field interactions, multi-step interview flow). Most expensive — reserve for behaviors only the UI can verify.
- **Integration** — behavior crosses two or more services and the cross-cut itself is what you're verifying.

Default to the lowest layer that gives confidence.

## Output shape

When the agent applies this skill, it should produce a clear recommendation with rationale. Format:

> **Recommendation:** Automate at the **API** layer.
>
> **Why:** The acceptance criterion is "endpoint rejects invalid VIN with HTTP 400 + structured error." That's a pure contract check — no UI involvement, no manual judgment, no flux. High regression risk because VIN is on the quote critical path.
>
> **Estimated effort:** ~½ day. Single test class, `[TestCaseSource]` for invalid VIN variants.

Never answer with "it depends" — pick a layer and explain. If you genuinely cannot decide without more info, ask one specific clarifying question.

## When to ask, not decide

If any of the following are unknown:
- Whether the feature ships behind a feature flag and what state the flag will be in for routine runs.
- Whether the partner/tenant in scope already has an automation user provisioned.
- Whether the underlying API is contract-stable or still in design.

Pause and ask the user before recommending. Triage decisions made on incomplete information are how dead automation gets created.

## Cross-references

- [../framework/test-design.md](../framework/test-design.md) — after triage says "yes, automate it," this is the design playbook.
- [../recipes/test-review-rubric.md](../recipes/test-review-rubric.md) — once authored, this is the review checklist.
- [tests-stay-clean.md](tests-stay-clean.md) — why automation that goes flaky is worse than no automation.
