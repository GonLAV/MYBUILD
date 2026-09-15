# Bolt.Automation.Common — CLAUDE.md

Shared primitives: `IScopeContext`, the logging stack, enums (`Tenant`, `Environment`, `FrontEndType`), URL/user models, `TrxParser`, `FileNameUtils`.

## IScopeContext

Central per-test context — tenant, environment, typed `Data`, and ad-hoc test values:

```csharp
ScopeContext.Set(ctx => ctx.Tenant, Tenant.USAA);
ScopeContext.Set(ctx => ctx.CurrentUser, user);
ScopeContext.SetTestValue("ApiKey", key);

var tenant = ScopeContext.Get(ctx => ctx.Tenant);
```

## Logging

Use `IAutomationLogger` for structured, step-scoped logging:

```csharp
await _logger.ExecuteStepAsync("Execute Test Action", async () =>
{
    _logger.Info("Starting operation");
    _logger.LogUiAction("Click", "Button", "description");
    _logger.LogBusinessRule("RuleName", passed, "details");
    _logger.LogDataValidation("FieldName", passed, expected, actual, "context");
});
```

- Logging lives in **implementation classes** (page objects, API clients, helpers) — not test methods. See `philosophy/logging-where-work-happens.md`.
- **Never log secrets/PII** — use `[REDACTED]`. Full rules: `Documentation/AI-Agent-Logging-Instructions.md`.

## Configuration

- `appsettings.json` (base) + `appsettings.{Environment}.json` (QA / UAT / Development / ...).
- Environment resolves from the `Environment` / `ASPNETCORE_ENVIRONMENT` variable.
- **Framework code must not read environment variables directly** — go through configuration/options so values stay testable and overridable.

## Conventions

- **Primary constructors** for new classes (C# 14 / .NET 10).
- Tenants: BOLTAG, UNIFY, PROGRESSIVEPL, USAA, COMPARION, LIBERTYX, KRAFTLAKEX, BOLTACCESS.

Deeper: `kb lookup --topic framework:overview`.
