---
topic: framework:logging
summary: IAutomationLogger — StartStep, LogApiCallAsync, LogBusinessRule, LogDataValidation.
status: ready
---

# Logging — mechanism

> **When to read:** Phase 3 (writing logging into the test), Phase 4 (interpreting failure logs).
>
> **Doctrine (READ FIRST):** [Documentation/AI-Agent-Logging-Instructions.md](../../AI-Agent-Logging-Instructions.md) — non-negotiable rules for where logging belongs, what to redact, when to add steps. This file documents the *interface*; that document documents the *policy*. Do not duplicate the policy here.
>
> **Companion file:** [../philosophy/logging-where-work-happens.md](../philosophy/logging-where-work-happens.md) — the *why* behind the policy.

## `IAutomationLogger` — the interface

Defined in `Bolt.Automation.Common/Logging/Core/IAutomationLogger.cs`:

```csharp
// Levels
void Log(LogLevel level, string message, params object[] args);
void Info(string message);
void Debug(string message);
void Warning(string message);   // NOT Warn — common typo
void Error(string message);
void Fatal(string message);
void LogException(Exception exception, string? message = null);

// Steps
IStepScope StartStep(string stepName, string? description = null);
void EndStep(StepStatus status = StepStatus.Passed);

// Specialized
void LogBusinessRule(string ruleName, bool passed, string? details = null);
Task LogApiCallAsync(HttpRequestMessage request, HttpResponseMessage response, long durationMs);
void LogUiAction(string actionType, string element, string? details = null);
void LogDataValidation(string validationType, bool passed, string expected, string actual, string? details = null);
void LogJson(string message, object data, LogLevel level = LogLevel.Debug);
```

**Common typo:** `_logger.Warn(...)` does not exist — it's `_logger.Warning(...)`. The compiler catches it but the error message is unhelpful. See [../recipes/troubleshooting/logger-method-naming.md](../recipes/troubleshooting/logger-method-naming.md).

## `ExecuteStepAsync` — the primary idiom

`Bolt.Automation.Common/Logging/Extentions/AutomationLoggerExtensions.cs`. Variants:

```csharp
// Async, no return
Task ExecuteStepAsync(this IAutomationLogger? logger,
    string stepName, Func<Task> action, string? description = null);

// Async, returns T
Task<T> ExecuteStepAsync<T>(this IAutomationLogger? logger,
    string stepName, Func<Task<T>> func, string? description = null);

// Sync versions: ExecuteStep / ExecuteStep<T>
```

Behavior: wraps in `StartStep()` scope, marks `Complete()` on success, `Fail(ex.Message)` on exception, then **rethrows**. The test still fails — logging is non-suppressing.

Usage:

```csharp
await _logger.ExecuteStepAsync("Open the first existing account", async () => {
    var homePage = PageFactory.CreatePage<ADBX_HomePage>();
    await homePage.ClickOnMenuTab(NavigationType.Accounts);
    PageFactory.CreatePage<ADBX_AccountsTabPage>();
    await _pageHelper!.SelectTableRowAsync(1);
    return PageFactory.CreatePage<ADBX_AccountSummaryPage>();
}, "Account Summary page is displayed");
```

The optional third argument is the **expected outcome** that's logged for reporting.

## `LogBusinessRule` — pass/fail of a domain rule

```csharp
_logger.LogBusinessRule("Account Existence Validation", found,
    $"Account with email '{email}' {(found ? "exists" : "does not exist")}");
```

Use when the test outcome depends on a true/false rule that isn't a direct assertion. Example: "this account exists in this tenant," "the popup matched the soft variant," "the page rendered with no error banners."

## `LogDataValidation` — expected vs actual

```csharp
_logger.LogDataValidation("Has Rates", hasRates, "true", hasRates.ToString(),
    "Quote should return at least one rate");
Assert.That(hasRates, Is.True, "No rates returned on the result page");
```

When you use it, log the validation **before** the assertion — if the assertion throws, the log line still made it.

### When it is not needed

`LogDataValidation` exists so the actual value isn't invisible in the report. **Skip it when the actual value is already printed**, either by:

- **the assertion's own failure message** — `Assert.That(status.SecondaryStatus, Is.EqualTo(nameof(QuoteSecondaryStatus.UUD)), $"SecondaryStatus was not UUD. Actual: {status.SecondaryStatus}")` already names what arrived; or
- **the helper or page object that gathered the value** — a read that logs `$"QuoteStatus reports {primary} / {secondary}"` has already put it in the report, which is logging where the work happens.

In those cases a `LogDataValidation` line is duplication, not diagnostics. Reach for it when the value is otherwise unreported — a bare `Assert.IsTrue(flag)`, or a boolean whose message can't carry the underlying values.

## `LogUiAction` — for page-object internals

When a page-object method does something specific (like clicking a non-obvious element), log it inside the page object:

```csharp
_logger?.LogUiAction("Click", "Get Quotes", "KLX Markets page continue");
await button.ClickAsync(...);
```

Test methods don't call `LogUiAction` directly — that breaks the "log where work happens" rule (see [the policy doc](../../AI-Agent-Logging-Instructions.md)).

## `LogJson` — for API payloads

```csharp
_logger.LogJson("New Quote payload", payload, LogLevel.Debug);
```

Useful inside API clients. Avoid in UI tests (page-object methods don't have payloads to dump).

## Step granularity

A step is a logical chunk of test work. Rough guidelines:

- One per "manual TC step" (login, open account, fill page, click continue).
- Not finer (don't wrap each click).
- Not coarser (don't put the entire test in one step).

For TC 240782 we used 4 steps for the ADBX phase (login, open account, click NEW QUOTE / handle popups, switch tabs) + the executor walked the Interview pages with implicit per-page steps from `Executor`'s own logging.

## Exception hierarchy

`Bolt.Automation.Common/Exceptions/AutomationExceptions.cs`. All inherit from `AutomationException`. When to throw:

| Exception | When |
|---|---|
| `PageCreationException(pageTypeName, inner)` | Page object instantiation fails (browser unavailable, ctor fails). Rare in test code; framework throws it. |
| `PageElementException(elementDescription, detail, inner)` | Element not found/not interactable/in unexpected state. Used heavily in `ClickContinueButton` ("Could not find or click any continue button…"). |
| `PopupTimeoutException(popupName, action, timeoutMs, inner)` | Popup fails to appear or its button is unclickable within timeout. |
| `NavigationException(fromPage, toPage, url, attempts, inner)` | Navigation fails after retries. |
| `CarrierNotFoundException(carrierName, context, available)` | Expected carrier missing on results page; lists available carriers. |
| `CoverageValidationException(summary)` | Coverage dropdown values don't match expected. |
| `TestSetupException(message)` | **Test author throws this** — missing API data, unconfigured URLs, absent test data, ScopeContext not set. |
| `ApiResponseException(message, [inner])` | Invalid/unexpected API response (missing IDs, no quotes, etc.). |

In a test method, `TestSetupException` is the one you throw most often — usually with the null-coalescing operator:

```csharp
var user = TestContextAccessor.CurrentUserCollection.Agent
    ?? throw new TestSetupException("KLX Staging Agent user not configured");
```

Don't catch these to "soften" the failure. They mean the test can't run; let them propagate.

## Linking logs to test reporting

The MongoDB logger ingests step events keyed by test ID. After a passing run, spot-check (per the root `CLAUDE.md` verification step) that:

- The test ID matches `[TestCaseId]` value.
- All step entries have non-null `expectedResult` strings (i.e. you passed the third argument to `ExecuteStepAsync`).
- No `{placeholder}` template strings appear in the message bodies — those mean the logger interpolation failed (string concatenation issue or wrong arg count). Use `$""` interpolation, never `string.Format` for log messages.

## Checklist before considering logging done

- [ ] Test method has at most ~5 `ExecuteStepAsync` blocks for ADBX phase + 1 final assertion step.
- [ ] Each `ExecuteStepAsync` has a third-argument `expectedResult` string.
- [ ] Every assertion's actual value reaches the report — via its own failure message, via the helper that gathered it, or failing both, via a preceding `LogDataValidation`.
- [ ] No raw `Console.WriteLine` in the test (use `_logger.Info`).
- [ ] Page-object internals (overrides, helpers) `_logger?.Info(...)` significant decisions, not every line.
- [ ] No sensitive data anywhere — search the diff for password/token/email-with-PII before committing.
