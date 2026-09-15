# Playwright Frontend Helpers — Improvement Analysis

**Playwright version:** `1.53.0 → 1.58.0` (current: 1.60.0 — findings below re-verified 2026-08-25, all still open)  
**Scope:** `Bolt.Automation.FrontEnds` — `PlaywrightBase/` helpers and project-specific helpers

---

## Summary

After reviewing all helpers against the Playwright 1.54–1.58 changelog, **two categories** of improvements were identified:

| Category | Count | Impact |
|---|---|---|
| **Quick wins** — existing code using wrong/redundant Playwright APIs | 5 | Reliability & speed |
| **New feature opportunities** — Playwright 1.54–1.58 features not yet adopted | 5 | Capability & coverage |

---

## 🔧 Quick Wins — Existing Code Issues

### 1. `WaitHelper.WaitForElementAsync` — Manual retry loop over `WaitForAsync`

**File:** `PlaywrightBase/Helpers/WaitHelper.cs`

**Problem:**  
The method wraps `locator.WaitForAsync()` in a manual `for` loop with `Task.Delay(1000)` between attempts. `WaitForAsync` already retries internally via Playwright's built-in retry mechanism. The outer loop just multiplies the total wait time unnecessarily and adds 1 full second of dead time between each attempt even when the element appears immediately after the previous timeout.

```csharp
// ❌ Current — manual retry wraps a method that already retries
for (int attempt = 0; attempt < retries; attempt++)
{
    try
    {
        await locator.WaitForAsync(new LocatorWaitForOptions { State = waitState, Timeout = timeout });
        return locator;
    }
    catch (TimeoutException) when (attempt < retries - 1)
    {
        await Task.Delay(1000); // dead time — Playwright already waited `timeout` ms
    }
}
```

```csharp
// ✅ Recommended — single call with total budget
var totalTimeout = timeout * retries;
await locator.WaitForAsync(new LocatorWaitForOptions { State = waitState, Timeout = totalTimeout });
return locator;
```

---

### 2. `ElementInteractionHelper` — Dead `CountAsync()` check after `WaitForAsync(Attached)`

**File:** `PlaywrightBase/Helpers/ElementInteractionHelper.cs`

**Problem:**  
After a successful `WaitForAsync(State = Attached)`, the element is guaranteed to exist in the DOM. The `CountAsync() == 0` guard that follows is dead code — it can never be true if `WaitForAsync` didn't throw.

```csharp
// ❌ Current — CountAsync after WaitForAsync(Attached) is unreachable for 0
await locator.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Attached, Timeout = waitTimeout });

if (await locator.CountAsync() == 0)  // dead code
{
    logger?.Debug($"Locator '{locatorValue}' not found, trying next");
    continue;
}

if (await locator.IsVisibleAsync())  // separate network round-trip
```

```csharp
// ✅ Recommended — wait directly for Visible state; if it throws, catch and continue
try
{
    await locator.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = waitTimeout });
    // element is guaranteed visible here — no extra round-trips needed
}
catch (TimeoutException)
{
    logger?.Debug($"Locator '{locatorValue}' not visible, trying next");
    continue;
}
```

This removes two extra async round-trips (`CountAsync` + `IsVisibleAsync`) per locator attempt.

---

### 3. `WaitHelper.WaitForElementsValuesAsync` — `EvaluateAsync("el => el.value")` vs `InputValueAsync()`

**File:** `PlaywrightBase/Helpers/WaitHelper.cs`

**Problem:**  
Raw JS evaluation is used to read `<input>` values. Playwright has a first-class API for this that is safer (returns `null` for non-input elements rather than silently returning an empty string) and requires no JS string.

```csharp
// ❌ Current — JS eval, fragile for non-input elements
values.Add(await element.EvaluateAsync<string>("el => el.value"));
```

```csharp
// ✅ Recommended — proper Playwright API
values.Add(await element.InputValueAsync());
```

---

### 4. `WaitHelper.WaitForElementToDisappearAsync` — `CountAsync()` polling loop

**File:** `PlaywrightBase/Helpers/WaitHelper.cs`

**Problem:**  
A manual polling loop calls `CountAsync()` with `Task.Delay(retryDelay)` between attempts just to check if an element exists before waiting for it to disappear. `WaitForAsync(State = Detached)` already handles the case where the element is not present — it resolves immediately.

```csharp
// ❌ Current — manual polling to check existence before the real wait
for (int attempt = 0; attempt < initialRetries; attempt++)
{
    if (await element.CountAsync() > 0) break;
    if (attempt < initialRetries - 1) await Task.Delay(retryDelay);
}
if (await element.CountAsync() == 0) { /* already gone */ return true; }
await element.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Detached, Timeout = timeout });
```

```csharp
// ✅ Recommended — WaitForAsync(Detached) handles "already gone" natively
await element.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Detached, Timeout = timeout });
// If element was already detached, this resolves immediately — no polling needed
```

