---
topic: partner:index
summary: Partner tenants overview — which partners exist, what FrontEnds they use, where data lives in TestDataStore.
status: ready
---

# Partner tenants — index

> **When to read:** Phase 1 (matching the TC's tenant/role to existing data), Phase 3 (adding a new `UserDataStore` entry), Phase 3 (pulling a reusable `ApplicationTestData` profile).
>
> **Per-partner pages** in this directory carry the partner-specific UI quirks (KLX class collapsing, USAA SSO, Progressive HQX 2.0, etc.). This file is the *data + architecture* side — what enums exist, how login/users/URLs are resolved.

## `Tenant` enum

Defined in `Bolt.Automation.Common/Enums.cs`:

```csharp
public enum Tenant {
    BOLTAG,        // Bolt Agent Gateway — primary multi-LOB tenant
    PROGRESSIVEPL, // Progressive Personal Line
    USAA,          // USAA SSO integration
    UNIFY,         // Multi-tenant platform (subtenants: EXTERNAL, marketslib)
    COMPARION,     // Comparion platform (multi-tenant)
    LIBERTYX,      // Liberty Mutual LibertyX
    KRAFTLAKEX,    // KraftLakeX LSP platform
    BOLTACCESS     // Bolt Access (Agents)
}
```

Tenant is set on the test class via `[Tenant(Tenant.X)]` and resolves into `ScopeContext.Data.Tenant` during `TestBase.InitializeTestAsync`.

Each partner has its own page in this directory:
- [boltag.md](boltag.md)
- [kraftlakex.md](kraftlakex.md)
- [unify.md](unify.md)
- [progressivepl.md](progressivepl.md)
- [usaa.md](usaa.md)
- [comparion.md](comparion.md)
- [libertyx.md](libertyx.md)
- [boltaccess.md](boltaccess.md)

Narrow partner-specific gotcha pages (also in this directory):
- [pgr-quote-status-polling.md](pgr-quote-status-polling.md) — `QuoteStatusWithPollingAsync` polls until the expected pair; asserting that pair afterwards can never fail.
- [pgr-covmod-wind-hail-na.md](pgr-covmod-wind-hail-na.md) — Wind/Hail Not-Applicable mirrors the Standard deductible, no editable dropdown.

## `Environment` enum

```csharp
public enum Environment {
    Qa,
    Dev,
    Uat,
    Staging,
    Production
}
```

Set via the `ASPNETCORE_ENVIRONMENT` env var (read at framework init) and exposed via `ScopeContext.Data.Environment`. Tests with `[RunIn(Environment.X)]` skip with `Assert.Inconclusive` when running outside the configured env.

## Multi-tenant architectures

Some tenants have additional sub-structure:

- **UNIFY** — has subtenants (EXTERNAL, marketslib); `LoginUrl` differs per subtenant. See [unify.md](unify.md).
- **COMPARION** — SSO-only; no username/password in QA/UAT. See [comparion.md](comparion.md).
- **KRAFTLAKEX** — LSP partner model; agents have a `GroupExternalId` (e.g. `"AOR-TEST"`). See [kraftlakex.md](kraftlakex.md).

These don't need new enum values — they're modelled in `UserTestData` properties.

## Reusable UI form-data profiles

Per-flow defaults that differ from the field registry's `DefaultValue` live in `Bolt.Automation.TestDataProvider`. The canonical file is:

```
Bolt.Automation.TestDataProvider/TestData/ApplicationTestData/AutoUIFormData.cs
```

Existing static classes inside `ApplicationTestData`:

| Class | What it covers | Where used |
|---|---|---|
| `DriverFormData.GailSchaffPrimary` / `AbbadeSecondaryDriver` / `SafecoLicenseDetails` | Safeco D2C driver fixtures. | D2C Safeco purchase tests. |
| `VehicleFormData.Lexus2007` | 2007 LEXUS IS 250 PL Auto vehicle. | PL Auto / D2C vehicle lifecycle tests. |
| `CLAutoFormData.Defaults` | Full Commercial Auto flow scaffold (Business + Vehicle + CL Policy values that differ from registry defaults). | `KLX_CLAuto_OldInterview_E2E_SubmitQuote` (TC 240782); reusable for other tenants doing CL Auto. |

The pattern when authoring a new test is:

```csharp
var formData = CLAutoFormData.Defaults;                                  // reusable scaffold
formData[FieldNames.InterviewAddress] = $"{address.AddressLine1}, …";    // per-test override
formData[FieldNames.OrganizationName] = businessName;
// …
await Executor.ExecuteToPage<Product_ResultsPage>(
    FlowType.InterviewCLAutoFlow, startPage, formData, fillForms: true);
```

### Constraint — TestDataProvider can't reference FrontEnds

`Bolt.Automation.TestDataProvider.csproj` references only `Bolt.Automation.ApiClients` and `Bolt.Automation.Common` (a `FrontEnds` reference would be circular). So **keys in these profile dictionaries are string literals** matching the `nameof(FieldX)` value of the constants in the `FieldNames` partials. Example from `AutoUIFormData.cs`:

```csharp
public static Dictionary<string, string> Defaults => new() {
    ["LegalEntity"]       = "Corporation",
    ["FederalIDNumber"]   = "478500001",
    ["BusinessStartYear"] = DateTime.Now.Year.ToString(),
    ["AnnualPayroll"]     = "0",
    // …
};
```

They must stay in sync with the registry's canonical names — renaming a `FieldNames.X` constant does not update the string-literal key here. If a profile entry stops working, first grep `FieldNames` for any renames.

### When to extract a profile

The profile concept earns its keep when a second test (different tenant, same flow) wants the same scaffold. For a one-off scenario with no expected reuse, just inline the dictionary in the test — don't pre-emptively create a profile.

### Consolidated `<Flow>FormData.Defaults` vs granular profiles

For **full-flow tests** using `Executor.ExecuteToPage`, a single per-flow `Defaults` dictionary (like `CLAutoFormData.Defaults`) is preferred — granular splits (one class per page) force consumers into manual merging with no real flexibility payoff. Symptoms of overshoot: the test starts with `.Concat(…).ToDictionary(…)` LINQ acrobatics across multiple sub-profiles. That's a signal to consolidate.

Granular profiles (`DriverFormData.X`, `VehicleFormData.X`) earn their keep in D2C-style flows that fill one entity per page via separate `popup.FillForm(profile)` calls — different consumption shape.

## `UserDataStore` — the data source of truth

`Bolt.Automation.TestDataProvider/Repositories/DataStores/UserDataStore.cs`. Schema:

```csharp
public static readonly Dictionary<(Tenant?, Environment?), UserTestDataCollection> All;
```

Each entry maps a `(Tenant, Environment)` tuple to a collection of named user roles. Example (KRAFTLAKEX Staging, around line 476):

```csharp
[(Tenant.KRAFTLAKEX, Environment.Staging)] = new UserTestDataCollection {
    Agent = new UserTestData {
        Username = "lsp1_aortest@test.com",
        Password = "Seapass1",
        Role = UserRole.Agent,
        Email = "lsp1_aortest@test.com",
        GroupExternalId = "AOR-TEST",
        ApiKey = "kS3TFwRsnrwdkA004KEuhBvSAt6YIyFkrkkBJsYF",
        AgentIdentity = "Basic bHNwMWFvcjE6QU9SLVRFU1Q=",
    },
    // Consumer = ..., Underwriter = ..., etc., as needed
}
```

`TestContextAccessor.CurrentUserCollection` reads `All[(Tenant, Environment)]` to find the right collection.

## `UserTestData` — the user model

Top-level fields you'll see most:

| Property | When used |
|---|---|
| `Username` / `Password` | Standard agent login (BOLTAG, KRAFTLAKEX, UNIFY). |
| `Role` (`UserRole` enum: Agent / Consumer / Underwriter / ServiceAgent / ServiceManager / SSO) | Always — drives `TestContextAccessor.CurrentUserCollection.<Role>`. |
| `Email` | Identity / search key. |
| `ApiKey` | API-driven flows; required for `EndUserApi`/`GetQuoteApi`/etc. |
| `AgentIdentity` | Basic-auth header value (`Basic <base64>`); used for some agent APIs. |
| `UserExternalId` | Tenant-level user ID (e.g. for case manager). |
| `GroupExternalId` | LSP/agent group identifier (e.g. KLX `"AOR-TEST"`). |
| `WorkSpaceGroupId` | Workspace-scoped group ID (BOLTAG specific). |
| `Sso` (`SsoUserData { Issuer, Audience }`) | SSO users (USAA, COMPARION, LIBERTYX). |
| `LoginUrl` | Override the tenant-default login URL (e.g. partner portal redirects). |
| `SendAgentIdentity` (bool) | Whether to include the `AgentIdentity` header on API calls. |
| `OAuthToken` | Progressive-specific token. |

Roles within `UserTestDataCollection`:
- `Agent` — primary login user for agent flows.
- `Consumer` — D2C user (no password, API-only).
- `Underwriter` — case manager.
- `ServiceAgent` / `ServiceManager` — service-side auth.
- `Sso<Variant>` — multiple SSO users for different test cases.

## Adding a new `(Tenant, Environment)` entry

1. Open `UserDataStore.cs`. Find the alphabetically-grouped section for your tenant.
2. Add the tuple key + `UserTestDataCollection` literal, mirroring the closest existing entry as a template.
3. Populate the roles you need. Don't fill every role — start with `Agent`; add others when a future test needs them.
4. **Don't commit real production credentials.** Test credentials are typically tagged with stage suffixes (`@test.com`, fake phone numbers).
5. Verify by running an existing test in the new env or a new test you're writing. The first sign of a missing entry is `TestContextAccessor.CurrentUserCollection.Agent` returning null and your test throwing `TestSetupException`.

## URL collection

`ScopeContext.Data.UrlDataCollection` is populated similarly from a URL data store. Read `LoginUrl`, `BaseUrl`, etc. like:

```csharp
var loginUrl = ScopeContext.Data.UrlDataCollection.FrontEnd?.LoginUrl
    ?? throw new TestSetupException("LoginUrl not configured");
```

If your tenant/env tuple has no URL entry, the resolution returns null and you'll throw at the null-coalesce.

## Resolving role to user

In a test:

```csharp
var user = TestContextAccessor.CurrentUserCollection.Agent
    ?? throw new TestSetupException("KLX Staging Agent user not configured");
```

`CurrentUserCollection` is keyed on `(ScopeContext.Tenant, ScopeContext.Environment)` set by `[Tenant]` + the active environment. **Never** load `UserDataStore.All` directly — that bypasses the framework's resolution and breaks tenant-scoping.

## Anti-patterns

- **Hardcoding credentials in the test class.** Always use `TestContextAccessor.CurrentUserCollection.<Role>`.
- **Using a different tenant's user.** A test marked `[Tenant(Tenant.KRAFTLAKEX)]` resolves the KLX user collection — using BOLTAG creds inside it means you skipped the attribute. Fix the attribute, not the lookup.
- **Sharing one `(Tenant, Environment)` entry across roles.** If `Agent` and `Consumer` differ, they're different `UserTestData` objects under different role properties — not a single one.
- **Embedding the password into a string-formatted log line.** Even at Debug level. Use `[REDACTED]` or omit entirely.

## When tenant/env is missing

Sometimes a TC mentions a tenant the framework hasn't onboarded. Two options:

1. **Add the tenant** if it's a long-term need: new `Tenant` enum value + new `UserDataStore` entries + any tenant-specific UI quirks (see the per-partner pages).
2. **Defer** if it's a one-off: tell the user the framework lacks tenant support; ask whether to scaffold the tenant before authoring the test, or to find an equivalent existing tenant.

The skill should default to option 2 — adding a tenant is a multi-file change with infrastructure implications. Don't sneak it in as part of a TC implementation.
