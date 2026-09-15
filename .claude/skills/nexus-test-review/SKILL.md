---
name: nexus-test-review
description: Review a nexus automation TEST (or a test PR) for correctness, framework-idiom adherence, and design-philosophy violations, judged against its sibling tests, not in isolation. Trigger when the user asks to review a test file/method, check a test against siblings, or review a test-only change. Do NOT trigger for framework/infrastructure changes (nexus-framework-review), authoring a new test (nexus-test-author), or debugging a failure (nexus-debug).
---

# nexus-test-review

Review a test the way a senior automation engineer would: against the codebase's own conventions and siblings, with the design philosophy in hand. Navigate the KB on demand with `nexus-agent kb` / `nexus-agent philosophy`.

## Doctrine: context before judgement, flag don't assume

- Review against siblings, never in isolation. A lone method tells you little; the pattern its neighbours follow tells you a lot.
- When a finding might be an intentional divergence, raise it as a question, not a defect.

## CLI commands this skill uses

| Need | Command |
|---|---|
| Find sibling tests/pages to compare against | `nexus-agent code find-similar --pattern <Symbol>` |
| The review rubric | `nexus-agent kb lookup --topic recipe:test-review-rubric` |
| Philosophy docs for an area | `nexus-agent philosophy lookup --area tests\|page-objects\|logging` |
| Framework idiom details | `nexus-agent kb lookup --topic framework:<topic>` |

## Flow

1. **Locate siblings.** `code find-similar --pattern <TestClassOrBase>`, then read 1–2 nearest neighbours so you know the local convention before judging.
2. **Load the rubric.** `kb lookup --topic recipe:test-review-rubric`. Review across its dimensions, not ad hoc.
3. **Check idioms.** Attributes (`[Tenant]` present? `[Category]`, `[TestCaseId]`?), base class, NUnit parameterized conventions (serializable args only, see `Bolt.Automation.Tests/CLAUDE.md`), string field-name constants via `IPageHelper` (no `UIElement`, no XPath, no `Thread.Sleep`).
4. **Check philosophy.** The big one: logging in the test body is a violation. Log in page objects and helpers. Cite `philosophy lookup --area logging` (`philosophy/logging-where-work-happens.md`) and `--area tests` (`philosophy/tests-stay-clean.md`). A test method should read as step orchestration plus business validations.
5. **Report.** Group findings by severity; cite the rubric dimension and/or the philosophy leaf for each. For anything that could be deliberate, ask rather than assert.

## What to flag

- `[Tenant]` missing on a tenant-specific test.
- `[Author]` missing. Every test must carry `[Author(Author.<name>)]`.
- Logging, `Console.WriteLine`, or DOM assertions inside the test method.
- Non-serializable `TestCaseData` args (`bool?`, arrays, objects). These break orchestrator FQN discovery.
- XPath locators, `Thread.Sleep`, raw `HttpClient`.
- Re-implementing a helper a sibling already provides (point to the sibling from `find-similar`).
- `// Step N` narration duplicating an `ExecuteStepAsync` label, or verbose multi-line comments that restate the code or re-explain platform behavior. Comments carry the non-obvious why, compactly; wide-relevance knowledge belongs in the KB (`philosophy:compact-comments`, `philosophy:tests-stay-clean` check 9).

> **If you commit a fix, reference the TC.** Put `TC #<id>` in the commit message so Azure DevOps auto-links it to the work item during the PR. Real numeric id; ask if unknown. Multiple TCs: `TC #a, TC #b`.

## KB topics to open first

`recipe:test-review-rubric` · `philosophy:tests-stay-clean` · `philosophy:compact-comments` · `philosophy:logging-where-work-happens` · `framework:test-class`.
