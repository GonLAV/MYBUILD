---
topic: framework:popups
summary: PopupBase + PopupFactory, dismiss/answer patterns, integration with PageHelper.
status: ready
---

# Popups

> **When to read:** Phase 2 (predicting popups based on the entry path), Phase 3 (wiring popup detection + resolution into the test).

## Convention overview

Popups are page objects. The framework's idioms:

1. **Detection on the parent page.** The page that hosts the popup exposes an `IsXxxPopUpExists()` method that does a non-blocking `ElementExists` check.
2. **Construction via factory.** The popup itself is created with `PageFactory.CreatePage<TPopup>()` once detection succeeds; the popup's constructor validates its own readiness.
3. **Resolution via popup methods.** The popup exposes named action methods (`ClosePopup`, `ResolveAsync`, `SelectExistingAccountAsync`, etc.) that encapsulate the click sequence.

The test orchestrates: `if (parent.IsXxxPopUpExists()) { var popup = PageFactory.CreatePage<TPopup>(); await popup.ResolveAsync(...); }`.

## Detection — how `IsXxxPopUpExists` works

`Bolt.Automation.FrontEnds/Projects/ADBX/Base/ADBX_BasePage.cs` defines:

```csharp
public async Task<bool> IsUpdateAccountInformationPopUpExists() =>
    await PageHelper.ElementExists(LocatorType.XPath, CommonLocators.AccountPopup);

public async Task<bool> IsAccountMatchPopUpExists() =>
    await PageHelper.ElementExists(LocatorType.XPath, CommonLocators.AccountMatchPopup);
```

Each new popup gets:
- A `CommonLocators` constant for its root element (typically `//div[contains(@class,'modal-X')]` or a heading match).
- An `IsXxxPopUpExists()` method on the parent page (or `ADBX_BasePage` if it can appear on multiple pages).

`ElementExists` is a non-throwing existence check — short timeout, returns `bool`. Don't confuse with `WaitForAsync` which throws on miss.

## ADBX_AccountAlreadyExistsPopup — the worked example

`Bolt.Automation.FrontEnds/Projects/ADBX/Popups/ADBX_AccountAlreadyExistsPopup.cs`. This popup appears after clicking ADD on the New Quote popup when an existing account matches the entered data. Two variants:

```csharp
public enum AccountMatchType { None, SoftMatch, HardMatch, Unknown }
public enum AccountMatchSoftAction { SelectExisting, CreateNew }
```

- **HardMatch** — subtitle "A new account cannot be created with the same information." User must pick an existing account and continue.
- **SoftMatch** — subtitle "To proceed, select one of the options below." User can either pick an existing account or create a new one (`#new` / `#existing` radios).

### Public API

```csharp
public Task<bool> IsPopupVisibleAsync();        // any variant visible
public Task<AccountMatchType> GetMatchTypeAsync(); // None / SoftMatch / HardMatch / Unknown
public Task SelectCreateNewAccountAsync();      // soft match only
public Task SelectExistingAccountAsync(int optionIndex = 0); // pick a row from the list
public Task ClickContinueAsync();               // shared Continue button

public Task<AccountMatchType> ResolveAsync(
    AccountMatchSoftAction softMatchAction = AccountMatchSoftAction.SelectExisting,
    int existingOptionIndex = 0);
```

`ResolveAsync` is the test-friendly entry point — handles all three branches:

- `HardMatch` → `SelectExistingAccountAsync(index)` → `ClickContinueAsync()`.
- `SoftMatch` + `SelectExisting` → click "existing" radio → select option at index → `ClickContinueAsync()`.
- `SoftMatch` + `CreateNew` → click "new" radio → `ClickContinueAsync()`.
- `None` / `Unknown` → no-op.

### Usage from a test

```csharp
if (await accountSummary.IsAccountMatchPopUpExists()) {
    var matchPopup = PageFactory.CreatePage<ADBX_AccountAlreadyExistsPopup>();
    var matchType = await matchPopup.ResolveAsync(
        softMatchAction: AccountMatchSoftAction.SelectExisting);
    _logger.Info($"Account-match popup resolved as {matchType}");
}

await BrowserManager.SwitchToLastTabAsync();   // popup ADD opens a new tab
```

If you only care that the popup is gone (not which variant), you can ignore the return value. If the test depends on specific behavior per variant, branch on `matchType`.

## ADBX_EnterUpdateAccountInformationPopup

Shown when an existing account needs additional fields filled before the user can continue. Exposes:

```csharp
public Task ClosePopup();           // dismiss without filling — close-X click
public Task FillForm(Dictionary<string,string>? data);  // fills the popup body
```

Detected via `parentPage.IsUpdateAccountInformationPopUpExists()`.

Typical pattern:

```csharp
var page = PageFactory.CreatePage<ADBX_AccountSummaryPage>();
if (await page.IsUpdateAccountInformationPopUpExists()) {
    var popup = PageFactory.CreatePage<ADBX_EnterUpdateAccountInformationPopup>();
    await popup.ClosePopup();
}
```

