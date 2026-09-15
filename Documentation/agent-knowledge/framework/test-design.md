---
topic: framework:test-design
summary: Principles for designing tests — what to assert, how to scope, when to split (forward-port from V1 test-design skill).
status: ready
---

# Test design — principles

> **When to read:** Before writing a new test from a TC or feature description; before splitting a macro-test that's grown unwieldy.
>
> **Forward-ported from:** V1 `test-design` skill (`.skill-explore/agent-kit/.claude/skills/test-design/SKILL.md`). Updated for V2 framework realities (TestBase/UITestBase split, `.SetProperty("TestCaseId", id)` parameterization).

## Input

A user story, feature description, TC, or acceptance criteria — anything that describes a behavior the team wants verified. If the input is vague, ask the user to clarify the acceptance criteria before proceeding.

## Step 1 — Risk analysis

For the feature, answer:
- What can go wrong?
- What's the business impact of a regression here? (Insurance domain — wrong quote, wrong policy, wrong tenant data, wrong eligibility decision.)
- What's the most likely regression path? (UI binding, API contract, validation rule, feature-flag state, tenant scoping.)

Use the answers to prioritize which behaviors get automated first.

## Step 2 — Behavior breakdown

Decompose into atomic behaviors. Apply the **axis of decomposition** rule:

- Scenarios that fail for the **same reason** if the system breaks → ONE test, `[TestCaseSource]` rows. (e.g. five invalid-input variants all rejected by the same validator.)
- Scenarios that fail for **different reasons** → SEPARATE tests. (e.g. "missing required field" vs. "invalid format" — different code paths, different failure modes.)

## Step 3 — Layer assignment

For each behavior, pick the lowest layer that gives confidence:

| Layer | Use when | Bolt project |
|-------|----------|--------------|
| Unit | Pure C# logic in helpers/utilities — no external deps | the project that owns the code |
| API | Behavior is observable at the API contract (validation, response shape, status, side effect on a downstream service) | `Bolt.Automation.Tests` + `Bolt.Automation.ApiClients` (inherit `TestBase`) |
| UI | Behavior is only observable in the browser (rendering, field interactions, navigation, role-based visibility) | `Bolt.Automation.Tests` + `Bolt.Automation.FrontEnds` (inherit `UITestBase`) |

Default to the lowest layer. UI tests are the most expensive — reserve them for things only the UI can verify.

## Step 4 — Generate test cases

For each behavior, produce:

- **Class name** — `{Tenant}_{Feature}Tests`, file name matches (e.g. `KLX_CLInterviewTests.cs`).
- **Method name** — `Method_ExpectedBehavior_WhenCondition` (or the V2 convention `{Tenant}_{Lob}_{Feature}_{Action}` for E2E flow tests).
- **Base class** — `TestBase` for API-only, `UITestBase` for browser-driven. See [test-class.md](test-class.md).
- **Mandatory attributes** — `[Test]` (or `[TestCaseSource]`), `[Tenant(...)]`, `[TestCaseId(...)]`, `[Category(...)]`, `[Author(...)]`, `[RunIn(Environment.X)]` where applicable.
- **Test data source** — `Bolt.Automation.TestDataProvider` (per-flow profile like `CLAutoFormData.Defaults`) or inline `[TestCaseSource]` method.
- **LD flag requirements** — list any flags that must be set in `[SetUp]` and reset in `[TearDown]`.
- **Logger context** — tenant, correlationId, primary entity ID (quoteId / policyId / submissionId) attached in setup.

## Anti-patterns

- **Over-splitting (wrong axis)** — splitting per field when all fields fail for the same validation rule. Merge into one `[TestCaseSource]`.
- **Under-splitting (macro-test)** — one 200-line test covering login + nav + form fill + submit + verify. Split per behavior; let earlier behaviors live in their own focused tests.
- **Wrong layer** — verifying an API response shape by reading it off the UI. Use an API test.
- **Skipping tenant context** — never. `[Tenant]` is mandatory on every test.
- **`Thread.Sleep`** — never. Playwright waits or `WaitUtils`.

## Worked example

Feature: "Quote form rejects invalid VIN."

**Wrong decomposition** — one test per invalid VIN format (too short, too long, has letters O/I/Q, has special chars). Four tests, four identical bodies. → Merge into ONE test with `[TestCaseSource]` listing all invalid formats.

**Wrong layer** — UI test that fills the VIN field and reads the error toast. → API test that POSTs to the quote endpoint and asserts the 400 response shape. UI test only if the *rendering* of the error (toast position, ARIA live region) is part of the acceptance criteria.

**Right decomposition** — note the use of `.SetProperty("TestCaseId", id)` per variant (the V2 NUnit parameterized convention; see root `CLAUDE.md`):

```csharp
public class VinValidationTests : TestBase
{
    [Test]
    [Tenant(Tenant.BOLTAG)]
    [Category("Regression")]
    [Category("API")]
    [Author(Author.Viktor)]
    [TestCaseSource(nameof(InvalidVins))]
    public async Task SubmitQuote_Returns400_WhenVinFormatInvalid(string vin, string expectedError)
    {
        // ...
    }

    private static IEnumerable<TestCaseData> InvalidVins()
    {
        yield return new TestCaseData("ABC", "VIN must be 17 characters")
            .SetProperty("TestCaseId", "100001");
        yield return new TestCaseData("1HGCM82633A00400O", "VIN cannot contain letters O, I, Q")
            .SetProperty("TestCaseId", "100002");
        // ...
    }
}
```

## Cross-references

- [test-class.md](test-class.md) — test class skeleton, attribute table, base classes.
- [../philosophy/when-to-automate.md](../philosophy/when-to-automate.md) — when to skip automating.
- [../philosophy/tests-stay-clean.md](../philosophy/tests-stay-clean.md) — what stays in the test vs the page object.
- [../recipes/test-review-rubric.md](../recipes/test-review-rubric.md) — 10-dimension review.
