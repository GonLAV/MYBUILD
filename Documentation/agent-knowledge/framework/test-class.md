---
topic: framework:test-class
summary: TestBase / UITestBase skeleton, [Tenant]/[Category] attributes, IScopeContext lifecycle, data builders.
status: ready
---

# Test class skeleton

> **When to read:** Phase 3 (writing the test class).
>
> **Not this base?** Professional Services / runtime-injected tests (new tenant or environment,
> no static data-store entry, values from `INJECTED_*` env vars) use
> `InjectionTestBase`/`UIInjectionTestBase` instead — see [injection-tests.md](injection-tests.md)
> (`framework:injection-tests`), including the multi-axis injected-parameter pattern (state x LOB).

## Where the file lives

`Bolt.Automation.Tests/Tests/<Tenant>/<TenantName>_<Feature>Tests.cs`. Examples:

- `Bolt.Automation.Tests/Tests/KLX/KLX_CLInterviewTests.cs` (TC 240782 — this session).
- `Bolt.Automation.Tests/Tests/Interview/CLTests.cs` (BOLTAG CL).
- `Bolt.Automation.Tests/Tests/CaseManagerBrowserTests.cs` (login → quote summary → edit → interview pattern).

Use the tenant abbreviation as the folder and a `<Tenant>_<Feature>Tests` class name.

## Minimal UI test skeleton

