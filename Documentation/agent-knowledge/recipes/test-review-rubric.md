---
topic: recipe:test-review-rubric
summary: Ten-dimension rubric for reviewing automation test PRs (forward-port from V1 test-review skill).
status: ready
---

# Test review — 10-dimension rubric

> **When to read:** Reviewing an existing test file or test method. The user names what they want reviewed — do not pull in adjacent files unless the review depends on them.
>
> **Forward-ported from:** V1 `test-review` skill (`.skill-explore/agent-kit/.claude/skills/test-review/SKILL.md`).

## Evaluation dimensions (score each 1–5)

1. **Granularity** — atomic? Right axis of decomposition? (Many similar tests with one input difference → should be `[TestCaseSource]`. One macro-test covering 5 behaviors → split.)
2. **Naming** — `Method_ExpectedBehavior_WhenCondition` or the V2 E2E convention `{Tenant}_{Lob}_{Feature}_{Action}`. Does the method name actually describe the behavior?
3. **Isolation** — no shared state, no ordering deps, proper `[SetUp]` / `[TearDown]`, `[Parallelizable]` where safe?
4. **Assertions** — strong enough? Testing the *right thing*, not just status codes? Does each assertion's message name the **actual** value it received? If the actual value reaches the report some other way — the message itself, or the helper that gathered it — no `LogDataValidation` is needed (see [../framework/logging.md](../framework/logging.md), "When it is not needed").
5. **Tenant + context** — `[Tenant]`, `[TestCaseId]`, `[Category]`, `[Author]`, `[RunIn]` present and accurate? Logger context (tenant, correlationId, entity ID) attached in setup?
6. **Selector quality** — `data-testid` or ARIA preferred? Page objects expose intent, not raw Playwright? Zero XPath at the test layer?
7. **Wait strategy** — Playwright waits or `WaitUtils`? Zero `Thread.Sleep`?
8. **API hygiene** — DI'd clients from `Bolt.Automation.ApiClients`, no raw `HttpClient`, no hardcoded creds, `.EnsureSuccessContent()` on responses?
9. **LD flag hygiene** — flags set in `[SetUp]` are restored in `[TearDown]`?
10. **Logging** — uses `StartStep` / `ExecuteStepAsync` / `LogBusinessRule` / `LogUiAction` where appropriate, not just `Info` everywhere? Page-object internals carry the implementation logging, test body stays at the orchestration level (see [../philosophy/logging-where-work-happens.md](../philosophy/logging-where-work-happens.md))?

## Output format

- **Verdict per file**: PASS / PASS WITH NOTES / FAIL.
- **Findings grouped by theme**, not per-line.
- **Concrete rewrites only** for tests that are wrong, flaky, or miss critical risk.
- For acceptable-but-not-optimal tests, say so and move on. Don't rewrite them.

## Rules

- If a test is acceptable but not optimal, say so — don't rewrite it. Reserve strong pushback for tests that are wrong, flaky, or miss critical risk.
- Don't rewrite tests the user didn't ask about.
- Don't invent acceptance criteria.
- Don't suggest tooling changes (test framework swap, library swap) unless asked.
- Don't propose ReportPortal, xUnit, or MSTest — replaced/banned by team convention. XPath belongs only inside the FieldRegistry (tenant-variant fallback) — flag it anywhere else (page objects, tests).

## Quick anti-pattern catalogue

When scanning a PR, these patterns earn a finding:

- `Thread.Sleep(...)` anywhere.
- `[Test]` without `[Tenant]`.
- `[Test]` without `[Author]` — every test must carry `[Author(Author.<name>)]`.
- `_logger.Info(...)` in the test body for step orchestration (should be `ExecuteStepAsync`).
- An assertion whose actual value appears nowhere in the report — no `Actual: {x}` in its message, and nothing logged it where it was gathered. (A `LogDataValidation` alongside a message that already prints the actual value is duplication, not a finding — see [../framework/logging.md](../framework/logging.md), "When it is not needed".)
- Assertion logic hidden in a helper/page object (e.g. a `Validate…Async` that asserts internally or throws a `…ValidationException`) instead of in the test body. The helper should return the gathered values; the test asserts on them (see [../philosophy/tests-stay-clean.md](../philosophy/tests-stay-clean.md), check 8).
- `// Step N — …` narration comments duplicating an `ExecuteStepAsync` step name, or verbose multi-line comment blocks that restate the code / re-explain platform behavior. Comments explain the non-obvious *why*, compactly; wide-relevance knowledge belongs in the KB (see [../philosophy/compact-comments.md](../philosophy/compact-comments.md) and [../philosophy/tests-stay-clean.md](../philosophy/tests-stay-clean.md), check 9).
- New `HttpClient(...)` instantiation in test code (should be DI'd Refit client).
- Hardcoded URL or credential in the test (should be `ScopeContext.Data.UrlDataCollection` / `TestContextAccessor.CurrentUserCollection`).
- `BuildFormData()` helper method instead of inline `formData` dictionary (see [../framework/test-class.md](../framework/test-class.md)).
- Re-supplying field values that already match the registry `DefaultValue` (see [../philosophy/sparse-dictionaries.md](../philosophy/sparse-dictionaries.md)).
- XPath at the test layer (page objects may use XPath, tests should not).
- New `[TestCaseSource]` data that passes complex objects, `bool?`, or `string[]` as args (NUnit can't serialize — see root `CLAUDE.md` NUnit Parameterized Test Conventions).

## Cross-references

- [../framework/test-design.md](../framework/test-design.md) — the design-time mirror of this rubric.
- [../philosophy/tests-stay-clean.md](../philosophy/tests-stay-clean.md) — the underlying principle.
- [../philosophy/when-to-automate.md](../philosophy/when-to-automate.md) — if the answer to "should this even exist" is no, the rubric is moot.
