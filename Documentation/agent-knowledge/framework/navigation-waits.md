---
topic: framework:navigation-waits
summary: WaitForNavigationOrUrlContainsAsync returns bool but throws on timeout — never returns false; assert the URL, not the bool. When the destination does not resolve (chrome-error), read the target off the navigation request.
status: ready
---

# Navigation waits — the bool that is never false

`IPageHelper.WaitForNavigationOrUrlContainsAsync(urlPart, timeout)` is declared `Task<bool>`, but on timeout it **throws `NavigationException`** ([WaitHelper.cs](../../../Bolt.Automation.FrontEnds/PlaywrightBase/Helpers/WaitHelper.cs), the `catch (TimeoutException)` branch). It only ever returns `true`.

## Why it matters

This shape is a **dead assertion** — it can never fail, and the message never prints:

```csharp
var redirected = await _pageHelper.WaitForNavigationOrUrlContainsAsync("progressive.com", 15000);
Assert.That(redirected, Is.True, "an MPQ3 quote must redirect, never render a BOLT page");  // unreachable
```

What actually happens on a missing redirect is an opaque `NavigationException` at the wait, killing the test before its own assertions run. Tests that check a redirect *as their business outcome* lose both the message and every finding that came after it.

## What to do instead

**If the navigation is a precondition** — the test asserts something else once it arrives — call it and let it throw. That is the correct failure.

**If the navigation is the thing under test**, catch and report. Either return the landing URL and assert on that:

```csharp
try { await pageHelper.WaitForNavigationOrUrlContainsAsync(domain, timeoutMs); }
catch (NavigationException ex) { logger.Info($"No hand-back to '{domain}': {ex.Message}"); }
var landingUrl = browserManager.GetCurrentTab()?.Url ?? "no active page";
// assert: Assert.That(landingUrl, Does.Contain(domain), $"... Left on: {landingUrl}")
```

…or catch it into a real bool, as `HQXConsumerBase.ClickExitToAutoQuoteAsync` does.

Asserting the URL beats asserting a bool either way: the failure message can name the page the user was actually left on.

## When the destination does not resolve

Some flows hand an external system a redirect target that nothing hosts — a payment provider's `successUrl`, an SSO hand-back stub. DNS fails, Chromium commits an error page, and `page.Url` becomes `chrome-error://chromewebdata/`. The target and its query string are **gone from the address bar**, so every landing-URL technique above returns nothing usable, and `WaitForNavigationOrUrlContainsAsync` waits out its full timeout for a URL the browser will never show.

`WaitHelper` tries to recover the intended URL from Chrome's navigation history over CDP, but it does that **once, on entry** — if the error page has not committed by that instant, the call falls through to `WaitForURLAsync` and times out anyway. Do not rely on it for a redirect that is the business outcome.

Read the target off the navigation **request** instead. Chromium issues it before it resolves the host, so DNS failure never touches it:

```csharp
var request = await Page.RunAndWaitForRequestAsync(
    async () => await Page.Locator(SubmitButtonLocator).ClickAsync(),
    r => r.IsNavigationRequest && r.Url.Contains(host, StringComparison.OrdinalIgnoreCase),
    new PageRunAndWaitForRequestOptions { Timeout = timeout });
return request.Url;   // full target, query string intact - assert on this in the test body
```

A timeout here means the redirect was never requested at all — worth wrapping in a `NavigationException` that says so, since Playwright's own message only says an event did not fire. `Product_PaymentIntegrationPage.ClickNextAndGetRedirectUrlAsync` (TC #123432) is the worked example.

## Related

A page object used as a flow terminal validates by URL substring (`PageIdentifier` → `ValidateUrlAsync`) and **throws** if it never arrives, because `PageFlowHelper.CreateAndValidatePage` always constructs with `validatePageReady: true`. So making a redirect a flow terminal turns it into a precondition — the same trade-off as above. See [flows-executor.md](flows-executor.md).

## Cross-references

- [flows-executor.md](flows-executor.md) — flow terminals and page validation.
- [../recipes/test-review-rubric.md](../recipes/test-review-rubric.md) — dimension 4 (assertions must report the actual value).