---

### 5. `HQXConsumerElementInteractionHelper.SelectDropdown` — Unnecessary `Task.Delay` calls

**File:** `Projects/HQXConsumer/Helpers/HQXConsumerElementInteractionHelper.cs`

**Problem:**  
Two `Task.Delay` calls are used as stability buffers around a dropdown interaction where explicit `WaitForAsync` calls already handle the state transitions.

```csharp
// ❌ Current — Task.Delay before WaitForAsync is redundant
await dropDown.ClickAsync(new() { Timeout = timeout });
await Task.Delay(200);  // unnecessary — WaitForAsync(Visible) below covers this
var dropdownPanel = dropDown.Page.Locator(".ng-dropdown-panel");
await dropdownPanel.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = timeout });
await Task.Delay(100);  // unnecessary — Filter().First.ClickAsync() retries until ready
var optionLocator = dropdownPanel.Locator(".ng-option").Filter(new LocatorFilterOptions { HasText = selectValue });
```

```csharp
// ✅ Recommended — let Playwright's retry engine handle stability
await dropDown.ClickAsync(new() { Timeout = timeout });
var dropdownPanel = dropDown.Page.Locator(".ng-dropdown-panel");
await dropdownPanel.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = timeout });
var optionLocator = dropdownPanel.Locator(".ng-option").Filter(new LocatorFilterOptions { HasText = selectValue });
```

---

## ✨ New Feature Opportunities (Playwright 1.54–1.58)

### 1. Composable `locator.Filter()` — Dropdown & table row selection (1.56)

**Relevant files:** `HQXConsumerElementInteractionHelper.cs`, `TableHelper.cs`

Playwright 1.56 allows `Has`, `HasNot`, and `HasText` to be combined in a single `.Filter()` call. This is directly applicable to dropdown option filtering and table row matching.

**Current pattern in HQXConsumer dropdown:**
```csharp
var optionLocator = dropdownPanel.Locator(".ng-option")
    .Filter(new LocatorFilterOptions { HasText = selectValue });
```

**Improved — combine text AND active state check:**
```csharp
var optionLocator = dropdownPanel.Locator(".ng-option")
    .Filter(new LocatorFilterOptions
    {
        HasText = selectValue,
        HasNot = dropdownPanel.Locator(".ng-option-disabled") // skip disabled options
    });
```

**Table row matching in `TableHelper.FindRowByColumnValueAsync`** (currently uses XPath + loops):
```csharp
// ✅ Replace XPath loop with composable filter
var targetRow = _page.Locator("datatable-body-row")
    .Filter(new LocatorFilterOptions
    {
        Has = _page.Locator($".grid-prop-{columnName}"),
        HasText = columnValue
    });
```

---

### 2. `toHaveAccessibleErrorMessage()` — New method on `PageValidationHelper` (1.54)

**File:** `PlaywrightBase/Helpers/PageValidationHelper.cs`

`PageValidationHelper.GetValidationMessagesAsync()` already collects `[aria-invalid='true']` elements via CSS selectors. Playwright 1.54 adds a first-class assertion `toHaveAccessibleErrorMessage()` that reads the element's `aria-errormessage` attribute target — more reliable than CSS selector scraping.

**Proposed new method to add to `PageValidationHelper`:**
```csharp
/// <summary>
/// Asserts that a specific field has an ARIA-linked error message visible on the page.
/// Uses Playwright's accessibility tree — works even when the error element has no visible CSS class.
/// </summary>
public async Task AssertFieldHasAccessibleErrorAsync(string fieldSelector, string? expectedMessage = null)
{
    var field = page.Locator(fieldSelector);
    await field.WaitForAsync(new() { State = WaitForSelectorState.Visible });

    if (expectedMessage is not null)
        await Assertions.Expect(field).ToHaveAccessibleErrorMessageAsync(expectedMessage);
    else
        await Assertions.Expect(field).ToHaveAccessibleErrorMessageAsync(new Regex(".+"));
}
```

**Where to use it:** Form validation tests on HQXConsumer/HQXAgent pages where `[aria-invalid="true"]` fields exist.

---

### 3. `locator.ContentFrame` — Iframe helper method (1.55)

**Relevant files:** `PageHelper.cs` / `NavigationHelper.cs`

No iframe-specific abstraction exists in the current helpers. If any page objects interact with embedded content (e.g., payment iframes, embedded quote widgets), the 1.55 `ContentFrame` property eliminates the need for a separate `page.FrameLocator()` call.

**Proposed addition to `PageHelper`:**
```csharp
/// <summary>
/// Gets the FrameLocator for an iframe element using the cleaner 1.55 ContentFrame API.
/// Use instead of page.FrameLocator(selector) when you already have a locator.
/// </summary>
public IFrameLocator GetIframeContent(string iframeSelector)
{
    var iframeLocator = _page.Locator(iframeSelector);
    return iframeLocator.ContentFrame;
}
```

