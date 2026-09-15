# Bolt.Automation.InfraTests

Infrastructure verification tests (NUnit), separated from the main test project so environment/plumbing checks can run independently of product tests.

## Contents (`InfraTests/`)

| File | Verifies |
|---|---|
| `AdbxApiTests.cs` | ADBX API client + auth pipeline |
| `GetQuoteApiTests.cs` | GetQuote API client |
| `PartnerPortalApiTests.cs` | Partner Portal API client |
| `SsoApiTests.cs` | SSO/SAML flow |
| `DBTests.cs` | Database connectivity |
| `OutlookAndFeatureFlagTests.cs` | Outlook (email) + LaunchDarkly integrations |
| `LoggerCapabilitiesDemo.cs` | Logger capability showcase (demo, not a template — it uses bare `StartStep` scopes that real tests must not copy; use `ExecuteStepAsync`) |

## Running

```bash
dotnet test Bolt.Automation.InfraTests/Bolt.Automation.InfraTests.csproj

# Filtered
dotnet test Bolt.Automation.InfraTests/Bolt.Automation.InfraTests.csproj --filter "FullyQualifiedName~SsoApiTests"
```

## Configuration

Same configuration structure as the main test project: `appsettings.json` + per-environment overrides (`Development`, `QA`, `UAT`), `nlog.config`. Secrets come from the Bolt secrets bundle (`BOLT_SECRETS_PATH` — see the `nexus-secrets` skill). Reporting goes through the shared MongoDB logging layer.

## Dependencies

References the same core projects as the main test project: Core, Common, ApiClients, FrontEnds, TestDataProvider, ExternalServices.
