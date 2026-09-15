# Bolt.Automation.ApiClients — CLAUDE.md

Refit-based HTTP clients for platform services (ADBX, CaseManager, Platform, SSO, GetQuote).

## Pattern

Clients are **Refit interfaces** resolved through `RefitApiServiceLocator`:

```csharp
var locator = _scope.ServiceProvider.GetRequiredService<RefitApiServiceLocator>();
var api = locator.GetService<IGetQuoteApi>();
```

Validate responses with `.EnsureSuccessContent()` — don't hand-check status codes.

## Auth

Clients flow through the platform auth pipeline (token acquisition + header injection) wired in DI. A new client registers alongside the existing ones via the project's `Add*` extension; it does **not** manage its own auth.

## Never do

- **No raw `HttpClient`.** All HTTP goes through a Refit interface + the auth pipeline. A hand-rolled client bypasses auth, logging, and retry.
- **No secrets in code or logs.** Tokens/keys come from configuration; redact with `[REDACTED]`.

## Deeper context

- `kb lookup --topic domain:tc-api` — the nexus-logger Azure DevOps TC API (a separate user-JWT service; the agent CLI's `tc` commands wrap it, not this pipeline).
