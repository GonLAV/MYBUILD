---
topic: partner:pgr-quote-status-polling
summary: PGR CheckQuoteStatus — QuoteStatusWithPollingAsync polls until the expected pair and throws otherwise, so asserting that pair afterwards can never fail; read until settled instead.
status: ready
---

# Reading a PGR quote status without writing a tautology

`IPlatformQuoteStatusApi.QuoteStatusWithPollingAsync(request, expectedStatus, expectedSecondaryStatus)` delegates to `RetryHelper.RetryUntilAsync`, which loops until its predicate matches and **throws** `ApiResponseException` when the timeout elapses. It never returns a non-matching response.

So the common shape below **cannot fail its assertion**. The only failure mode is an `ApiResponseException` after the 100s default — which now names the expected pair and the last status seen, but still costs the full 100s wait:

```csharp
var s = (await api.QuoteStatusWithPollingAsync(req, QuoteStatus.Incomplete, QuoteSecondaryStatus.ManufacturedHome)).Content!;
Assert.That(s.QuoteStatus, Is.EqualTo(nameof(QuoteStatus.Incomplete)));      // always true
Assert.That(s.SecondaryStatus, Is.EqualTo(nameof(QuoteSecondaryStatus.ManufacturedHome)));  // always true
```

**When the status pair is the thing under test, read until *settled* and assert what it settled to.** Use `QuoteStatusUntilAsync`, which wraps the **non-throwing** `RetryHelper.RetryAsync` and returns the last status it saw:

```csharp
var status = await platformApi.QuoteStatusUntilAsync(
    request,
    s => !string.Equals(s.SecondaryStatus, nameof(QuoteSecondaryStatus.None), StringComparison.OrdinalIgnoreCase));

Assert.That(status.SecondaryStatus, Is.EqualTo(nameof(QuoteSecondaryStatus.UUD)),
    $"SecondaryStatus was not UUD. Actual: {status.SecondaryStatus}");
```

Reach for `RetryHelper.RetryAsync` directly when the read is not a `QuoteStatus` call. **Do not use `RetryUntilAsync` here** — it throws on timeout, so the failure arrives as an exception rather than a named assertion, and only after the whole budget elapses.

Either shape *fails* when the status is wrong; the difference is diagnosis. Polling for the expected pair burns the full 100s before it reports anything; reading until settled fails in seconds with `SecondaryStatus was DNQ, expected UUD`.

Two findings this shape produced on QA that the polling shape had hidden for weeks: the Florida manufactured-home kickout reports `NoAppetite / NoAppetite` rather than the recorded `Incomplete / None`, and the exotic-pets test — which polled for `DNQ` + `UUD` against `DNQ / DNQ` behaviour — had **never once reported its own finding**.

## Neighbouring traps

- **`MsgStatusCd == "Success"` is not a status assertion.** It is the ACORD envelope's transport status; it says nothing about `QuoteStatus`/`SecondaryStatus`.
- **`WaitForNavigationOrUrlContainsAsync` returns a `bool` that is never `false`** — it throws `NavigationException` on timeout. So asserting that bool is a *dead* assertion, not a safe one. Assert the landing URL instead. See [../../framework/navigation-waits.md](../../framework/navigation-waits.md).
- **`IPollyRetryService.ExecuteWithRetryAsync` is unfit for long waits.** At the default `timeOutSeconds = 70` the policy computes 5 retries with 62s of exponential backoff, leaving 8s split across 6 attempts — **~1s per attempt**. Use `RetryHelper.RetryUntilAsync` or a plain loop.

## Cross-references

- [../../philosophy/tests-stay-clean.md](../../philosophy/tests-stay-clean.md) — assertions belong in the test body; helpers gather.
- [progressivepl.md](progressivepl.md) — the PGR tenant leaf.
