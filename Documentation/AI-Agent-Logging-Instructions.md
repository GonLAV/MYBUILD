# AI Agent Logging Instructions

## Overview
This document provides precise instructions for AI agents to add comprehensive logging using the Bolt Automation Logger (`IAutomationLogger`). Follow these patterns to ensure consistent, actionable logging coverage while maintaining test readability.

## CRITICAL: Logging Scope and Implementation Strategy

### Strict File Modification Rules
When implementing logging, AI agents MUST follow these restrictions:

1. **ONLY Modify User-Specified Tests**: Work only on test methods explicitly mentioned by the user
2. **ONLY Modify Files in User's Working Branch**: Use `git status` and `git diff --name-only` to identify files modified by the user. DO NOT add logging to any file not already modified by the user in their active branch
3. **NO Implementation File Changes**: If the user creates/modifies test files but did not touch implementation files (API clients, page objects, etc.), DO NOT add logging to those implementation files
4. **Respect User's Intent**: If the user only modified test files, only improve logging within those test files

### Multi-Level Analysis Required  
When implementing logging, AI agents MUST:

1. **Check User's Working Branch First**: Review `git status` to see exactly which files the user has modified
2. **Restrict to User-Modified Files Only**: Only add logging to files that appear in `git status` or `git diff --name-only`  
3. **Focus on User-Specified Tests**: Only work on the specific test methods the user mentioned
4. **Maintain Test Readability**: Tests should focus on business logic flow, not be cluttered with infrastructure logging

### Implementation Priority (Most Important First)

1. **Page Object Methods**: Log UI interactions within page object methods themselves
2. **API Client Methods**: Log API calls within client implementation methods
3. **Helper/Utility Methods**: Log data processing within utility methods
4. **Test Data Builders**: Log data creation within builder methods
5. **Test Methods**: Only high-level step orchestration and test-specific validations

### Example: Wrong vs Right Approach

**WRONG - Logging in Test Method**:
```csharp
[Fact]
public async Task LoginTest()
{
    await _logger.ExecuteStepAsync("Navigate to Login", async () =>
    {
        _logger.LogUiAction("Navigate", "LoginPage", loginUrl);
        loginPage.NavigateToLogin();
    });
    
    await _logger.ExecuteStepAsync("Fill Login Form", async () =>
    {
        _logger.LogUiAction("Type", "Username", username);
        _logger.LogUiAction("Type", "Password", "[REDACTED]");
        _logger.LogUiAction("Click", "LoginButton", "Submitting form");
        loginPage.FillLoginForm(username, password);
        loginPage.ClickLogin();
    });
}
```

**RIGHT - Logging in Page Object Methods**:
```csharp
// Test Method - Clean and Readable
[Fact]
public async Task LoginTest()
{
    await _logger.ExecuteStepAsync("Execute Login Workflow", async () =>
    {
        loginPage.NavigateToLogin();
        loginPage.FillAndSubmitLoginForm(username, password);
        _logger.LogBusinessRule("LoginSuccess", loginPage.IsLoggedIn(), "User should be logged in");
    });
}

// Page Object - Contains Implementation Logging
public class LoginPage
{
    public void NavigateToLogin()
    {
        _logger.LogUiAction("Navigate", "LoginPage", _loginUrl);
        _page.GotoAsync(_loginUrl);
        _logger.Info("Login page loaded successfully");
    }
    
    public void FillAndSubmitLoginForm(string username, string password)
    {
        _logger.LogUiAction("Type", "Username", username);
        _usernameField.FillAsync(username);
        
        _logger.LogUiAction("Type", "Password", "[REDACTED]");
        _passwordField.FillAsync(password);
        
        _logger.LogUiAction("Click", "LoginButton", "Submitting login form");
        _loginButton.ClickAsync();
    }
}
```

## AI Agent Implementation Workflow

### Step 1: Analyze User's Working Branch (MANDATORY)
Before implementing ANY logging, examine ONLY the files the user has modified:
```bash
# Commands to identify ONLY user-modified files
git status                    # See all modified files by user
git diff --name-only         # List changed files by user
git log --oneline -n 5       # Recent commits for context
```

**CRITICAL**: Only work with files that appear in the `git status` output. Do NOT modify any other files.

### Step 2: Restrict to User-Modified Files ONLY
Examine each file that the user modified and categorize:

1. **Test Files** (`*Tests.cs`): Focus on the specific test methods the user mentioned
2. **User-Modified Page Objects**: Only if the user modified these files in their branch
3. **User-Modified API Clients**: Only if the user modified these files in their branch  
4. **User-Modified Builders/Helpers**: Only if the user modified these files in their branch

