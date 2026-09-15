---
name: nexus-secrets
description: Set up and maintain the local secrets bundle that nexus tests need to run outside CI. Guides first-time setup from zero (install AWS CLI → aws configure sso → aws sso login → nexus-agent secrets sync → BOLT_SECRETS_PATH → resolve target env) and ongoing maintenance (TTL refresh, --force, status, troubleshooting null secrets). Trigger when a local run fails with missing connection strings / "Set BOLT_SECRETS_PATH", when a user asks how to set up secrets locally, when app secrets (Outlook/Mongo/Aws/JWT/LaunchDarkly) resolve empty, when a test passes from the terminal but throws a secret/IOptions error (e.g. "LaunchDarkly SDK key is not configured") only when debugging in VS Code / Visual Studio Test Explorer, or when tenant tests skip with "Not configured to run in <env>". Do NOT trigger for authoring tests (nexus-test-author), debugging a flow failure unrelated to secrets (nexus-debug), or CI mount issues (DevOps-owned).
---

# nexus-secrets

Make a developer's machine able to run nexus tests that need real secrets, without any of those values living in the repo. That covers DB connection strings, app secrets (Outlook client secret, Mongo reporting, AWS S3 keys, the JWT/HMAC auth secret, LaunchDarkly SDK key, tenant BasicAuth), and per-user/Twilio test credentials (user passwords, GetQuote/Platform/CaseManager API keys, base64 basic-auth `AgentIdentity`/`ApiSource`/`OAuthToken`, Twilio `AuthToken`/`AccountSid`).

## Mental model: one file, three readers, two ways it arrives

There is exactly one bundle file: `automation_nexus_secrets_store.json`. Three independent consumers read it:

- `SecretsStore` (DB) reads `environments[<env>].tenants.<TENANT>.*SqlConnectionString`. The existing reader, used everywhere DB access happens.
- `BoltSecretsConfigurationProvider` (app secrets) reads `environments[<env>].appSecrets.*`, flattened into `IConfiguration`, so `IOptions<T>` binding (OutlookClientOptions, AuthenticationOptions, …) is unchanged.
- `SecretsStore` (user + Twilio, Tier-2) reads `environments[<env>].userSecrets.<TENANT>.<Role>.*` and `environments[<env>].twilio.<TENANT>.*`, overlaid at read-time onto the structural data in `UserDataStore`/`TwilioDataStore` by `TestContextAccessor`. Unlike DB (SSPI on Windows), these have no local fallback: without the bundle, affected API/UI/Twilio tests resolve empty credentials.

The file arrives one of two ways:
- CI: DevOps mounts it via a CSI volume and sets `BOLT_SECRETS_PATH`. Black box, out of scope here.
- Local: `nexus-agent secrets sync` fetches it from AWS Secrets Manager once, caches it on disk with a TTL, and you point `BOLT_SECRETS_PATH` at the cache dir. The same cached file feeds both readers.

`BOLT_SECRETS_PATH` is a directory. Both readers append `automation_nexus_secrets_store.json` when the path is a directory (mirror of `SecretsStore` behavior).

## The CLI

```
nexus-agent secrets sync [--region <r>] [--secret-id <id>] [--ttl <hours>] [--force]
nexus-agent secrets status
```
(If `nexus-agent` is not on PATH: `dotnet run --project Bolt.Automation.AgentTools -- secrets sync`.)

- **Credentials.** The default AWS credential chain: your SSO profile (`AWS_PROFILE` + `aws sso login`) or env creds. The tool never takes a secret or key as an argument.
- **Region + secret id.** Taken from `BoltSecrets:Region` / `BoltSecrets:SecretId` in `appsettings.json` (defaults: `us-east-1`, `automation/nexus/secrets-store`), overridable with `--region` / `--secret-id`.
- **One API call.** A single `GetSecretValueAsync` fetches the whole bundle; the payload is written verbatim to the cache.
- **Cache dir.** `%LOCALAPPDATA%\BoltAutomation\secrets` on Windows, i.e. `C:\Users\<you>\AppData\Local\BoltAutomation\secrets`. On Linux/macOS it is `~/.local/share/BoltAutomation/secrets` (`SpecialFolder.LocalApplicationData`). Outside any repo, so it can never be committed.
- **TTL.** Default 12h, tracked by a `.synced-at` sidecar. Within the TTL, `sync` is a no-op cache hit (no AWS call). `--force` ignores the TTL; `--ttl <hours>` changes the window.
- Never prints secret values. Only paths, counts, and ages.
- **Exit codes:** `0` success · `2` user-facing error (`config_missing`, `aws_fetch_failed`) · `3` bad input.

