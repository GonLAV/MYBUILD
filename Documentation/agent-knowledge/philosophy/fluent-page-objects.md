---
topic: philosophy:fluent-page-objects
summary: Page objects expose intent (FillField, ClickContinue), not Playwright primitives — tests stay readable.
status: ready
---

# Fluent page objects

A page object's public surface is the vocabulary the test uses to describe behavior. If the surface says `FillForm(formData)`, `ClickContinue()`, `IsRatesPresent()`, the test reads as English: "fill the start page, continue, check we got rates." If the surface exposes `Page.Locator(".x").ClickAsync()`, the test reads as Playwright: "click the element matching this selector and hope it's the right one."

The framework's stance: page objects expose intent. Tests express scenarios in that vocabulary. Playwright never appears at the test layer.

## The contract

`InterviewBase` ([../framework/page-objects.md](../framework/page-objects.md)) gives every Interview page:

- `FillForm(Dictionary<string,string>?)` — apply the data dictionary to whatever fields the page exposes.
- `ClickContinue()` — proceed to the next page.
- `ValidatePageReady()` — confirm we're on the page the test thinks we are.

The implementation underneath uses Playwright extensively — but it's encapsulated. A test calling `await startPage.FillForm(data)` doesn't know whether the page filled three fields or thirty; whether each field is an `<input>` or an `<ng-select>`; whether the registry used XPath or CSS. The test cares about *the business outcome of filling the page*, not the per-locator mechanics.

Override patterns ([../recipes/override-patterns.md](../recipes/override-patterns.md)) extend the fluent surface where needed — `FillVinAndDecodeAsync`, `SelectViaArrowAsync`. Each override is named for what the user does. The test calls them by name; it doesn't reach inside.

## Why this matters

A page-object layer that exposes Playwright primitives breaks down within months:

1. **The XPath ban becomes unenforceable.** If tests call `page.Locator("//x")`, every locator on every page is a test-layer concern. Reviewers can't keep up. Eventually the ban becomes a suggestion.
2. **The DOM becomes a test contract.** When a tenant changes class names, every test that hardcoded a locator breaks — but the breakage looks like a test failure, not a tenant change. Diagnosis takes hours instead of minutes.
3. **Tests can't be read by non-engineers.** A test that says `FillForm(homeDataDefaults)` is parseable by a QA engineer or product manager. A test that says `Page.Locator("[name='PolicyData.SlabType'] label:has-text('Concrete')").ClickAsync()` is opaque.

The cost of fluent page objects is that page-object code is denser than the test code that uses it — the page absorbs the mechanics that the test would otherwise carry. That's the right trade. A page object is written once per page; it's used by every test on that page. The framework pays the cost of fluency once; the savings accrue every test, forever.

## What "intent" means

A method on a page object should answer the question *what does the user (or framework) do here?* — not *how does Playwright do it?*

Good names:
- `FillForm` — fill all the fields on this page from a data dictionary.
- `ClickContinue` — advance to the next page.
- `SelectLob(string lob)` — choose a line of business.
- `IsAccountMatchPopUpExists()` — check whether the soft-/hard-match popup appeared.
- `FillVinAndDecodeAsync(string vin)` — type a VIN and wait for the decode signal.

Bad names (these leak the DOM into the test):
- `ClickFirstButton()`
- `FillNgSelectAtIndex(int i, string value)`
- `LocateByXPath(string xpath)` — the framework should never let this exist.
- `GetElement(string selector)` — same.

When you find yourself wanting to add a primitive-shaped helper to a page object, ask: *what business action am I trying to describe?* Name the method that, then implement the primitive inside.

## The connection to sparse dictionaries

A page object that exposes `FillForm(formData)` can consume the sparse dictionary contract: the test provides only the keys that diverge from default; the page (via `MergeDataManager`) fills the rest. If `FillForm` didn't exist, every test would have to specify every value, defeating the sparse-dict principle.

Fluency at the page-object layer is what makes sparseness at the test layer possible.

## Anti-patterns

- **Tests using `page.Locator(...)` directly.** The page object should expose a named method.
- **Page-object helpers named for the DOM mechanism, not the user action.** `ClickNgSelectArrow` should probably be `SelectViaArrow` inside a `SelectFooDropdown` method — wrap the arrow-click in business intent.
- **Returning the IPage handle from a page-object method.** That lets the caller dig in. Don't.
- **Adding a method to a page object that's only used by one test.** If the action is genuinely one-off, inline it in the test (rare). More commonly: the method *will* be used elsewhere — name it for the business action and add it.

## Cross-references

- [../framework/page-objects.md](../framework/page-objects.md) — the mechanism (InterviewBase, ADBX_BasePage, override patterns).
- [../recipes/override-patterns.md](../recipes/override-patterns.md) — A–G patterns for extending fluency where the default isn't enough.
- [sparse-dictionaries.md](sparse-dictionaries.md) — the data-side analogue.
- [tests-stay-clean.md](tests-stay-clean.md) — the test-side consequence.