**RULE**: If a file type is not in the user's `git status` output, DO NOT add logging to it.

### Step 3: Implementation Strategy (Restricted)

**If User Modified Test Files Only**:
- Add logging ONLY within the test methods the user specified
- Use step management for test orchestration
- Add business rule and data validations
- DO NOT modify any implementation files (API clients, page objects, etc.)

**If User Modified Implementation Files**:
- Only add logging to the implementation files the user actually modified
- Follow the appropriate patterns for each file type
- Do not modify other implementation files

**If User Modified Both Tests and Implementation**:
- Prioritize implementation file logging (where work happens)
- Clean up test method logging to focus on business logic
- Only work with the files actually modified by the user

### Step 4: Dependency Injection Setup (Only if Required)
Only modify dependency injection if the user's modified files require it:

```csharp
// Only add if the user modified this class and it needs logging
public SomeClass(IAutomationLogger logger)
{
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
}
```

**IMPORTANT**: Do not add dependency injection to classes the user didn't modify.

## Logger Access Patterns

### TestBase Classes
Logger available as `_logger` field. No additional setup required.

### Page Objects and Utility Classes
Inject logger via constructor:
```csharp
public class PageObjectClass
{
    private readonly IAutomationLogger _logger;
    
    public PageObjectClass(IAutomationLogger logger)
    {
        _logger = logger;
    }
}
```

### API Clients
Logger should be injected and used within client methods:
```csharp
public class ApiClient
{
    private readonly IAutomationLogger _logger;
    
    public async Task<Response> CallApiAsync(Request request)
    {
        var stopwatch = Stopwatch.StartNew();
        var httpResponse = await _httpClient.SendAsync(httpRequest);
        await _logger.LogApiCallAsync(httpRequest, httpResponse, stopwatch.ElapsedMilliseconds);
        return response;
    }
}
```

## Core Logging Methods

### Basic Logging
```csharp
_logger.Trace("message", params);    // Low-level diagnostic details
_logger.Debug("message", params);    // Developer information, object states
_logger.Info("message", params);     // General test progress, key actions
_logger.Warning("message", params);  // Potential issues, unexpected values
_logger.Error("message", params);    // Test failures, validation errors  
_logger.Fatal("message", params);    // Critical errors causing test termination
```

### Exception Logging
```csharp
_logger.LogException(exception);                    // Exception only
_logger.LogException(exception, "Context message"); // Exception with context
```

## Step Management
**CRITICAL**: Use steps to structure all test logic. Steps provide hierarchical organization and automatic timing.

Steps mark **test phases**, not individual actions. A page object method that performs one
click logs it with `LogUiAction`; it does not open a step. "Verify retrieval fails with original
credentials" is a phase — "Click on retrieve button" is not.

### Basic Step Pattern
Always open a step through `ExecuteStepAsync` / `ExecuteStep`
(`Bolt.Automation.Common.Logging.Extentions`). The wrapper sets the status from the outcome of
the body: `Passed` on return, `Failed` on exception (which it then rethrows).

```csharp
await _logger.ExecuteStepAsync("Step Name", async () =>
{
    _logger.Info("Step details");
    // Step logic here
}); // Ends the step with timing and the correct status
```

Values produced inside a step can be returned out of it and used by later steps:

```csharp
var friendlyId = await _logger.ExecuteStepAsync("Create application", async () =>
{
    await _d2cHelper.CreateApplicationAndNavigate(_getQuoteApi, requestData);
    return await PageFactory.CreatePage<D2C_YourAddressPage>().GetFriendlyId();
});
```

> **Never open a step with a bare `using (_logger.StartStep(...))`.** `StepScope.Dispose()`
> defaults an un-completed step to `Failed` — it cannot tell whether it is unwinding from an
> exception, so it assumes the worst. A `using` block that returns normally therefore reports
> **Failed on a passing run**: the step shows red in the report while the test is green, and a
> nested one shows red under a parent that shows green. Only `ExecuteStepAsync` / `ExecuteStep`
> can see the `try/catch`, so only they can set the status correctly.

### Manual Step Control
Only when a step's start and end genuinely live in different scopes. You own the status.

```csharp
_logger.StartStep("Step Name", "Optional description");
// Multiple operations
_logger.EndStep(StepStatus.Passed); // .Failed, .Skipped, .Blocked, .Warning
```

If you must hold the scope yourself, call `Complete()` on **every** success path — including
early returns — or the step reports Failed:

```csharp
using var step = _logger.StartStep("Step Name");
if (!await SomethingExists()) { step.Complete("nothing to do"); return; }
// ...
step.Complete();
```

