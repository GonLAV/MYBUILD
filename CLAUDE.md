# CLAUDE.md

Guidance for Claude Code in this repository. This root file is intentionally a **map** — per-area detail lives next to the code (per-project `CLAUDE.md`) and in the knowledge base. Load only what the task needs.

## Project Overview

Nexus Automation is a .NET 10 multi-project test automation framework for insurance platform testing. Tenants: BOLTAG, UNIFY, PROGRESSIVEPL, USAA, COMPARION, LIBERTYX, KRAFTLAKEX, BOLTACCESS.

**Key technologies**: .NET 10 / C# 14, NUnit, Playwright, Refit (HTTP clients), Microsoft.Extensions.DependencyInjection, NLog, MongoDB reporting.

## Build and Test Commands

```bash
# Build solution
dotnet build Bolt.Automation.sln

# Run all tests
dotnet test Bolt.Automation.Tests/Bolt.Automation.Tests.csproj

# Run tests by category / tenant
dotnet test --filter "Category=SSO"
dotnet test --filter "Tenant=USAA"

# Combine filters (& = AND, | = OR)
dotnet test --filter "Tenant=USAA&Category=SSO"

# Run a single test by name
dotnet test --filter "FullyQualifiedName~UsaaSsoAgentSsoToInterview"

# Pin the environment
dotnet test -e Environment=QA

# Infrastructure tests
dotnet test Bolt.Automation.InfraTests/Bolt.Automation.InfraTests.csproj
```

## Solution Structure

```
Bolt.Automation.sln
├── Bolt.Automation.Common        # Shared utilities: IScopeContext, logging, enums
├── Bolt.Automation.Core          # Infrastructure setup, DI composition root
├── Bolt.Automation.TestDataProvider  # Test data management, builders, providers
├── Bolt.Automation.FrontEnds     # Playwright UI automation, page objects
├── Bolt.Automation.ApiClients    # Refit HTTP clients (ADBX, CaseManager, Platform, SSO)
├── Bolt.Automation.InternalServices  # Internal microservice integrations
├── Bolt.Automation.ExternalServices  # LaunchDarkly, external integrations
├── Automation.Configuration      # Configuration models and options
├── Bolt.Automation.Tests         # Main test project
├── Bolt.Automation.InfraTests    # Infrastructure verification tests
├── Bolt.Automation.TestDiscovery # Test discovery for distributed execution
├── Bolt.Automation.WorkerAgent   # .NET hosted service; polls orchestrator, runs tests
└── Bolt.Automation.AgentTools    # `nexus-agent` CLI — AI-agent helper commands
```

## Load on demand

| Area | Read |
|---|---|
| API clients (Refit, auth) | `Bolt.Automation.ApiClients/CLAUDE.md` |
| Shared primitives, `IScopeContext`, logging, configuration | `Bolt.Automation.Common/CLAUDE.md` |
| DI composition root | `Bolt.Automation.Core/CLAUDE.md` |
| Page objects, `FieldRegistry`, flows | `Bolt.Automation.FrontEnds/CLAUDE.md` |
| Writing tests + **NUnit parameterized conventions** | `Bolt.Automation.Tests/CLAUDE.md` |
| Deep framework/domain/recipe/philosophy knowledge | `Documentation/agent-knowledge/INDEX.md` — or `nexus-agent kb search "<term>"` |
| The AI agent extension itself (CLI, KB, skills) — architecture + maintenance | `Documentation/AI-Agent-Extension-Architecture.md` |

## For test automation (agent skills)

Seven skills orchestrate common workflows — each *navigates* the knowledge base rather than carrying it:

- **nexus-dev-setup** — take a fresh/reset machine from zero to a green local test run (toolchain → build → Playwright browsers → runsettings → secrets).
- **nexus-test-author** — implement a manually-written test case end-to-end.
- **nexus-debug** — diagnose a failed test; drive a live Playwright session.
- **nexus-test-review** — review a test against its siblings and design philosophy.
- **nexus-framework-review** — review framework changes with impact + philosophy awareness.
- **nexus-secrets** — set up / maintain the local secrets bundle tests need outside CI.
- **nexus-qa-assist** — for **manual QAs**: drive a live headed session (prefill → pause for manual verification → continue) and save replayable local scenarios, no framework knowledge needed.