```csharp
using Bolt.Automation.Common;
using Bolt.Automation.Common.Enums;
using Bolt.Automation.Common.Exceptions;
using Bolt.Automation.Common.Logging.Extentions;
using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;
using Bolt.Automation.FrontEnds.Projects.ADBX.Pages;
using Bolt.Automation.FrontEnds.Projects.ADBX.Popups;
using Bolt.Automation.FrontEnds.Projects.Interview.Flows;
using Bolt.Automation.FrontEnds.Projects.Interview.Pages;
using Bolt.Automation.TestDataProvider.TestData.CommonTestData;
using Bolt.Automation.Tests.TestExtension.Attributes;
using Bolt.Automation.Tests.TestExtension.Base;
using Bolt.Automation.Tests.TestHelpers.ADBX;
using NUnit.Framework;
using static Bolt.Automation.TestDataProvider.TestData.ApplicationTestData.ApplicationTestData;
using FieldNames = Bolt.Automation.FrontEnds.Projects.Interview.FormData.FieldNames;
using BoltEnvironment = Bolt.Automation.Common.Environment;
using AuthorAttribute = Bolt.Automation.Tests.TestExtension.Attributes.AuthorAttribute;

namespace Bolt.Automation.Tests.Tests.KLX
{
    // Secondary-front-end pattern: this fixture drives Interview after ADBX, and lives outside
    // Tests/AdbxTests. A fixture whose front end IS ADBX should inherit AdbxUITestBase instead —
    // see the note under the code block.
    public class KLX_CLInterviewTests : UITestBase
    {
        private AdbxTestHelper _adbxHelper = null!;

        public KLX_CLInterviewTests() : base()
        {
            ScopeContext.Set(ctx => ctx.FrontEnd, FrontEndType.ADBX);
        }

        protected override void InitializeComponents()
        {
            base.InitializeComponents();
            _adbxHelper = new AdbxTestHelper(_logger, BrowserManager, PageFactory, _pageHelper!, ScopeContext);
        }

        [Test]
        [RunIn(BoltEnvironment.Staging)]
        [Tenant(Tenant.KRAFTLAKEX)]
        [Category("KLX")]
        [Category("CL")]
        [Category("Quoting")]
        [Category("InterviewV2")]
        [Author(Author.Viktor)]
        [TestCaseId(240782)]
        [Description("KLX LSP agent submits a CL Auto quote through the old interview to rates")]
        public async Task KLX_CLAuto_OldInterview_E2E_SubmitQuote()
        {
            var user = TestContextAccessor.CurrentUserCollection.Agent
                ?? throw new TestSetupException("KLX Staging Agent user not configured");
            var loginUrl = ScopeContext.Data.UrlDataCollection.FrontEnd?.LoginUrl
                ?? throw new TestSetupException("KLX Staging LoginUrl not configured");

            var personalInfo = PersonalInfo.GetRandomPersonalInfo();
            var driver = Drivers.KLXTestDriver;
            var address = AddressData.TX;
            const string businessName = "Test Automation";
            var effectiveDate = DateTime.Today.AddDays(2).ToString("MM/dd/yyyy");
            var operatorDob = DateTime.Parse(driver.DOB).ToString("MM/dd/yyyy");

            await _adbxHelper.LoginAsync(user, loginUrl);

            // ... open existing account, click NEW QUOTE, resolve account-match popup ...

            // Switch FrontEnd before driving Interview pages
            ScopeContext.Set(ctx => ctx.FrontEnd, FrontEndType.Interview);
            var startPage = PageFactory.CreatePage<Product_StartPage>();

            // Reusable CL Auto defaults from TestDataProvider; per-test overrides below.
            // Anything not set here falls back to the field registry's DefaultValue.
            var formData = CLAutoFormData.Defaults;
            formData[FieldNames.InterviewAddress] = $"{address.AddressLine1}, {address.City}, {address.State}, {address.ZipCode}";
            formData[FieldNames.OrganizationName] = businessName;
            formData[FieldNames.FirstName] = personalInfo.FirstName;
            formData[FieldNames.LastName] = personalInfo.LastName;
            formData[FieldNames.PrimaryPhoneNumber] = personalInfo.PrimaryPhoneNumber;
            formData[FieldNames.EposNaicDescription] = "Auto Hauling, Long-Distance";
            formData[FieldNames.VIN] = "4RAVS1629TK143345";
            formData[FieldNames.Gender] = driver.Gender;
            formData[FieldNames.MaritalStatus] = driver.MaritalStatus;
            formData[FieldNames.OperatorDateOfBirth] = operatorDob;
            formData[FieldNames.DriverLicenseNumber] = driver.DriverLicenseNumber ?? "25486475";
            formData[FieldNames.EffectiveDate] = effectiveDate;
            formData[FieldNames.Email] = personalInfo.Email;

            var resultsPage = await Executor.ExecuteToPage<Product_ResultsPage>(
                FlowType.InterviewCLAutoFlow, startPage, formData, fillForms: true);

            await _logger.ExecuteStepAsync("Verify rates returned", async () => {
                var hasRates = await resultsPage.IsGetRates();
                _logger.LogDataValidation("Has Rates", hasRates, "true", hasRates.ToString(),
                    "Quote should return at least one rate");
                Assert.That(hasRates, Is.True, "No rates returned on the result page");
            });
        }
    }
}
```

The reusable `CLAutoFormData.Defaults` lives in `Bolt.Automation.TestDataProvider/TestData/ApplicationTestData/AutoUIFormData.cs` and carries the flow's non-default values (business profile, vehicle profile, CL Policy coverage limits + deductibles). See [../domain/partners/INDEX.md](../domain/partners/INDEX.md) for the data-provider extraction pattern.

## Attributes — what each one does

