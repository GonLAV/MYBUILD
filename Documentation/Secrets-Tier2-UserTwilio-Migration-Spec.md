# Secrets Tier-2 — `UserDataStore` & `TwilioDataStore` Migration Spec

**Status:** Implemented (commit `604ac68a`) — kept as the historical design record
**Follows:** Tier-1 (PR 79990 / commit `2be2a677` — "Nexus security cleanup"), which migrated app secrets → `appSecrets` and DB connection strings → `tenants`.
**Owner:** _TBD_
**Related work item:** _TBD_ (sibling to #246002)

---

## 1. Problem

The Tier-1 security cleanup relocated app-wide secrets and DB connection strings into the AWS Secrets Manager bundle (`BOLT_SECRETS_PATH`). It did **not** touch two static test-data stores, which still hold **live credentials hardcoded in source** (and therefore in git history):

| Store | File | Secret fields | Live values today |
|---|---|---|---|
| `UserDataStore` | `Bolt.Automation.TestDataProvider/Repositories/DataStores/UserDataStore.cs` | `Password`, `ApiKey`, `OAuthToken`, `AgentIdentity`, `ApiSource` | ~60+ real API keys, test-account passwords, and base64 basic-auth credentials across all 8 tenants × up to 5 environments |
| `TwilioDataStore` | `Bolt.Automation.TestDataProvider/Repositories/DataStores/TwilioDataStore.cs` | `AuthToken`, `AccountSid` | 2 live (BOLTAG Qa/Uat); UNIFY/`marketslib` entries are `<placeholder>` templates |

Neither file appears in the Tier-1 commit — they are the untouched leftovers.

---

## 2. Why these differ from Tier-1 (configuration-loader) secrets

Tier-1 secrets are **app-wide, per-environment** singletons: `environments.<env>.appSecrets` is flattened into `IConfiguration` by `BoltSecretsConfigurationProvider` and consumed via `IOptions<...>`.

These two stores are **test data keyed by `(Tenant, Environment)`** (Twilio adds `subtenant`), and each entry is a **rich object mixing secret and non-secret fields**. Structural fields — `Username`, `Email`, `Role`, `LoginUrl`, `Source`, `Id`, `WorkSpaceGroupId`, `UserExternalId`, `GroupExternalId`, `Subtenant`, `Sso.Issuer`, `Sso.Audience`, phone numbers — are **not** secrets and must stay in code.

So the migration is **not** "move the block into `appSecrets`." It is: **split each record** — structural shape stays in code, credential fields move to the bundle keyed by tenant + environment + role. This is exactly what the Tier-1 **DB migration already did** for `DatabaseConnectionInfo` (kept `Server`/`Database`, sourced the password/connection string from the bundle at runtime).

### Field classification

**`UserTestData`** (`Bolt.Automation.Common/Models/Users/UserTestData.cs`) — mutable `class`:

- **Secret (move to bundle):** `Password`, `ApiKey`, `OAuthToken`, `AgentIdentity`, `ApiSource`.
- **Structural (keep in code):** `Id`, `Username`, `FirstName`, `LastName`, `UserExternalId`, `GroupExternalId`, `WorkSpaceGroupId`, `Role`, `Email`, `Phone`, `IsActive`, `SendAgentIdentity`, `Subtenant`, `LoginUrl`, `Source`, `Permissions`, `Attributes`, `Sso` (Issuer/Audience).

> Note: `AgentIdentity`, `ApiSource`, and `OAuthToken` are base64-encoded credential material — treat as secrets, not identifiers.

**`TwilioTestData`** (`Bolt.Automation.Common/Models/Twilio/TwilioTestData.cs`) — immutable `record` (`required init`):

- **Secret (move to bundle):** `AuthToken`, `AccountSid`.
- **Structural (keep in code):** `ToPhoneNumber`, `FromPhoneNumber`.

---

## 3. Lifecycle — the single seam

Both stores are static `readonly` dictionaries loaded **once per process**, shared across all (parallel) tests. They are read in **exactly one place**:

```
TestContextAccessor.CurrentUserCollection  (TestContextAccessor.cs:46)   → UserDataStore.All[(tenant, env)]
TestContextAccessor.CurrentTwilioData      (TestContextAccessor.cs:102)  → TwilioDataStore.Get(tenant, env, subtenant)
```

`TestContextAccessor` is registered scoped (`TestDataProviderServiceExtensions.cs:11`) and resolved by `TestBase` (`TestBase.cs:72`). The canonical pattern in every test:

```csharp
var user = TestContextAccessor.CurrentUserCollection.<Role>;  // reads UserDataStore
ScopeContext.Set(ctx => ctx.CurrentUser, user);               // stashes UserTestData (incl. secrets) on the scope
```

The credentials are consumed **downstream off `ScopeContext`** by the auth pipeline, never re-read from the store:

- `GetQuoteApiKeyHandler` → `X-Api-Key` / `X-Agent-Identity` (gated on `SendAgentIdentity`) / `X-API-Source`, and `OAuthToken` for PROGRESSIVEPL bearer exchange.
- `PlatformApiHandler` → `X-Bolt-ApiKey`; PROGRESSIVEPL `/auth` Basic exchange via `OAuthToken`.
- `CaseManagerApiKeyHandler`, `CasePortalApiClientFactory` → `X-Api-Key` / `X-API-Key`.
- `STS_LoginPage` (Playwright), `AdbxApiHandler`, `PartnerPortalApiHandler` → `Password`.
- Twilio: `TwilioSignatureHandler` reads `ScopeContext.Data.TwilioData.AuthToken` → `X-Twilio-Signature`.

**Implication:** hydrating secrets at those **two getters** reaches all ~60 downstream call sites and every handler with **zero changes** to tests or handlers.

---

## 4. Design — dedicated typed reader (mirrors the DB precedent)

Chosen over the "reuse `appSecrets`/`IConfiguration`" alternative to keep tenant/env/role addressing strongly typed and avoid polluting `IConfiguration` with dozens of per-tenant keys.

### 4.1 Bundle schema

Add per-environment sub-trees **parallel to** `tenants` (which stays DB-only):

```jsonc
"environments": {
  "qa": {
    "tenants":     { "BOLTAG": { "PlatformSqlConnectionString": "..." } },   // existing (DB)
    "appSecrets":  { "OutlookClient": { "ClientSecret": "..." }, "...": {} }, // existing (Tier-1)

    "userSecrets": {                                                         // NEW
      "BOLTAG": {
        "ServiceAgent":      { "ApiKey": "...", "AgentIdentity": "..." },
        "Agent":             { "Password": "..." },
        "ConsumerOrganicPL": { "ApiKey": "..." }
      }
    },
    "twilio": {                                                             // NEW
      "BOLTAG": { "AuthToken": "...", "AccountSid": "..." }
      // subtenant nesting where a (tenant, subtenant) pair diverges, e.g. UNIFY/marketslib
    }
  }
}
```

- `userSecrets` key path: `TENANT` (uppercase) → collection-property name (`ServiceAgent`, `Agent`, `ConsumerOrganicPL`, …) → secret field. This mirrors how `UserDataStore.All` / `UserTestDataCollection` are already addressed.
- Environment keys resolved via the shared `SecretsBundle.ResolveEnvironmentKey` so they never drift from the DB/appSecrets readers.

### 4.2 Reader

- Extend `AutomationSecretsStore` / `EnvironmentSecrets` (`Bolt.Automation.Common/Models/Secrets/DatabaseSecrets.cs`) with `UserSecrets` and `Twilio` nodes.
- Add typed getters to `SecretsStore` / `ISecretsStore`, e.g.:
  - `IReadOnlyDictionary<string, UserSecretFields>? GetUserSecrets(Tenant tenant, Environment env)` (keyed by role/property name), or a per-field `GetUserSecret(tenant, env, roleKey, field)`.
  - `TwilioSecretFields? GetTwilioSecrets(Tenant tenant, Environment env, string subtenant)`.
- Reuse `SecretsBundle.ResolveEnvironmentKey` and the uppercase tenant key convention already used by `GetDatabaseConnectionString`.

### 4.3 Hydration at the seam

In `TestContextAccessor` (the only consumers):

- **`CurrentUserCollection`** — return a **hydrated clone** of the collection, not the shared static. `UserTestData` is a mutable class shared once per process; mutating it in place would race under NUnit parallel execution and leak values across tenants/envs. For each populated user, overlay `Password/ApiKey/OAuthToken/AgentIdentity/ApiSource` from the bundle when `SecretsStore.Instance.IsLoaded`; otherwise fall back to the in-code value (empty after strip → clear thrown error).
- **`CurrentTwilioData`** — `TwilioTestData` is a record; overlay via `with { AuthToken = ..., AccountSid = ... }`.

**Precedent to copy:** `DatabaseTestDataCollection` indexer (`Bolt.Automation.Common/Models/Database/DatabaseTestDataCollection.cs:41-63`) — structural shape in code, secret layered in at read-time from `SecretsStore.Instance` when loaded, clear fallback otherwise.

### 4.4 Strip source + populate AWS

- Remove the credential values from both stores; keep structural fields. Delete the `<placeholder>` Twilio rows.
- Throw a clear, actionable error when a required credential is absent and the bundle is not loaded (mirror `DatabaseConnectionInfo.ToConnectionString`'s "Set BOLT_SECRETS_PATH…" message).
- Author the `userSecrets` / `twilio` JSON for every tenant×env combination and load it into the AWS Secrets Manager secret that `nexus-agent secrets sync` already fetches. **No sync-tooling change required** — `SyncCommand` pulls the whole bundle.

---

## 5. 🔴 Critical prerequisite — rotation

Every credential in these stores is **already in git history**. Moving them to AWS is cosmetic unless **each exposed value is rotated**: all API keys, test-account passwords, base64 basic-auth (`AgentIdentity` / `ApiSource` / `OAuthToken`), and the Twilio `AuthToken` / `AccountSid`. Rotation must be coordinated with the owners of those platform accounts and completed **as part of** this work — not deferred.

---

## 6. Local-dev / CI impact

DB tests survive without a bundle on Windows via SSPI. **User and API-key credentials have no such fallback** — after this migration, running the affected API/UI tests locally will **require** `BOLT_SECRETS_PATH` to be set (via `nexus-agent secrets sync`). This widens the set of tests that hard-depend on the bundle; update the `nexus-secrets` skill accordingly. CI already mounts the bundle (`worker.Dockerfile` sets `BOLT_SECRETS_PATH=/mnt/bolt-secrets`), so no CI change beyond adding the new nodes to the secret payload.

---

## 7. Task breakdown

1. **Schema + model** — add `UserSecrets` and `Twilio` nodes to `AutomationSecretsStore` / `EnvironmentSecrets` (`DatabaseSecrets.cs`).
2. **Reader** — typed getters on `SecretsStore` / `ISecretsStore` for user + Twilio secrets (reuse `SecretsBundle` env/tenant key helpers).
3. **Hydration** — overlay secrets in `TestContextAccessor.CurrentUserCollection` (clone) and `CurrentTwilioData` (`with`), with fallback/throw.
4. **Strip source** — remove credentials from `UserDataStore` / `TwilioDataStore`; drop Twilio placeholders.
5. **Author bundle JSON** — `userSecrets` / `twilio` for all tenant×env; load into the AWS secret.
6. **Rotate** — every exposed credential (§5), coordinated with account owners.
7. **Tests + docs** — extend `BoltSecretsProviderTests` / `DevOpsVerificationTests` with user/Twilio fixtures + env-isolation assertions; update the `nexus-secrets` skill's "app secrets" list to include user/Twilio credentials and the widened local-run requirement.

---

## 8. Acceptance criteria

- No live credential remains in `UserDataStore.cs` or `TwilioDataStore.cs`; only structural fields.
- With the bundle mounted, all affected API/UI/Twilio tests resolve credentials and pass in QA and UAT.
- Without the bundle, affected tests fail with a clear "Set BOLT_SECRETS_PATH" message (not a null-ref / empty-header silent failure).
- Env isolation holds — a UAT credential never resolves under QA (assert, per the Tier-1 pattern).
- The `tenants` (DB) subtree and `appSecrets` continue to resolve unchanged.
- Every previously-exposed credential has been rotated.