### Nested Steps
```csharp
await _logger.ExecuteStepAsync("Test Setup", async () =>
{
    _logger.Info("Starting test setup");
    
    await _logger.ExecuteStepAsync("Initialize Test Data", async () =>
    {
        _logger.Info("Creating user data");
        // Data creation logic
    });
    
    await _logger.ExecuteStepAsync("Navigate to Page", async () =>
    {
        _logger.Info("Opening application URL");
        // Navigation logic
    });
});
```

## Specialized Logging Methods

### Business Rule Validation
```csharp
_logger.LogBusinessRule("RuleName", passed: true/false, "Rule description or failure reason");
```

### API Call Logging
```csharp
await _logger.LogApiCallAsync(httpRequest, httpResponse, durationMs);
```

### UI Action Logging
```csharp
_logger.LogUiAction("Click", "LoginButton", "Submitting login form");
_logger.LogUiAction("Type", "UsernameField", value);
_logger.LogUiAction("Navigate", "HomePage", targetUrl);
```

### Data Validation
```csharp
_logger.LogDataValidation("ValidationType", passed: bool, expected: "value", actual: "value", "Details");
```

### JSON Object Logging
```csharp
_logger.LogJson("API Response", responseObject);              // Basic JSON
_logger.LogJsonWithPreview("User Data", userObject);         // With preview
_logger.LogJsonStyled("Configuration", configObject);        // Enhanced styling
```

### File Attachment
```csharp
_logger.AttachFile(screenshotPath, "Screenshot after login failure");
```

## Test Structure Pattern

Apply this logging pattern to every test method:

```csharp
[Fact]
public async Task TestMethodName()
{
    await _logger.ExecuteStepAsync("Test Setup", async () =>
    {
        _logger.Info("Initializing test data for {0}", testScenario);
        // Setup logic with Info logs for key actions
    });
    
    await _logger.ExecuteStepAsync("Execute Test Action", async () =>
    {
        _logger.Info("Performing main test operation");
        
        // Log UI actions
        _logger.LogUiAction("Click", "TargetButton", "Initiating workflow");
        
        // Log API calls if applicable
        await _logger.LogApiCallAsync(request, response, duration);
        
        // Log business validations
        _logger.LogBusinessRule("RequiredFieldValidation", passed, details);
    });
    
    await _logger.ExecuteStepAsync("Verify Results", async () =>
    {
        _logger.Info("Validating test outcomes");
        
        // Log all validations
        _logger.LogDataValidation("ExpectedValue", passed, expected, actual, context);
        
        if (testFailed)
        {
            _logger.Error("Test validation failed: {0}", failureReason);
            _logger.AttachFile(screenshotPath, "Failure evidence");
        }
    });
}
```

## Log Level Guidelines

- **Trace**: Object serialization, detailed state inspection, low-level diagnostics
- **Debug**: Variable values, conditional logic outcomes, intermediate calculations  
- **Info**: Test progress milestones, user actions, API calls, navigation steps
- **Warning**: Non-critical issues, fallback scenarios, performance concerns
- **Error**: Test failures, validation errors, exception conditions
- **Fatal**: Test infrastructure failures, environment issues

## Required Logging Points

Add logging at these mandatory points:

1. **Test Method Start**: Log test purpose and parameters
2. **Each Major Step**: Use ExecuteStepAsync/ExecuteStep for logical test sections
3. **External API Calls**: Use LogApiCallAsync for all HTTP requests
4. **UI Interactions**: Use LogUiAction for clicks, typing, navigation
5. **Business Logic Validations**: Use LogBusinessRule for all rule checks
6. **Data Validations**: Use LogDataValidation for expected vs actual comparisons — **unless the actual value already reaches the report**, either from the assertion's own failure message (`$"... Actual: {x}"`) or from the helper/page object that gathered and logged it. In those cases skip it; a duplicate line is noise, not diagnostics.
7. **Exception Handling**: Use LogException in all catch blocks
8. **Test Completion**: Log final outcome with relevant context

## Context-Specific Patterns

### API Tests
```csharp
await _logger.ExecuteStepAsync("API Request Preparation", async () =>
{
    _logger.Info("Building {0} request to {1}", httpMethod, endpoint);
    _logger.LogJson("Request Payload", requestObject);
});

await _logger.ExecuteStepAsync("Execute API Call", async () =>
{
    await _logger.LogApiCallAsync(request, response, stopwatch.ElapsedMilliseconds);
});

await _logger.ExecuteStepAsync("Response Validation", async () =>
{
    _logger.LogDataValidation("StatusCode", response.IsSuccessStatusCode, "200", response.StatusCode.ToString());
    _logger.LogJsonWithPreview("Response Data", responseObject);
});
```

