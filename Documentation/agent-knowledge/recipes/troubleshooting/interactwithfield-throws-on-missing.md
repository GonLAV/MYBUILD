---
topic: troubleshooting:interactwithfield-throws-on-missing
summary: A page callback dies on a field the main FillForm pass skipped happily — InteractWithField forces IgnoreIfNotFound = false.
status: ready
---

# Callback throws on a field FillForm was happy to skip

**Symptom:** the run log shows the main fill pass tolerating a field:

```
Processing field: Occupation = Other
Element not found but ignoring due to IgnoreIfNotFound option
```

…and then a `pageCallbacks` callback dies on that *same* field:

```
System.Exception : Timed out trying to select element — it was not ready within 3000ms.
Locator: XPath = '//*[contains(@class, 'PolicyData.OccupationStr')]//ng-select'
```

**Cause:** both `FieldInteractionHelper.InteractWithField` overloads hard-set
`IgnoreIfNotFound = false` on the options they build — the string-value one and the
`ElementInteractionOptions` one alike. Your own options object cannot override it; the field is
reassigned after you pass it. `ElementInteractionHelper` honours the flag (`options?.IgnoreIfNotFound
!= true` guards the rethrow), so an explicit `InteractWithField` **always** throws on a missing
element, while `FillForm`'s path leaves the flag at its default `true` and logs-and-continues.

So the asymmetry is by design: `FillForm` is best-effort, `InteractWithField` is an assertion that the
field is there.

**Fix — gate the call when the field is genuinely optional:**

```csharp
if (await _pageHelper!.ElementExists(FieldNames.Occupation, timeout: 15000))
    await _pageHelper.InteractWithField(FieldNames.Occupation, "Other");
```

`IPageHelper.ElementExists(string fieldName, int timeout = 1000, string? expectedPlaceholder = null)`
resolves through the registry, so no locator is duplicated into the test.

**Do not "fix" this by raising the timeout.** A longer wait only helps a field that is *late*; a field
that is *conditional* never arrives, and a 20s timeout just makes the failure slower. Establish which
one you have first — see
[conditional-field-not-rendered.md](conditional-field-not-rendered.md) and
[reading-captured-page-source.md](reading-captured-page-source.md).

**Cross-references:** [field-not-found.md](field-not-found.md),
[../ui-only-entry-point-coverage.md](../ui-only-entry-point-coverage.md).