| Attribute | Purpose | When required |
|---|---|---|
| `[Test]` | NUnit test marker. | Always. |
| `[Tenant(Tenant.X)]` | Sets `ScopeContext.Tenant` and resolves user/URL data via `(Tenant, Environment)` lookup. | Tenant-specific tests (most). |
| `[RunIn(Environment.X)]` | Gates execution; tests skip with `Assert.Inconclusive` when a different env is configured. | Tests pinned to QA/Staging/UAT/Production. |
| `[NotRunIn(Environment.X)]` | Inverse — exclude an environment. | Rare; `[RunIn]` is usually clearer. |
| `[Category("X")]` | NUnit category for filtering (`--filter "Category=X"`). | At least one (KLX, CL, Quoting, etc.). |
| `[Category("InterviewV2")]` | Convention category for tests that drive the Interview v2/v3 flow (matches `BOLTAG_CL_Consumer_E2E_Test` and `KLX_CLAuto_OldInterview_E2E_SubmitQuote`). | Any test using an `Interview*Flow` via `Executor.ExecuteToPage`. |
| `[Author(Author.X)]` | Test owner for reporting. | Always; use the `Author` enum value for the engineer. |
| `[TestCaseId(N)]` | Azure DevOps test case ID. | When the TC is tracked in ADO. |
| `[Description("...")]` | Human-readable test description. | Strongly recommended; appears in reports. |

Multiple `[Category]` attributes are allowed; they're treated as conjunctive in filters (`--filter "Category=KLX&Category=CL"`).

## `UITestBase` — what's available

`Bolt.Automation.Tests/TestExtension/Base/UITestBase.cs` provides (protected unless noted):

| Member | Type | Use |
|---|---|---|
| `BrowserManager` | `IBrowserManager` | Tab/page lifecycle, navigation, screenshot manager. |
| `PageFactory` | `IPageFactory` | `PageFactory.CreatePage<TPage>()` — DI-resolved, validates page-ready in ctor. |
| `Executor` | `PlaywrightExecutor` | Flow execution. |
| `_pageHelper` | `IPageHelper?` | Direct locator interaction (also exposed via pages). |
| `ScopeContext` | `IScopeContext` | Per-test context store (inherited from `TestBase`). |
| `_logger` | `IAutomationLogger?` | Step/business-rule logging (inherited). |
| `TestContextAccessor` | static-ish | Read access to `CurrentUserCollection`, `CurrentUrlCollection`, etc. (inherited). |

`InitializeComponents()` (override hook) runs after the browser is up and DI scope is resolved. Use it to construct test-specific helpers (e.g. `AdbxTestHelper`) — not the constructor, because `_pageHelper` isn't ready yet there.

## Login flow

`Bolt.Automation.Tests/TestHelpers/ADBX/AdbxTestHelper.cs`:

```csharp
public async Task LoginAsync(UserTestData user, string url) {
    ScopeContext.Set(ctx => ctx.CurrentUser, user);
    await BrowserManager.NavigateAsync(url);
    var login = PageFactory.CreatePage<STS_LoginPage>();
    await login.Login();   // reads CurrentUser, fills, clicks, waits for token redirect
}
```

`STS_LoginPage.Login()` (in `Bolt.Automation.FrontEnds/Projects/STS/STS_LoginPage.cs`) reads `ScopeContext.Get(ctx => ctx.CurrentUser)`, fills username/password, submits, then `WaitForFunctionAsync` polls until the URL no longer contains `token=` (the redirect completes).

URL is read from the resolved `UrlDataCollection`:

```csharp
var loginUrl = ScopeContext.Data.UrlDataCollection.FrontEnd?.LoginUrl
    ?? throw new TestSetupException("KLX Staging LoginUrl not configured");
```

### Which base class for an ADBX fixture

- **Front end IS ADBX** (a fixture in `Tests/AdbxTests/`) → inherit **`AdbxUITestBase`**. It sets
  `FrontEndType.ADBX` and exposes lazy `AdbxHelper` and `SsoHelper` properties, so the fixture needs
  no constructor and no `InitializeComponents` override. Do not hand-construct the helper there.
- **ADBX is a step on the way somewhere else** (Interview, D2C, PartnerPortal, Professional Services)
  → stay on your own base and hand-construct as the KLX example above does. Inheriting
  `AdbxUITestBase` would stamp `FrontEndType.ADBX` on a fixture that is not an ADBX test.