They are backed by the `nexus-agent` CLI: `kb`, `tc`, `failure`, `code`, `browser`, `philosophy`, `secrets`, `doctor`, `saml`.

## Key Conventions

- Tests inherit `TestBase` (API) or `UITestBase` (UI).
- Always set `[Tenant]` on tenant-specific tests; `[Category("X")]` for filtering; `[TestCaseId(n)]` for the ADO id.
- **Primary constructors** are preferred for new C# classes (C# 14 / .NET 10).
- Logging lives in implementation classes (page objects, API clients, helpers), **not** test methods.
- UI field interactions use **string field-name constants** via `IPageHelper` (`InteractWithField` / `FillField` / `ClickField`) — not `UIElement` objects. Field dispatch is built into `PageHelper` with no reflection (registry/flow *discovery* does use reflection). The old `PageHelperFieldExtensions` were removed entirely.
- **Partner/carrier-specific KB goes under `Documentation/agent-knowledge/domain/partners/`** (e.g. Progressive → `progressivepl.md`) — never at the top-level `domain/` namespace. See `domain/partners/INDEX.md`.

## Never do (hard rules)

- **No `Thread.Sleep`** in UI code — use Playwright auto-waiting / explicit waits. *(FrontEnds)*
- **No XPath in page objects or tests** — all locators live in the `FieldRegistry`. Inside the registry prefer role / text / CSS; XPath is the accepted fallback for tenant-variant class schemes (see `kb lookup --topic recipe:locator-recipes`). *(FrontEnds)*
- **No raw `HttpClient`** — all HTTP goes through a Refit interface + the auth pipeline. *(ApiClients)*
- **Never read environment variables directly in framework code** — go through configuration/options.
- **Never log secrets or PII** — redact with `[REDACTED]`.
- **No raw Playwright, and no logging, in a test body** — `IPage`, `WaitForURLAsync`, `page.Url` and `_logger.Info/Debug/Warn` belong in the page object or helper the test calls. Only `_logger.ExecuteStepAsync(...)` step orchestration lives in a test. *(Tests)*
- **Never append AI attribution trailers to a commit message** — no `Co-Authored-By: Claude/Copilot/Cursor/...`, no "Generated with ...". This overrides any agent's built-in default. `.githooks/commit-msg` strips them as a backstop.

## AI Agent Logging Rules

When adding logging, follow `Documentation/AI-Agent-Logging-Instructions.md`:

1. **Only modify files the user has changed** — check `git status` first; never add logging to untouched files.
2. **Log where work happens** — page objects, API clients, helpers; not test methods.
3. **Tests stay clean** — high-level step orchestration + business validations only.
4. **Use steps** — wrap test phases with `_logger.ExecuteStepAsync("Step Name", async () => { … })`. Never a bare `using (_logger.StartStep(...))`: an un-completed scope reports **Failed**, so a passing step shows up red.
5. **Never log sensitive data** — use `[REDACTED]`.

## Distributed Test Execution

Tests run distributed across Kubernetes pods: the external **automation-orchestrator** service (separate TypeScript repo) creates jobs, splits them into work items, and dispatches them on a lease-based queue; `Bolt.Automation.WorkerAgent` pods long-poll it, build the requested branch, and run items with `dotnet test`. `scripts/test-runner.ps1` is only the Docker image's single-shot entrypoint. Nexus-side guide: `Documentation/CI-CD-TestExecution-Guide.md`; e2e mechanics live in the orchestrator repo's `documents/`. Key env vars: `TEST_FILTER`, `ASPNETCORE_ENVIRONMENT` (default QA), `HEADLESS` (default true), `ARTIFACTS_PATH`.