## First-time setup: full walkthrough from zero (no AWS CLI yet)

This is the complete path a brand-new developer takes on Windows. Walk them through in order; do not skip ahead. Each step gates the next. (`aws sso login` / `configure sso` are interactive and open a browser, so the *user* runs those; you run the non-interactive parts.) The nexus-specific AWS values are:

| Setting | Value |
|---|---|
| SSO start URL | `https://d-9067687362.awsapps.com/start` |
| SSO region | `us-east-1` |
| Account | `992382389570` |
| Role / permission set | `AutomationTeam` |
| Secret | `automation_nexus_secrets_store` (region `us-east-1`) |

### Step 1: install the AWS CLI v2
Check first: `aws --version`. If "not recognized":
```powershell
winget install --id Amazon.AWSCLI --accept-source-agreements --accept-package-agreements
```
It installs to `C:\Program Files\Amazon\AWSCLIV2\aws.exe`. It is not on PATH until you open a new terminal. Either reopen the shell, or call it by full path: `& "C:\Program Files\Amazon\AWSCLIV2\aws.exe" …`.
(Linux/macOS: install via your package manager / the AWS bundled installer.)

### Step 2: configure the SSO profile (interactive; user runs it)
```powershell
aws configure sso
```
Answer the prompts with the table above:
- SSO start URL: `https://d-9067687362.awsapps.com/start`
- SSO region: `us-east-1`
- SSO registration scopes: press Enter (accept default `sso:account:access`)
- A browser opens; **Allow** the `botocore-client` authorization
- Account `992382389570` and role `AutomationTeam` are usually auto-selected (only option)
- CLI default region: `us-east-1` · output: `json` (Enter)
- Profile name: e.g. `nexus`

This writes `~/.aws/config`. From here on, set `AWS_PROFILE` to that name (`$env:AWS_PROFILE = "nexus"`) or pass `--profile nexus` to `aws`.

### Step 3: log in and confirm the session
```powershell
$env:AWS_PROFILE = "nexus"
& "C:\Program Files\Amazon\AWSCLIV2\aws.exe" sso login
& "C:\Program Files\Amazon\AWSCLIV2\aws.exe" sts get-caller-identity   # should print the assumed AutomationTeam role
```
SSO tokens are short-lived. When `sync` later fails with `aws_fetch_failed` / credentials, just re-run `aws sso login`.

### Step 4: fetch the bundle
```powershell
$env:AWS_PROFILE = "nexus"
nexus-agent secrets sync
# or, if nexus-agent isn't built/on PATH:
dotnet run --project Bolt.Automation.AgentTools -- secrets sync
```
Expect `[secrets] bundle written to …\BoltAutomation\secrets\automation_nexus_secrets_store.json` and a `[secrets] set BOLT_SECRETS_PATH=…` line. Copy that path.

> The `nexus-agent` build must include `AWSSDK.SSO` + `AWSSDK.SSOOIDC` (alongside `AWSSDK.SecretsManager`) or SSO credential resolution dies with *"Assembly AWSSDK.SSOOIDC could not be found"*. They're referenced in `Bolt.Automation.AgentTools.csproj`; if you see that error, restore/build is stale. Rebuild.

### Step 5: export `BOLT_SECRETS_PATH` persistently (point at the dir, not the file)
- Windows (future shells):
  ```powershell
  setx BOLT_SECRETS_PATH "$env:LOCALAPPDATA\BoltAutomation\secrets"
  ```
  `setx` only affects *new* shells; reopen the terminal/IDE. For the current shell also run `$env:BOLT_SECRETS_PATH = "$env:LOCALAPPDATA\BoltAutomation\secrets"`.
- bash/Linux/macOS: add to `~/.bashrc` / `~/.zshrc`:
  ```bash
  export BOLT_SECRETS_PATH="$HOME/.local/share/BoltAutomation/secrets"
  ```