The helpers on the base are lazy because `_pageHelper` and the browser are not populated until
`UITestBase`'s `[SetUp]`, which runs after the constructor. Caching is safe: `AssemblyInfo.cs` sets
`FixtureLifeCycle(LifeCycle.InstancePerTestCase)`, so each test case gets a fresh instance.

## `AdbxTestHelper` — common operations

| Method | Use |
|---|---|
| `LoginAsync(user, url)` | Standard login → returns nothing; populates `ScopeContext.CurrentUser`. Use this when the login does **not** land on Home (MFA challenge, access denied) or the page is not needed. |
| `LoginToHomeAsync(user, url)` | Login → returns the `ADBX_HomePage` to navigate from. |
| `LoginAndNavigateToQuoteAsync(user, url, friendlyId)` | Login + search Home for a friendly id + select row 1. |
| `OpenAccountFromAccountsTabAsync(homePage, email)` | Navigate Accounts tab, search by email, select row 1; returns `(found, AccountSummaryPage?)`. |
| `SearchAndOpenAccountFromHomeAsync(...)` | Same idea from the Home page search. |
| `CreateAccountViaNewQuoteAsync(additionalFormData?)` | Click New Quote on Home, fill popup, click Add, switch to new tab, navigate back, return `(email, homePage)`. |

**SSO login is not on this helper.** `AdbxTestHelper` knows nothing about SSO — use `SsoHelper.LoginAsync(user, relayState, BrowserManager)`, which `AdbxUITestBase` exposes as a `SsoHelper` property right beside `AdbxHelper`. SSO is front-end agnostic (the relay state decides where you land), so it does not belong to ADBX. `DefaultsTests.LoginForTenantAsync` is the worked example: LIBERTYX and COMPARION reach ADBX only by SSO, UNIFY and BOLTAG only by login URL.

A nullable ADBX page threaded into `NavigateToMenuAsync` / `NavigateInnerTabAsync` / `NavigateToAdminMenuAsync` is a **protocol, not an oversight**: those read `null` as "SSO already landed you on the target page, skip navigation."

For the typical "log in → open existing account → click NEW QUOTE" entry path:

```csharp
await _adbxHelper.LoginAsync(user, loginUrl);

var accountSummary = await _logger.ExecuteStepAsync("Open the first existing account from Accounts tab", async () => {
    var homePage = PageFactory.CreatePage<ADBX_HomePage>();
    await homePage.ClickOnMenuTab(NavigationType.Accounts);
    PageFactory.CreatePage<ADBX_AccountsTabPage>();
    await _pageHelper!.SelectTableRowAsync(1);
    var page = PageFactory.CreatePage<ADBX_AccountSummaryPage>();
    if (await page.IsUpdateAccountInformationPopUpExists()) {
        var updateAccountPopup = PageFactory.CreatePage<ADBX_EnterUpdateAccountInformationPopup>();
        await updateAccountPopup.ClosePopup();
    }
    return page;
}, "Account Summary page is displayed");
```

## Form data — inline dictionary, not a helper method

Avoid a private `BuildFormData()` helper. Inline the dictionary in the test method, pulling a reusable per-flow profile from `ApplicationTestData` and then overriding only the per-test fields:

```csharp
var formData = CLAutoFormData.Defaults;                                  // reusable scaffold
formData[FieldNames.InterviewAddress] = $"{address.AddressLine1}, …";    // per-test override
formData[FieldNames.OrganizationName] = businessName;
formData[FieldNames.VIN] = "4RAVS1629TK143345";
// … only the keys that differ from registry defaults
```

Reasons this beats `BuildFormData()`:

- The test reads top-to-bottom; you don't have to scroll to a separate method to see what data is being driven.
- It matches `BOLTAG_CL_Consumer_E2E_Test` and the cleaned KLX test on `wip/klx-cl-auto-tc240782`.
- The reusable `CLAutoFormData.Defaults` (in `Bolt.Automation.TestDataProvider`) is shareable across multiple tenants doing the same flow; a private helper is not.