## ADBX_NewQuotePopup (or analogous)

The New Quote popup is a form-driven popup that follows the registry pattern: its fields are tagged in `FieldRegistryADBX` with `Pages = [typeof(ADBX_NewQuotePopup)]`. So filling it is just `popup.FillForm(...)`:

```csharp
var popup = await accountSummary.ClickOnNewQuote();
await popup.FillForm(new Dictionary<string, string> {
    [ADBX_FieldNames.AccountBusinessLine] = "Commercial",
    [ADBX_FieldNames.AccountFirstName] = "Auto",
    [ADBX_FieldNames.AccountLastName] = "Test",
    [ADBX_FieldNames.AccountEmail] = randomEmail,
    [ADBX_FieldNames.AccountPhone] = "5555552020",
    [ADBX_FieldNames.AccountBusinessName] = "Test Automation",
});
await popup.ClickPopupAdd();   // popup-specific action button
```

Key takeaways:
- Popup field names live in `ADBX_FieldNames` (not `Common.FieldNames`) — the popup is an ADBX construct.
- The action button (`Add`, `Save`, `Submit`) is a popup method, not the standard `ClickContinue`.

## ADBX_AddPolicyInformationPopUp — a term you set is not a term you get

Two of this popup's registry defaults contradict each other: `PolicyTerm` is `"6 months"` and
`PolicyExpirationDate` is `DateTime.Today.AddYears(1)`. `FillForm` works in registry order, and
`PolicyExpirationDate` sits *after* `PolicyTerm`, so the fill selects the 6-month term and then
overwrites the expiration date the app derived from it. **The policy saves as 12 months.**

So a caller that wants a term other than 12 months has to re-apply it after the fill, which lets the app
derive the date again:

```csharp
await addPolicyPopup.FillForm(policyFormData);
await pageHelper.InteractWithField(ADBX_FieldNames.PolicyTerm, policyFormData.GetValueOrDefault(ADBX_FieldNames.PolicyTerm));
await addPolicyPopup.ClickPopupConfirm();
```

Re-apply it unconditionally, not only when the caller passed a term. `FillForm` fills every registry
field on the page, so it applies `PolicyTerm`'s own default (`"6 months"`) when the caller omits the
key — and `PolicyExpirationDate` overwrites the derived date just the same. Guarding the re-apply on
the caller's dictionary leaves exactly those callers with the year-out date they never asked for.
Passing `null` re-applies the registry default, which is what the fill used.

Three call sites independently needed this (`PolicyTests.Renewals`, and both CL binder tests, where it
now lives once in `AdbxTestHelper.RecordSoldNoteAsync`). Making the two defaults agree
would be the real fix, but `PolicyTests` has a case that *asserts* the year-out default, so the registry
and that test have to change together.

The failure is quiet: a test that only checks the binder exists passes while writing the wrong term.
Assert the dates if the term matters.

Also note the Product select is a native `<select>` matched on **visible label**, not option value —
pass `LobType.BusinessOwners.ToToken()` ("Business Owners Policy"), not `"BOP"`, or you get a bare
3000ms timeout.

## Adding a new popup

1. **Locator constant**. Add the popup's root locator to `CommonLocators` (or a popup-specific constants class):
   ```csharp
   public const string MyNewPopup =
       "//div[contains(@class,'modal-content')]//h5[contains(text(),'My New Popup Title')]";
   ```
2. **Detection method**. Add `IsMyNewPopupExists()` on the page where the popup can appear (or on `ADBX_BasePage` if cross-page).
3. **Popup class**. Inherit from `ADBX_BasePage` (or `InterviewBase` for Interview-side popups). Override `PageIdentifier` to a substring unique to the popup's root markup, and `PageName` for logs.
4. **Methods**. Expose action methods that name what the user does (`ConfirmAsync`, `CancelAsync`, `SelectOptionAsync(int)`, etc.). Avoid generic `ClickButton(name)` wrappers — they leak the locator detail to the test.
5. **Resolution helper** if the popup has multiple variants (like AccountAlreadyExists). One method that picks the variant and does the right thing.
6. **Tests**: detect first, instantiate via `PageFactory`, call action method, log the outcome.

## Anti-patterns

- **Catching `ElementNotFoundException` to detect a popup.** Use `IsXxxPopUpExists` — a try/catch obscures intent and is slower.
- **Instantiating the popup before detection succeeds.** `PageFactory.CreatePage<TPopup>()` validates readiness in the constructor; if the popup isn't there, you get a noisy `PageElementException` rather than a clean `false`.
- **Reusing `ClickContinueButton` for popup confirmations.** Popups have their own action buttons; don't pollute the base selector list with popup-specific selectors.
- **Hardcoding `ResolveAsync` to one branch.** If the test only handles SoftMatch and a HardMatch shows up, you want a clear `match type unexpected` failure — `ResolveAsync` already does this. Don't bypass it with branch-specific calls unless the test genuinely depends on the branch.
- **Letting the popup dismiss itself "eventually."** If a popup appears, resolve it explicitly. Drive the dismissal — don't wait for it.
