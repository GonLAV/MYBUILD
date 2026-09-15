---
topic: troubleshooting:index
summary: Troubleshooting recipe book — symptom → likely cause → fix; jump to matching section after page_source review.
status: ready
---

# Troubleshooting — first-run failure recipes

> **When to read:** Phase 4, every iteration. This is a recipe book — diagnose, then jump to the matching section.

## How to read this file

Each leaf in this directory is **symptom → likely cause → fix**. When iterating on a failing test, find the symptom that matches the failure, open that file, apply the fix, re-run.

The framework's primary diagnostic artifact is the captured DOM snapshot at:

```
Bolt.Automation.Tests/bin/Debug/net10.0/TestResults/<test-name>/page_source_*.html
```

Always open that file before changing code — most fixes start with grepping for the field's class name in the actual rendered HTML.

## Symptom index

| # | Symptom | Recipe |
|---|---|---|
| 1 | "Could not find UIElement for field X" | [field-not-found.md](field-not-found.md) |
| 2 | Locator wrong — page validation passes but field doesn't fill | [locator-mismatch.md](locator-mismatch.md) |
| 3 | Default re-injected — field has wrong value or fights the page | [default-reinjected.md](default-reinjected.md) |
| 4 | Strict-mode multi-match (locator resolves to N elements) | [strict-mode-multimatch.md](strict-mode-multimatch.md) |
| 5 | Conditional reveal — field appears mid-fill | [conditional-reveal.md](conditional-reveal.md) |
| 6 | Continue button — no selector matches | [continue-button-no-match.md](continue-button-no-match.md) |
| 7 | Page validation timing out | [page-validation-timeout.md](page-validation-timeout.md) |
| 8 | Strict popup interception (overlay element intercepts click) | [popup-interception.md](popup-interception.md) |
| 9 | Tab/focus race after typing into ng-select typeahead | [typeahead-focus-race.md](typeahead-focus-race.md) |
| 10 | Click on disabled button does nothing | [disabled-button-click.md](disabled-button-click.md) |
| 11 | Tab switch ordering after ADBX popup | [tab-switch-ordering.md](tab-switch-ordering.md) |
| 12 | Field validation error blocks Continue | [field-validation-blocks-continue.md](field-validation-blocks-continue.md) |
| 13 | VIN decode signal never arrives (environmental flake) | [vin-decode-flake.md](vin-decode-flake.md) |
| 14 | `_logger?.Warn` doesn't exist | [logger-method-naming.md](logger-method-naming.md) |
| 15 | Test method body has logging but report is empty | [test-body-logging-missing-steps.md](test-body-logging-missing-steps.md) |
| 16 | Two unrelated fixes → break-fix-break loop | [break-fix-break-loop.md](break-fix-break-loop.md) |
| 17 | Framework rejects div locator on a Dropdown field | [div-not-supported-dropdown.md](div-not-supported-dropdown.md) |
| 18 | Dropdown opens but option text not found | [dropdown-option-missing.md](dropdown-option-missing.md) |
| 19 | Floated label intercepts pointer events on ng-select | [floated-label-interception.md](floated-label-interception.md) |
| 20 | FillAsync rejected on mat-datepicker hidden input | [mat-datepicker-fillasync.md](mat-datepicker-fillasync.md) |
| 21 | Conditional field expected but container not present | [conditional-field-not-rendered.md](conditional-field-not-rendered.md) |
| 22 | Question-set validation has "extra" sub-fields; expected list undercounts | [reveal-on-answer-questionset.md](reveal-on-answer-questionset.md) |
| 23 | Every test skips, even ones with no env gate | [all-tests-skip-vpn.md](all-tests-skip-vpn.md) |
| 24 | Page callback dies on a field the main FillForm pass skipped | [interactwithfield-throws-on-missing.md](interactwithfield-throws-on-missing.md) |
| 25 | Page-source signals: which ones to trust when reading a captured DOM | [reading-captured-page-source.md](reading-captured-page-source.md) |
| 26 | Switcher clicked and reported OK, but the answer is not recorded | [switcher-answer-not-recorded.md](switcher-answer-not-recorded.md) |

## When you don't know what's wrong

Default order of investigation:

1. Build error → fix syntax/type/missing-using.
2. Compile passes, runtime exception → read the exception type:
   - `TestSetupException` → user/URL config missing.
   - `PageElementException` → locator or click target.
   - `PopupTimeoutException` → popup didn't appear or didn't dismiss.
   - `NavigationException` → URL or tab issue.
3. Read the DOM snapshot before changing code.
4. Check the relevant [../../domain/partners/](../../domain/partners/) page for tenant-specific behavior matching the symptom.
5. If still stuck after 2 iterations, stop and re-read the manifest from Phase 1 — you may be implementing the wrong page.

## Red flags that mean "stop iterating, re-think"

- Five iterations on the same page without progress. The page model is probably wrong; consider whether `PageIdentifier` should be different, or whether you need a different page object.
- Adding `IgnoreIfNotFound = false` to "force" a locator. Almost always means the locator is wrong, not the option.
- Adding `await Task.Delay(5000)` to "give it time." Find the actual signal (a class change, an attribute, a network response) and wait for that.
- Catching exceptions to make a test green. The test is then lying. Either the exception is real (fix it) or the assertion is wrong (fix it).
- Repeating registry `DefaultValue` values in the test's `formData` dict. Every redundant entry is noise that hides the values that actually matter for the scenario. `MergeDataManager.GetSmartFormData` already fills defaults for missing keys.
