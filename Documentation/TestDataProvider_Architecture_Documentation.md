# TestDataProvider — Architecture

`Bolt.Automation.TestDataProvider` is the test-data layer: static per-(tenant, environment) data stores, a scope-aware accessor, and factory providers that build API request payloads.

## Structure

```
Bolt.Automation.TestDataProvider/
├── Context/
│   └── TestContextAccessor.cs        # Scope-aware access point (registered scoped in DI)
├── Repositories/DataStores/          # Static Dictionary<(Tenant, Environment), Collection> stores
│   ├── UserDataStore.cs              # User collections (credentials hydrated from secrets bundle)
│   ├── UrlDataStore.cs               # Front-end / API URL collections
│   ├── TwilioDataStore.cs            # Twilio numbers/config (secrets-hydrated)
│   ├── DatabaseConnectionsDataStore.cs
│   ├── SsoRelayStateDataStore.cs
│   ├── CarrierBridgeUrlDataStore.cs
│   └── TestSpecificDataStore.cs      # Free-form (tenant, env, key) → value
├── Providers/                        # Factory classes that build API payloads
│   ├── AdbxApiDataProvider/          # CaseViewDataProvider, PolicyDataProvider
│   ├── CaseManagerAPIDataProvider/   # CreateCase/CreateCaseMessage/CreateConsumer/CreatePolicyBinder/UpdateConsumer
│   ├── CommonDataProvider/           # AccountDataProvider (random personal info + address)
│   ├── GetQuoteApiDataProvider/      # PersonalLine/CommercialLine providers + data mappers
│   └── PlatformAPIDataProvider/      # QuoteStartPrefill, QuoteStatus, RetrieveQuote, DeepLink,
│                                     #   QuoteDeepLink, Provisioning, CRMNote
├── TestData/                         # Static scenario data grouped by area
│   ├── ApplicantTestData/  ApplicationTestData/  CommonTestData/
│   ├── PlatformAPITestData/          # incl. FULL_CUSTOMIZATION_GUIDE.md for QuoteStart requests
│   ├── CoverageModificationTestData/ PartnerPortalTestData/  PageSkippingTestData/
│   ├── SoldNoteTestData/  D2CFarmers/
├── Enums/                            # ApiEndpointType, FormType, PageType, Source
├── Extensions/                       # PlatformApiLobExtensions
└── TestDataProviderServiceExtensions.cs  # AddTestDataProvider → registers TestContextAccessor (scoped)
```

## TestContextAccessor

The single scope-aware entry point (primary-constructor class taking `IScopeContext`). Resolves the current tenant + environment from `_scopeContext.Data` and exposes:

| Member | Returns |
|---|---|
| `CurrentUserCollection` | `UserTestDataCollection` — users by `UserRole`, credentials hydrated from the secrets bundle |
| `CurrentUrlCollection` | `UrlTestDataCollection` — front-end/API URLs for the active tenant/env |
| `CurrentTwilioData` | `TwilioTestData?` — secrets-hydrated Twilio data (subtenant-aware) |
| `CurrentRelayStateCollection` | SSO relay states |
| `CurrentDatabaseConnectionCollection` | DB connection descriptors |
| `CarrierBridgeUrls` | Carrier-bridge URL collection |
| `GetTestSpecificValue(key)` | Free-form per-(tenant, env) value from `TestSpecificDataStore` |

## Secrets integration

Data stores hold the *shape* of the data; sensitive values (user passwords, Twilio credentials) are not in source. When `SecretsStore.Instance.IsLoaded`, `TestContextAccessor` hydrates them via `GetUserSecrets(tenant, env)` / `GetTwilioSecrets(tenant, env, subtenant)` from the Bolt secrets bundle (`BOLT_SECRETS_PATH`). Design record: [Secrets-Tier2-UserTwilio-Migration-Spec.md](./Secrets-Tier2-UserTwilio-Migration-Spec.md); local setup: the `nexus-secrets` skill.

## Data stores

Each store is a static class with an `All` dictionary keyed by `(Tenant, Environment)`:

```csharp
public static class UrlDataStore
{
    public static readonly Dictionary<(Tenant, Environment), UrlTestDataCollection> All = new()
    {
        ...
    };
}
```

Adding a tenant/environment = adding a dictionary entry. No I/O, no lazy loading — plain static data, hydrated with secrets at access time where applicable.

## Providers

Providers are factories that assemble request models for the API clients, defaulting everything except what the caller overrides — in line with the sparse-dictionaries philosophy (the test states only what differs). Example: `QuoteStartPrefillDataProvider` (`GetCustomQuoteStartRequest`, `GetScenarioBasedQuoteStartRequest`, `GetFullPrefillData`, `PrefillScenario` enum) — full customization guide in [TestData/PlatformAPITestData/FULL_CUSTOMIZATION_GUIDE.md](../Bolt.Automation.TestDataProvider/TestData/PlatformAPITestData/FULL_CUSTOMIZATION_GUIDE.md).

## Usage in tests

```csharp
// TestBase exposes the accessor; tenant/env come from the test's attributes
var agent = _testContextAccessor.CurrentUserCollection.GetUser(UserRole.Agent);
var ssoUrl = _testContextAccessor.CurrentUrlCollection.SsoApi;

// Providers build payloads with overrides only
var request = QuoteStartPrefillDataProvider.GetCustomQuoteStartRequest(...);
```

## DI registration

`AddTestDataProvider(services, configuration)` registers `TestContextAccessor` as scoped; everything else is static. Wired from `InfrastructureServiceCollectionExtensions` in `Bolt.Automation.Core`.