### UI Tests
```csharp
await _logger.ExecuteStepAsync("Page Navigation", async () =>
{
    _logger.LogUiAction("Navigate", "LoginPage", targetUrl);
    _logger.Info("Waiting for page load completion");
});

await _logger.ExecuteStepAsync("Form Interaction", async () =>
{
    _logger.LogUiAction("Type", "Username", "Entering user credentials");
    _logger.LogUiAction("Type", "Password", "[REDACTED]");
    _logger.LogUiAction("Click", "LoginButton", "Submitting form");
});

await _logger.ExecuteStepAsync("Result Verification", async () =>
{
    _logger.LogUiAction("Verify", "WelcomeMessage", "Checking login success");
    _logger.LogDataValidation("LoginResult", isSuccess, "Success", actualResult);
});
```

### Data-Driven Tests
```csharp
await _logger.ExecuteStepAsync($"Test Iteration: {testCase.Name}", async () =>
{
    _logger.LogJson("Test Case Data", testCase);
    
    await _logger.ExecuteStepAsync("Execute Test with Data", async () =>
    {
        // Test execution with data logging
    });
});
```

## Anti-Patterns (Do Not Use)

1. **Over-logging in test methods**: Do not duplicate implementation logging in tests
2. **Logging in wrong abstraction layer**: Add logging where work happens, not where work is orchestrated
3. **Ignoring modified files**: Only adding logging to test methods while ignoring page objects/API clients
4. **No logging in loops without context**: Add step or meaningful message
5. **Logging sensitive data**: Passwords, tokens, PII - use "[REDACTED]"
6. **Empty steps**: Every step must contain meaningful operations
7. **Missing step end calls**: Always use `using` statements or explicit EndStep
8. **Generic error messages**: Include specific context and expected behavior
9. **Logging without levels**: Choose appropriate level based on information importance

### Most Critical Anti-Pattern
**DO NOT** modify implementation files that the user did not touch:
```csharp
// WRONG - User only modified test file, but agent modified page object
// In SSOTests.cs (user modified this)
[Fact]
public void TestSSO() { /* test code */ }

// In PageObject.cs (user did NOT modify this - DO NOT TOUCH)
public void ClickSubmit()
{
    _logger.LogUiAction("Click", "SubmitButton", "Submitting form"); // WRONG!
    _submitButton.ClickAsync();
}
```

**CORRECT** approach when user only modified test files:
```csharp
// User only modified SSOTests.cs, so only improve logging there
[Fact]
public async Task TestSSO()
{
    await _logger.ExecuteStepAsync("Execute SSO Test", async () =>
    {
        // Add logging only within the test method
        _logger.Info("Starting SSO authentication");
        
        var result = ssoClient.Authenticate();
        
        _logger.LogBusinessRule("SSO Success", result.IsSuccess, "SSO should succeed");
        _logger.LogDataValidation("RedirectUrl", result.RedirectUrl.Contains("expected"), "expected", result.RedirectUrl);
    });
}

// PageObject.cs - DO NOT MODIFY (user didn't touch this file)
public void ClickSubmit()
{
    _submitButton.ClickAsync(); // Keep as-is
}
```

### File Modification Examples

**Scenario 1: User only modified test file**
```bash
git status
# M  Bolt.Automation.Tests/Tests/SSOTests.cs
```
**Action**: Only add logging within SSOTests.cs methods. Do NOT touch any other files.

**Scenario 2: User modified test and page object**
```bash
git status  
# M  Bolt.Automation.Tests/Tests/SSOTests.cs
# M  Bolt.Automation.FrontEnds/Projects/Pages/LoginPage.cs
```
**Action**: Add logging to both files, but prioritize LoginPage.cs implementation logging.

**Scenario 3: User created new test using existing API client**
```bash
git status
# A  Bolt.Automation.Tests/Tests/NewApiTests.cs
# (SsoApiFactory.cs is NOT in git status)
```
**Action**: Only add logging to NewApiTests.cs. Do NOT modify SsoApiFactory.cs even if it would be "better" design.

## Performance Considerations

- Steps are lightweight but avoid excessive nesting (>5 levels)
- JSON logging truncates large objects automatically
- Use Debug/Trace levels for verbose output
- LogApiCallAsync is async-optimized for non-blocking logging

## Integration Notes

- All logs appear in the NUnit test output automatically
- NLog configuration supports file output and external targets
- Step hierarchy maintains context across async operations
- Logger instance is scoped per test for thread safety