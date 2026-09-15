# Bolt Automation Nexus — Solution Map

## Overview
Multi-project .NET 10 test automation solution targeting insurance platforms. Tenants: BOLTAG, UNIFY, PROGRESSIVEPL, USAA, COMPARION, LIBERTYX, KRAFTLAKEX, BOLTACCESS.

Per-project conventions live in each project's `CLAUDE.md`; deep framework/domain knowledge lives in [agent-knowledge/](./agent-knowledge/INDEX.md). This map is the orientation layer.

## Projects

### Bolt.Automation.Common
Shared primitives used by everything else.
- `Context/` — `IScopeContext` and per-test scope data (`ScopeContext.Data`: tenant, environment, user/URL/relay-state collections, HTTP headers)
- `Logging/` — `IAutomationLogger`, step scopes (`ExecuteStepAsync`), the Mongo reporting layer (`Logging/Mongo/` — see [MongoReporting-Implementation.md](./MongoReporting-Implementation.md))
- `Enums/` — `Tenant`, `Environment`, `UserRole`, `FrontEndType` (D2C, ADBX, PartnerPortal, HQXConsumer, HQXAgent, Interview), `CarrierEnums`, …
- `Configuration/`, `Exceptions/`, `Models/`, `PollyRetry/`, `Reporting/`, `Services/`

### Bolt.Automation.Core
DI composition root.
- `ConfigurationLoader` — builds configuration from JSON files → Bolt secrets bundle → env vars → command line → test parameters → `runsettings.local.json` (see [Configuration.md](./Configuration.md))
- `Infrastructure/InfrastructureServiceCollectionExtensions` — central `Add*Services` registration (see [ServiceRegistration.md](./ServiceRegistration.md))

### Bolt.Automation.TestDataProvider
Test data management. Structure:
- `Context/TestContextAccessor` — primary access point; hydrates users/Twilio/urls/test-specific values from the data stores into the scope context
- `Repositories/DataStores/` — static per-(tenant, environment) stores: `UserDataStore`, `UrlDataStore`, `TwilioDataStore`, `DatabaseConnectionsDataStore`, `SsoRelayStateDataStore`, `CarrierBridgeUrlDataStore`, `TestSpecificDataStore`. User/Twilio secrets themselves come from the secrets bundle (see [Secrets-Tier2-UserTwilio-Migration-Spec.md](./Secrets-Tier2-UserTwilio-Migration-Spec.md))
- `Providers/` — factory classes per API family: `AdbxApiDataProvider`, `CaseManagerAPIDataProvider`, `CommonDataProvider`, `GetQuoteApiDataProvider`, `PlatformAPIDataProvider`
- `TestData/` — static scenario data (`ApplicantTestData`, `ApplicationTestData`, `PlatformAPITestData`, `CoverageModificationTestData`, `PartnerPortalTestData`, `PageSkippingTestData`, `SoldNoteTestData`, `D2CFarmers`, …)
- `Enums/`, `Extensions/`
Details: [TestDataProvider_Architecture_Documentation.md](./TestDataProvider_Architecture_Documentation.md)

### Bolt.Automation.ApiClients
Refit HTTP clients — all HTTP goes through a Refit interface + auth pipeline (no raw `HttpClient`).
- Client families: `AdbxApi`, `CaseManagerApi`, `GetQuoteApi`, `PlatformApi`, `PartnerPortalApi`, `IntegrationHubApi` (Twilio webhooks), `SSO` (incl. SAML signing under `SSO/Saml/`), `StsApi`
- `Infrastructure/` — `RefitApiServiceLocator`, `RetryHelper`, header cache, token cache
Details: [ApiClients/ApiClients.md](./ApiClients/ApiClients.md)

### Bolt.Automation.FrontEnds
Playwright UI automation.
- `PlaywrightBase/` — `BrowserManager`, `PageHelper` (string field-name interactions), `FieldRegistry` infrastructure, waits, popups, screenshots
- `Projects/` — per-front-end page objects + registries: `ADBX`, `D2C`, `HQXAgent`, `HQXConsumer`, `Interview`, `PartnerPortal`, `STS`
- `Executor/` — `PlaywrightExecutor` flow walker (`FlowType`-keyed page sequences)
- `FormData/`, `CommonHelpers/`, `InterviewFlowHelpers/`, `Interfaces/`

### Bolt.Automation.InternalServices
Internal microservice integrations: scope-context implementations (`Context/`), generic microservice client factories, `Database/`, `MultiConfiguration/`.

### Bolt.Automation.ExternalServices
`LaunchDarkly` (feature flags), `Outlook` (email), `CasePortal`.

### Automation.Configuration
Options classes only, one folder per domain: `ApiClients/`, `Common/`, `ExternalServices/`, `FrontEnds/`, `InternalServices/`. See [OptionsClasses.md](./OptionsClasses.md).

### Bolt.Automation.Tests
Main test project.
- `TestExtension/` — `TestBase` (API) / `UITestBase` (UI), attributes (`[Tenant]`, `[TestCaseId]`), scope + reporting wiring
- `Tests/` — by area: `AdbxTests/`, `D2C/`, `Interview/`, `Progressive/`, `ProfessionalServices/`, `DevOps/`, plus `SSOTests`, `GetQuoteApiTests`, `CaseManagerApiTests`, `PartnerPortalTests`, `ProvisioningTests`, …
- `appsettings.{json,QA,UAT,Staging,Production,Development}.json`, `nlog.config`

### Bolt.Automation.InfraTests
Infrastructure verification tests (NUnit): API smoke (`AdbxApiTests`, `GetQuoteApiTests`, `SsoApiTests`), `DBTests`, logger capability demos.

### Bolt.Automation.TestDiscovery / Bolt.Automation.WorkerAgent / Bolt.Automation.AgentTools
- `TestDiscovery` — enumerates tests for distributed execution
- `WorkerAgent` — Generic-Host worker for the external orchestrator (see [CI-CD-TestExecution-Guide.md](./CI-CD-TestExecution-Guide.md))
- `AgentTools` — the `nexus-agent` CLI (`kb`, `tc`, `failure`, `code`, `browser`, `philosophy`, `secrets`, `doctor`, `saml`; see [AI-Agent-Extension-Architecture.md](./AI-Agent-Extension-Architecture.md))

## Key integration points

- **Scope flow**: `TestBase` creates a scoped service provider → `IScopeContext` initialized with tenant/environment from attributes → `TestContextAccessor` hydrates test data → HTTP headers propagate automatically to Refit clients.
- **UI flow**: tests call page objects → `PageHelper.InteractWithField(fieldName)` resolves through the `FieldRegistry` for the active `FrontEndType` → `PlaywrightExecutor` walks `FlowType` page sequences with merged default+override form data.
- **Data keying**: all static data keyed by `(Tenant, Environment)`; secrets resolved via the Bolt secrets bundle (`BOLT_SECRETS_PATH`).
- **Reporting**: run/test/log documents in MongoDB, artifacts in S3, orchestrator UI + `nexus-logger` MCP on top.
