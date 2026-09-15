---
topic: philosophy:logging-where-work-happens
summary: Logging lives in page objects + API clients, never in test methods — keeps tests as orchestration.
status: ready
---

# Logging where work happens

Logging belongs in the file that's doing the work, not the file that's asking for the work.

A `FillForm` call on a page object emits structured logs about which fields got filled and which were skipped — the page object knows that, the test does not. An API client's request/response logging happens inside the client, not at the call site — the client knows the headers, durations, and status; the test only knows it asked for a quote.

When the agent adds logging, the question to ask is: *whose work am I describing?* If the answer is "the page object's work," the logging belongs in the page object. If "the API client's work," in the client. The test method itself orchestrates these calls and asserts the business outcome — but it doesn't narrate the implementation.

## The contract

The Bolt logging policy ([Documentation/AI-Agent-Logging-Instructions.md](../../AI-Agent-Logging-Instructions.md)) makes this concrete:

1. Only modify files the user has changed.
2. Log where work happens — page objects, API clients, helpers. Not test methods.
3. Test methods stay clean — only step orchestration and business validations.
4. Use steps (`ExecuteStepAsync`) for all test logic.
5. Never log sensitive data.

This file is the *why*. The Instructions doc is the *what*. Read both.

## Why this matters

When logging is duplicated between the test method and the implementation it calls, three things break:

1. **The report becomes unreliable.** If `FillForm` logs "filled VIN" inside the page object AND the test logs "filling VIN" before the call, the MongoDB log has two entries for one action. Counts get inflated. Step durations drift. Diagnostic value drops.
2. **Logging quality regresses on every new test.** When the page object owns the logging, every test that uses that page object gets it. When the test owns the logging, every NEW test has to re-derive it from scratch — and the re-derivation is always worse than the first version (different field names, different granularity, different verbiage).
3. **The test becomes unreadable as business prose.** A test method that says `_logger.Info("Click vehicle button")` mid-flow is doing both jobs: orchestrating the framework AND narrating the result. The narration crowds out the orchestration. After three rounds of "log this step too please," the test reads like a play-by-play, not a specification.

Push the logging down. The test becomes shorter. Every other test that uses the same page object becomes better-instrumented for free.

## The exception — `ExecuteStepAsync` in the test method

Tests *do* use the logger, but only for one thing: wrapping logical phases in `_logger.ExecuteStepAsync("Step name", async () => { … }, "expected outcome")`. This is structural, not narrative. The step name is the test's table of contents. The expected outcome is what the report shows next to the green / red dot.

A test that uses `ExecuteStepAsync` 4–5 times is well-structured. A test that uses `_logger.Info` 4–5 times is doing logging-as-narration, which belongs in the implementation files those steps call. The line:

```csharp
_logger.Info("Clicking continue button");
await startPage.ClickContinue();
```

…is wrong twice over. The page object already logs the click; the test shouldn't pre-narrate it. The right shape is:

```csharp
await _logger.ExecuteStepAsync("Submit Start page", async () => {
    await startPage.ClickContinue();
}, "User lands on next page");
```

The step name and expected outcome describe the *business intent*. The page object handles the *mechanics narrative*.

## The connection to fluent page objects

This principle only works if the page object's methods are named after what the user is doing — `ClickContinue`, `FillForm`, `SelectLob`. When the page object is fluent ([fluent-page-objects.md](fluent-page-objects.md)), its log entries naturally read at the right level of abstraction. A page object that exposes `Page.Locator(".x").ClickAsync()` can't log meaningfully — it doesn't know whether the click was a Continue, a Submit, or a Cancel.

Logging-where-work-happens depends on knowing what the work is. Fluent page objects know.

## Anti-patterns

- **`_logger.Info("Calling FillForm")` immediately before `await page.FillForm(...)`.** The page object will log its work. Remove the redundant line.
- **`_logger.Info` to mark a step.** Use `ExecuteStepAsync` — it creates a real step scope in the report.
- **Logging the same data twice (request body in the test AND in the API client).** Pick the lower layer (the client) and remove the test-side log.
- **Adding logging to a file the user didn't touch.** Drives by adding logging across the codebase create noisy diffs that hide the actual change. Stay in scope.

## Cross-references

- [../framework/logging.md](../framework/logging.md) — the IAutomationLogger interface and step idioms.
- [Documentation/AI-Agent-Logging-Instructions.md](../../AI-Agent-Logging-Instructions.md) — the operational policy.
- [tests-stay-clean.md](tests-stay-clean.md) — why pushing logging out of the test makes the test better.
- [../recipes/troubleshooting/test-body-logging-missing-steps.md](../recipes/troubleshooting/test-body-logging-missing-steps.md) — the symptom of getting this wrong.
