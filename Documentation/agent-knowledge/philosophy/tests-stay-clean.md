---
topic: philosophy:tests-stay-clean
summary: Test methods are high-level step orchestration + business validations; no Playwright calls, no DOM grep, no log Info().
status: ready
---

# Tests stay clean

A test method has exactly two jobs: orchestrate steps, and assert business outcomes. If it's doing anything else, the code that's doing the something-else belongs in a different file.

A clean test reads top-to-bottom as a paragraph: log in, navigate to the start page, set up the data that diverges from defaults, run the flow, check the rates appeared. No Playwright. No DOM. No logging beyond `ExecuteStepAsync`. No data-builder helpers. No private `BuildFormData()` method buried at the bottom of the class.

## What "clean" looks like

```csharp
[Test, Tenant(Tenant.KRAFTLAKEX), Category("KLX"), Author(Author.Viktor), TestCaseId(240782)]
public async Task KLX_CLAuto_OldInterview_E2E_SubmitQuote()
{
    var user = TestContextAccessor.CurrentUserCollection.Agent
        ?? throw new TestSetupException("KLX Staging Agent user not configured");
    var loginUrl = ScopeContext.Data.UrlDataCollection.FrontEnd?.LoginUrl
        ?? throw new TestSetupException("KLX Staging LoginUrl not configured");

    var personalInfo = PersonalInfo.GetRandomPersonalInfo();
    var driver = Drivers.KLXTestDriver;
    var address = AddressData.TX;
    var effectiveDate = DateTime.Today.AddDays(2).ToString("MM/dd/yyyy");

    await _adbxHelper.LoginAsync(user, loginUrl);
    // ... open existing account, click NEW QUOTE, resolve popups ...

    ScopeContext.Set(ctx => ctx.FrontEnd, FrontEndType.Interview);
    var startPage = PageFactory.CreatePage<Product_StartPage>();

    var formData = CLAutoFormData.Defaults;
    formData[FieldNames.OrganizationName] = "Test Automation";
    formData[FieldNames.VIN] = "4RAVS1629TK143345";
    formData[FieldNames.OperatorDateOfBirth] = DateTime.Parse(driver.DOB).ToString("MM/dd/yyyy");
    formData[FieldNames.EffectiveDate] = effectiveDate;
    formData[FieldNames.Email] = personalInfo.Email;
    // (the rest of the per-test divergences from CLAutoFormData.Defaults)

    var resultsPage = await Executor.ExecuteToPage<Product_ResultsPage>(
        FlowType.InterviewCLAutoFlow, startPage, formData, fillForms: true);

    await _logger.ExecuteStepAsync("Verify rates returned", async () => {
        var hasRates = await resultsPage.IsGetRates();
        _logger.LogDataValidation("Has Rates", hasRates, "true", hasRates.ToString(),
            "Quote should return at least one rate");
        Assert.That(hasRates, Is.True, "No rates returned on the result page");
    });
}
```

A non-engineer can read this and explain what it does. That's the bar.

## What "not clean" looks like

```csharp
[Test]
public async Task KLX_CLAuto_E2E()
{
    _logger.Info("Starting KLX CL Auto test");                       // ← narrative logging
    var formData = BuildFormData();                                  // ← hidden helper
    var creds = "lsp1_aortest@test.com";                            // ← hardcoded data
    var loginUrl = "https://kraftlake-staging.boltqa.com/login";    // ← hardcoded URL
    await BrowserManager.NavigateAsync(loginUrl);
    _logger.Info("Filling login form");                              // ← narration
    await Page.Locator("#username").FillAsync(creds);                // ← raw Playwright in test
    await Page.Locator("#password").FillAsync("Seapass1");
    _logger.Info("Clicking submit");                                 // ← narration
    await Page.Locator("button[type=submit]").ClickAsync();
    // ... 150 more lines ...
}

private Dictionary<string,string> BuildFormData() {                  // ← helper that hides intent
    return new() { ["FirstName"] = "AutoTest", /* 40 more entries */ };
}
```

Six different philosophy violations in twenty lines. Each one is "small" by itself; together they make the test unreviewable.

