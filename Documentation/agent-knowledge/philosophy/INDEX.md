---
topic: philosophy:index
summary: Design principles guiding the framework — read first when reviewing changes that touch shared abstractions.
status: ready
---

# Philosophy — index

> **When to read:** First, whenever reviewing or designing a change that touches **shared abstractions** — FieldRegistry, page objects, the logger, the test base classes, the executor. The mechanics files (`framework/*.md`) tell you *how* something works; these tell you *why* it works that way and what to defend.

The framework has a small set of opinions. They aren't preferences — they're load-bearing. A change that violates one usually means we've grown a parallel mechanism that conflicts with the existing one. When that happens, the right move is almost always to fix the change to fit the existing mechanism, not to widen the mechanism to accommodate the change. The exception is an explicit decision recorded in [design-decisions.md](design-decisions.md).

## The seven principles

| File | Principle |
|---|---|
| [sparse-dictionaries.md](sparse-dictionaries.md) | The test says what's DIFFERENT from the framework defaults. Re-supplying defaults is noise that hides intent. |
| [logging-where-work-happens.md](logging-where-work-happens.md) | Logging lives in implementation files (page objects, API clients). Tests describe what to verify; the things being verified describe themselves. |
| [fluent-page-objects.md](fluent-page-objects.md) | Page objects expose intent (`FillField`, `ClickContinue`), not Playwright primitives. Tests read as business rules, not DOM mechanics. |
| [tests-stay-clean.md](tests-stay-clean.md) | Test methods are step orchestration + business validation. No Playwright calls, no DOM grep, no logger.Info() for steps. |
| [compact-comments.md](compact-comments.md) | Comments explain the non-obvious *why*, compactly. Durable/wide-relevance knowledge goes to the KB, not inline prose. |
| [when-to-automate.md](when-to-automate.md) | Not every TC is worth automating. Triage before you build. |
| [design-decisions.md](design-decisions.md) | The ADR log — append-only record of major framework decisions and their consequences. |

## How these reinforce each other

The principles aren't independent. Sparse dictionaries only work because page objects are fluent (the registry can encode intent, not just locators). Logging in implementation files only works because tests are clean (the test body has no logging to compete with). Fluent page objects only stay fluent because we keep them out of test bodies (tests-stay-clean is what protects the abstraction).

Pull any one of these out and the others start to leak. That's the test for whether a change is doctrinally sound: does it strengthen all four, or does it pick one to weaken?

## What this section is NOT

These aren't operational rules — the operational rules live in:
- [Documentation/AI-Agent-Logging-Instructions.md](../../AI-Agent-Logging-Instructions.md) — concrete logging do's and don'ts.
- The root [CLAUDE.md](../../../CLAUDE.md) "Key Conventions" + per-project CLAUDE.mds — concrete coding rules.

Philosophy explains the *why* the operational rules look the way they do. When an operational rule needs to change (new tenant, new framework version), check philosophy first — the rule may have been protecting an invariant that's now obsolete, or may need adjustment without weakening the invariant.