**Critical:** only set keys whose values differ from the registry's `DefaultValue` (and from whatever the profile already supplies). `MergeDataManager.GetSmartFormData` fills the rest. Re-supplying defaults is noise that hides the data the test actually cares about. See [field-registry.md](field-registry.md) "Test dictionaries should be sparse" and [../domain/partners/INDEX.md](../domain/partners/INDEX.md) "Reusable UI form-data profiles."

## Test method shape (rule of thumb)

A good UI test method:

1. Reads user + URL from `TestContextAccessor` / `ScopeContext`.
2. Declares local data fixtures (`driver`, `address`, `personalInfo`, `effectiveDate`, …).
3. Logs in via the helper.
4. Navigates to the entry point (existing account / new quote / SSO landing).
5. Switches FrontEnd if crossing the ADBX → Interview boundary.
6. Pulls `ApplicationTestData.<Flow>FormData.Defaults`, layers per-test overrides.
7. Calls `Executor.Execute<…>` or `ExecuteToPage<…>` with the layered dictionary.
8. Asserts the final page state with `LogDataValidation` + `Assert.That`.

Total: 30–80 lines including the inline form data. Anything longer is usually a sign that some logic belongs in a page object or `ApplicationTestData` profile.

## Running the test locally

```bash
# Build
dotnet build Bolt.Automation.sln

# Run a single test
dotnet test Bolt.Automation.Tests/Bolt.Automation.Tests.csproj \
  --filter "FullyQualifiedName~KLX_CLAuto_OldInterview_E2E_SubmitQuote" \
  --settings Bolt.Automation.Tests/local.runsettings

# Or via env
dotnet test --filter "Category=KLX&Category=CL" \
  --settings Bolt.Automation.Tests/qa.runsettings
```

`local.runsettings` / `qa.runsettings` / `staging.runsettings` set `ASPNETCORE_ENVIRONMENT` and any per-env overrides. `[RunIn(Staging)]` skips with `Assert.Inconclusive` if the env doesn't match.

## After-run artifacts

`UITestBase.UiTearDownAsync` captures the final DOM snapshot and screenshot under:

```
Bolt.Automation.Tests/bin/Debug/net10.0/TestResults/<test-name>/
  page_source_*.html
  screenshot_*.png
```

These are the primary inputs for Phase 4 diagnosis. Open the failing page's HTML, grep for the field's expected `class` substring, and verify the locator the registry would have used.

## Anti-patterns

- **Private `BuildFormData()` helper method.** Inline the dictionary in the test method instead. The pattern that the rest of the codebase uses (`BOLTAG_CL_Consumer_E2E_Test`, cleaned `KLX_CLAuto_OldInterview_E2E_SubmitQuote`) is `var formData = <Profile>.Defaults; formData[FieldNames.X] = ...;`.
- **Re-supplying values that already match the registry `DefaultValue`.** `MergeDataManager.GetSmartFormData` fills defaults for missing keys; every redundant entry is noise that obscures the values the test actually cares about.
- **Re-resolving the user inside the test method** when `TestContextAccessor.CurrentUserCollection.Agent` already exists.
- **Hardcoding the login URL.** Always read from `ScopeContext.Data.UrlDataCollection`.
- **Reusing `PageFactory.CreatePage<TPage>()` for the same page twice.** It validates page-ready each time and may throw if the page has navigated away. Cache the reference.
- **Forgetting the FrontEnd switch** when crossing ADBX → Interview. The Interview registry won't find ADBX-only fields and vice versa.
- **Asserting via `Assert.IsTrue` only** with no actual value anywhere in the report — the assertion fails and there is nothing to diagnose. Give the assertion a message that names the actual value, or log it where the value is gathered; add `LogDataValidation` only when neither is possible (see [logging.md](logging.md), "When it is not needed").