### Step 6: make sure the run resolves to your target environment
The provider flattens only the active env's `appSecrets` block, so the run must resolve to the env whose secrets you want. Setting `ASPNETCORE_ENVIRONMENT` is not always sufficient: `ConfigurationLoader` lets a local `*.runsettings` `Environment` parameter override it. Those runsettings are gitignored and per-developer (different names and contents on every machine), so there is no canonical file to point at; inspect *your own* setup. Don't trust the env var; trust what actually resolved (the next step's log line confirms it). A secret defined only for UAT will not resolve under QA, by design.

### Step 6b: running from an IDE (VS Code / Visual Studio Test Explorer), not a shell
Steps 5–6 set the env vars for shells. An IDE started *before* you set them keeps a stale environment. Its test runner (VS Code C# Dev Kit "Debug Test", Visual Studio Test Explorer) spawns a `testhost` that sees neither `BOLT_SECRETS_PATH` nor `ASPNETCORE_ENVIRONMENT`, so the app-secrets provider loads nothing and any secret-backed `IOptions` binding throws (classic symptom: `System.InvalidOperationException: 'LaunchDarkly SDK key is not configured.'` from `AddLaunchDarklyServices`, or Outlook/Mongo secrets null). Fixes, most robust first:

1. **Fully restart the IDE.** Quit completely, not "Reload Window". A freshly launched IDE inherits the persisted user env vars from Steps 5–6.
2. **Pin the env vars in the runsettings the IDE runner uses.** Add a `<RunConfiguration><EnvironmentVariables>` block (NOT `<TestRunParameters>`):
   ```xml
   <RunConfiguration>
     <EnvironmentVariables>
       <BOLT_SECRETS_PATH>C:\Users\<you>\AppData\Local\BoltAutomation\secrets</BOLT_SECRETS_PATH>
       <ASPNETCORE_ENVIRONMENT>Qa</ASPNETCORE_ENVIRONMENT>
     </EnvironmentVariables>
   </RunConfiguration>
   ```
   Why the distinction matters: `ConfigurationLoader` resolves the env *name* from a `<TestRunParameters>` `Environment` value or the `ASPNETCORE_ENVIRONMENT` process var, but it reads `BOLT_SECRETS_PATH` only from the process environment (`Environment.GetEnvironmentVariable`). So a `<TestRunParameters>` `Environment` param alone picks the env but does not carry the secrets path; only `<EnvironmentVariables>` (or an inherited shell env) delivers `BOLT_SECRETS_PATH` to the testhost.
   - VS Code C# Dev Kit: point `dotnet.unitTests.runSettingsPath` in `.vscode/settings.json` at that runsettings file.
   - F5 / launch.json debugging: put the same two vars in the config's `"env"` block instead (launch.json does not read runsettings).

### Step 7: verify (no API call, then a real test)
```powershell
nexus-agent secrets status   # bundleExists: true, recent syncedAt, positive ttlRemainingMinutes
```
Then run the DevOps verification tests. They have no DB dependency and directly assert the chain (provider loads, `appSecrets` block present, a known key resolves, browser launches):
```powershell
$env:BOLT_SECRETS_PATH="$env:LOCALAPPDATA\BoltAutomation\secrets"
# ensure the run resolves to your target env (Step 6) before running
dotnet test Bolt.Automation.Tests/Bolt.Automation.Tests.csproj `
  --filter "FullyQualifiedName~VerifyBoltSecrets|FullyQualifiedName~VerifyBrowserLaunchAndNavigation"
```
The provider logs `[BoltSecrets] Loading app secrets … for env '<X>'` at the top of the run. Confirm `<X>` is your target env, followed by `[BoltSecrets] Loaded N app-secret config keys` and `Passed`. If `<X>` is wrong, fix env resolution (Step 6). If a secret-dependent test still throws `Set BOLT_SECRETS_PATH…`, work the troubleshooting list below.

## Maintaining a working local version

- **Routine refresh.** Just run `nexus-agent secrets sync`. Inside the 12h TTL it's a free cache hit (`[secrets] cache hit (synced <age>), use --force to refresh`); past it, it refetches once.
- **Force a refresh** (after DevOps rotates a value or adds a new env's `appSecrets`): `nexus-agent secrets sync --force`.
- **Shorter or longer window:** `--ttl 1` (refetch hourly) or `--ttl 168` (weekly).
- **Different secret/region ad-hoc:** `--secret-id <id>` / `--region <r>` (e.g. to test against a staging copy of the bundle).
- **Check state anytime:** `nexus-agent secrets status` (no AWS call). Use it before assuming the cache is stale.

## Troubleshooting (secrets resolve to null / tests can't connect)

> **When a test fails on a credential, secret, or connection error** (e.g. `Set BOLT_SECRETS_PATH…`, a 401/auth failure, a DB-connection error, or app secrets resolving null): check the cache TTL first and refresh if stale. Run `nexus-agent secrets status` and note `ttlRemainingMinutes`/`syncedAt`. A lapsed TTL does not by itself break a test run (the readers read the on-disk bundle directly, ignoring the TTL), but an expired window is the signal that the cached bundle may be stale or rotated since you last synced. Refresh with `nexus-agent secrets sync --force` (re-run `aws sso login` first if the SSO token has expired, indicated by `aws_fetch_failed`), then re-run the test. If `status` shows `bundleExists: false`, sync is mandatory. Only after this, work the list below.

Work top-down:

1. **`BOLT_SECRETS_PATH` unset or wrong.** `secrets status` shows the dir it expects; confirm the env var points at that directory. If unset, `SecretsStore` silently falls back to SSPI and the app-secrets provider loads nothing.
2. **Cache missing or expired.** `bundleExists: false` or old `syncedAt` means run `nexus-agent secrets sync` (or `--force`).
3. **Wrong active environment.** The bundle has the key but the run resolved a different env. The `[BoltSecrets] … for env '<X>'` log line shows what `ConfigurationLoader` actually picked; if it isn't your target, a local (gitignored, per-dev) `*.runsettings` `Environment` parameter is overriding `ASPNETCORE_ENVIRONMENT`. Fix env resolution per Step 6. Tenant tests that "skip" with *"Not configured to run in <env>"* are this same issue, not a secrets problem.
4. **Bundle lacks the block.** If `environments[<env>].appSecrets`, `.userSecrets.<TENANT>.<Role>`, `.twilio.<TENANT>`, or a specific tenant's connection string is absent, that's a DevOps bundle-population gap, not a local issue. App secrets no-op with a warning; a non-SSPI DB connection throws a clear "Set BOLT_SECRETS_PATH…"; a missing user/Twilio credential surfaces as an empty API key, password, or auth header (401 or empty-header failure). Escalate to DevOps to add the block to the AWS secret.
5. **Malformed JSON.** The provider throws `InvalidOperationException` (fail-loud). Re-run `sync --force` to overwrite a truncated cache.
6. **AWS error on sync** (`aws_fetch_failed`):
   - *"Failed to resolve AWS credentials" / "Assembly AWSSDK.SSOOIDC could not be found"*: SSO token expired (re-run `aws sso login`) or the `nexus-agent` build is missing the SSO SDK packages (see the Step 4 note; rebuild).
   - *"Secrets Manager can't find the specified secret"*: wrong `SecretId`. It must be `automation_nexus_secrets_store` (an underscore name, not `automation/nexus/secrets-store`); check `BoltSecrets:SecretId` in appsettings or pass `--secret-id automation_nexus_secrets_store`.
   - Otherwise verify the `AutomationTeam` role can read the secret in `us-east-1`.
7. **Works from the shell but NOT when debugging in an IDE.** Symptom is a loud `InvalidOperationException: 'LaunchDarkly SDK key is not configured.'` (or any app-secret-backed `IOptions` resolving null) only under VS Code "Debug Test" / Visual Studio Test Explorer, while a `dotnet test` from your terminal passes. Root cause: the IDE was started before you set the env vars, so its `testhost` has a stale environment without `BOLT_SECRETS_PATH`. Confirm from the debug console: healthy shows `[BoltSecrets] Loaded N app-secret config keys for env '<X>'`; broken shows `[BoltSecrets] BOLT_SECRETS_PATH not set, skipping`. Fix per Step 6b (fully restart the IDE, and/or pin the vars in the runner's runsettings `<EnvironmentVariables>`, not `<TestRunParameters>`).

## Where each secret lives (and how to add one)

Every secret belongs in the bundle under `environments.<env>`, never in source. Pick the block by kind:

| Secret kind | Bundle path | Consumed via | Code home (structural only) |
|---|---|---|---|
| DB connection string | `tenants.<TENANT>.{Platform,Audit,Payment}SqlConnectionString` | `SecretsStore.GetDatabaseConnectionString` | `DatabaseConnectionsDataStore` (Server/Database) |
| App-wide secret (Outlook, Mongo, AWS, HMAC, LaunchDarkly, tenant BasicAuth) | `appSecrets.<Section>.<Key>` | `IOptions<T>` via `BoltSecretsConfigurationProvider` | the options class |
| User credential (password, ApiKey, OAuthToken, AgentIdentity, ApiSource) | `userSecrets.<TENANT>.<Role>.<Field>` | `TestContextAccessor.CurrentUserCollection` overlay | `UserDataStore` (Username/Role/Email/Sso/LoginUrl/…) |
| Twilio (AuthToken, AccountSid) | `twilio.<TENANT>` (or `twilio.<TENANT>:<subtenant>`) | `TestContextAccessor.CurrentTwilioData` overlay | `TwilioDataStore` (phone numbers) |

`<TENANT>` is uppercase (BOLTAG); `<Role>` is the `UserTestDataCollection` property name (ServiceAgent, Agent, …); `<env>` is lowercase (qa, uat, dev, staging, production).

**Adding a new secret, the loop:**

1. **Classify and place structural vs secret.** Structural fields (username, role, phone, URL, SSO issuer/audience, external ids) stay in the `*DataStore`; only the credential goes in the bundle. A brand-new leaf key also needs a model field on `UserSecretFields`/`TwilioSecretFields`/`TenantDatabaseSecrets` (or a new app-secrets options class). A key with no matching model field binds to nothing.
2. **Add it to your local copy** at `$BOLT_SECRETS_PATH/automation_nexus_secrets_store.json` under the right `environments.<env>` block. Keep valid JSON (`Get-Content … | ConvertFrom-Json` to sanity-check).
3. **Test locally.** Run the affected test with `BOLT_SECRETS_PATH` set (and the env pinned, e.g. `ASPNETCORE_ENVIRONMENT=QA`). The `DevOpsVerificationTests` (`VerifyBoltSecretsAppSecretsBlock`, `VerifyBoltSecretsUserAndTwilioBlock`) resolve the real bundle with no DB/browser dependency, a fast smoke test that a new block reads back.
4. **Push it to the durable store, or it's lost.** The local copy is a cache. The next `nexus-agent secrets sync --force` overwrites it from AWS Secrets Manager (`automation_nexus_secrets_store`), and CI only ever sees the AWS/mounted copy. Remind the user (and coordinate with DevOps) to add the same node to the shared secret store. This tool is read-only and cannot write it back. Until DevOps updates the AWS secret, the new value exists only on your machine.
5. **Rotate anything that was ever in git.** If the value you're moving previously lived in source (and therefore git history), it must be rotated with the owning platform account as part of the change. Relocating an exposed credential is cosmetic without rotation.

## Guardrails

- **Read-only access: no edit, no delete.** Secrets are only ever *read*. The tool performs exactly one AWS operation, `GetSecretValueAsync`, and never creates, edits, deletes, tags, or writes back to a secret. The `AutomationTeam` permission set must be scoped read-only: allow `secretsmanager:GetSecretValue` (+ `DescribeSecret`/`ListSecrets`), deny/omit `PutSecretValue`/`UpdateSecret`/`DeleteSecret`/`CreateSecret`/`RestoreSecret`/`PutResourcePolicy`. The only writes this flow makes are to the repo-external local cache dir. Rotating and populating bundle contents is DevOps-owned and happens in the AWS console, never through this tool.
- **No secret is ever logged.** Provider, `sync`, and `status` log only non-sensitive metadata: paths, env names, key *counts*, ages, exit codes. Never a secret value or the bundle payload. Never paste secret values into chat, PRs, logs, or the console. If you add diagnostics anywhere on the secrets path, redact values with `[REDACTED]` (repo-wide rule in CLAUDE.md). Do not echo the bundle file contents.
- Never copy the bundle into a repo or hardcode region/secret-id beyond the non-secret `BoltSecrets` appsettings section. The cache dir is intentionally outside the repo; `**/automation_nexus_secrets_store.json` is also gitignored as defense-in-depth.
- The bundle file name must stay exactly `automation_nexus_secrets_store.json`. Both readers and the CI mount depend on it.
- This skill consumes the bundle; it does not author it. Changing the bundle's contents or schema (new keys, new env) is a DevOps + provider change. Coordinate with DevOps, who own the AWS secret.