## The discipline

When reviewing a test (or your own draft), check for these — they're the most common leaks:

1. **Raw Playwright at the test layer.** `Page.Locator`, `IPage`, `ILocator` should not appear. The page object exposes a named method; use that. (See [fluent-page-objects.md](fluent-page-objects.md).)
2. **`_logger.Info` for step orchestration.** Use `ExecuteStepAsync`. (See [logging-where-work-happens.md](logging-where-work-happens.md).)
3. **`BuildFormData()` / `Setup<Whatever>()` helpers.** Inline the dictionary. If the data is reusable across tests, pull it into `ApplicationTestData.<Flow>FormData.Defaults` and layer per-test overrides — not into a private helper.
4. **Hardcoded URLs, credentials, or tenant-specific data.** Read from `ScopeContext.Data.UrlDataCollection` and `TestContextAccessor.CurrentUserCollection`.
5. **Re-supplying field values that match the registry default.** Sparse dictionaries only. (See [sparse-dictionaries.md](sparse-dictionaries.md).)
6. **Logic that branches on UI state.** If the test has `if/else` around UI behavior, that's a popup or a conditional flow — model it in the page object or as a separate flow, not in the test body.
7. **`Thread.Sleep` anywhere.** Use Playwright waits or the appropriate registry timeout. A test with `Sleep` is a test that's papering over a real synchronization issue.
8. **Assertions that live outside the test body.** `Assert.*` belongs in the test method — it's half of the test's job (orchestrate + assert). A helper or page object may *gather* the data to check and *log* it, but it must return that data for the test to assert on; it must not hide the pass/fail decision behind a thrown "validation" exception. A test whose body has no `Assert` isn't asserting a business outcome — it's delegating its own contract. (Some older helpers assert internally and throw — e.g. a `Validate…Async` that throws a `…ValidationException`. Don't copy that shape for new tests; have the helper return the values and assert in the test, using `Assert.Multiple` for per-item loops.)
9. **`// Step N — …` narration comments over `ExecuteStepAsync`.** The step name *is* the narration: `ExecuteStepAsync("Select the carrier and open its Custom package", …)` already says what the block does — a `// Step 2 — Select the carrier and open its Custom package` comment above it is pure duplication. Drop it. If a line needs explaining, explain the *non-obvious why* (a race, a backend quirk, a deliberate omission), not the *what*. See [compact-comments.md](compact-comments.md).

A test that passes all nine checks reads as orchestration + validation. That's the whole job.

## Why this matters

A clean test is the only kind that survives.

The framework will change underneath the test (new tenant, new override pattern, new login flow). Page-object code will be rewritten; logging will be refined; the data store will move. None of that should require touching the test. A clean test depends only on the *fluent surface*; it doesn't reach into mechanics. The mechanics churn beneath it, invisibly.

A dirty test, by contrast, has tendrils into every layer. Change the field-name convention? Every dirty test needs an edit. Move the login URL? Every dirty test needs an edit. Refactor the popup handler? Some subset of dirty tests was reaching past it and silently depends on the old shape.

Keeping the test layer thin is what lets the framework layer evolve. It's not aesthetic — it's a maintenance contract.

## Why the existing AI logging instructions matter

[Documentation/AI-Agent-Logging-Instructions.md](../../AI-Agent-Logging-Instructions.md) is the operational enforcement of this principle for the logging dimension. The five rules in that document — only modify changed files, log where work happens, tests stay clean, use steps, never log sensitive data — are derived from this philosophy. Read both. The operational rules tell you what to do; this file tells you why.

## Cross-references

- [fluent-page-objects.md](fluent-page-objects.md) — the abstraction that makes clean tests possible.
- [logging-where-work-happens.md](logging-where-work-happens.md) — clean tests don't carry logging.
- [sparse-dictionaries.md](sparse-dictionaries.md) — clean dictionaries are a precondition for clean tests.
- [../framework/test-class.md](../framework/test-class.md) — the minimal skeleton.
- [../recipes/test-review-rubric.md](../recipes/test-review-rubric.md) — the operational checklist.