**Usage in page objects:**
```csharp
// Before (1.53)
var frame = _pageHelper.Page.FrameLocator("iframe#payment-frame");
await frame.Locator("input#card-number").FillAsync(cardNumber);

// After (1.55)
var frame = _pageHelper.GetIframeContent("iframe#payment-frame");
await frame.Locator("input#card-number").FillAsync(cardNumber);
```

---

### 4. `browserContext.Clock` — Session timeout & token expiry tests (1.57)

**Relevant files:** `WaitHelper.cs`, test classes that test session-based flows

There is currently no mechanism to test time-dependent behavior (session expirations, JWT token expiry, policy effective dates) without real `Task.Delay` waits. Playwright 1.57 ships `browserContext.Clock` that freezes and controls browser time without real waiting.

**Proposed helper addition (new `ClockHelper` or extension on `BrowserManager`):**
```csharp
/// <summary>
/// Fast-forwards browser clock without real time passing.
/// Use to test session timeouts, token expiry, or date-dependent UI behavior.
/// </summary>
public static async Task FastForwardTimeAsync(IBrowserContext context, TimeSpan duration)
{
    await context.Clock.FastForwardAsync((long)duration.TotalMilliseconds);
}

/// <summary>
/// Freezes browser time at a specific date — useful for testing date pickers,
/// policy effective dates, or any UI that renders based on "today".
/// </summary>
public static async Task SetFixedTimeAsync(IBrowserContext context, DateTimeOffset fixedTime)
{
    await context.Clock.SetFixedTimeAsync(fixedTime.ToUnixTimeMilliseconds());
}
```

**Where to use it:**
- HQXConsumer/HQXAgent flows that show "session about to expire" banners
- Policy start date pickers that default to today
- Any test currently using `Task.Delay(TimeSpan.FromMinutes(...))` as a workaround

---

### 5. `page.RouteWebSocketAsync` — WebSocket/SignalR mocking (1.57)

**Relevant files:** none yet — this is a new capability

If any consumer/agent flows use real-time updates (SignalR, raw WebSocket for quote status, notifications, or live pricing), tests currently either depend on real backend events or skip those assertions. Playwright 1.57 adds full WebSocket interception.

**Proposed helper (new `NetworkInterceptionHelper` or extension on `PageHelper`):**
```csharp
/// <summary>
/// Mocks a WebSocket connection to return controlled responses.
/// Useful for testing quote status updates, notifications, or live pricing feeds
/// without depending on real backend WebSocket events.
/// </summary>
public static async Task MockWebSocketAsync(
    IPage page,
    string wsUrlPattern,
    Func<string, string?> messageHandler,
    IAutomationLogger? logger = null)
{
    await page.RouteWebSocketAsync(wsUrlPattern, ws =>
    {
        ws.OnMessage(message =>
        {
            logger?.Debug($"WS intercepted: {message}");
            var response = messageHandler(message);
            if (response is not null)
                ws.Send(response);
        });
    });

    logger?.Info($"WebSocket mock registered for pattern: {wsUrlPattern}");
}
```

**Where to use it:**
- Quote progress updates sent via WebSocket/SignalR
- Real-time notifications in the agent/consumer portals
- Any test that currently waits for a backend event to propagate to the UI

---

## Prioritised Action List

| Priority | Item | File | Effort |
|---|---|---|---|
| 🔴 High | Remove redundant `CountAsync()` after `WaitForAsync(Attached)` | `ElementInteractionHelper.cs` | Low |
| 🔴 High | Replace `EvaluateAsync("el => el.value")` with `InputValueAsync()` | `WaitHelper.cs` | Low |
| 🔴 High | Collapse `WaitForElementAsync` retry loop into single `WaitForAsync` | `WaitHelper.cs` | Low |
| 🟡 Medium | Remove `Task.Delay` stability buffers in `SelectDropdown` | `HQXConsumerElementInteractionHelper.cs` | Low |
| 🟡 Medium | Replace `CountAsync()` polling in `WaitForElementToDisappearAsync` | `WaitHelper.cs` | Low |
| 🟡 Medium | Add composable `Filter()` to `TableHelper.FindRowByColumnValueAsync` | `TableHelper.cs` | Medium |
| 🟢 New | Add `AssertFieldHasAccessibleErrorAsync` to `PageValidationHelper` | `PageValidationHelper.cs` | Low |
| 🟢 New | Add `GetIframeContent()` using `ContentFrame` to `PageHelper` | `PageHelper.cs` | Low |
| 🟢 New | Add `ClockHelper` / fast-forward time for session/date tests | New file | Medium |
| 🟢 New | Add `MockWebSocketAsync` for real-time flow testing | New file | Medium |
