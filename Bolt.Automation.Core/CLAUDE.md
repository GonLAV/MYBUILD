# Bolt.Automation.Core — CLAUDE.md

Infrastructure setup + the DI composition root.

## DI flow

1. `TestInfrastructure.GetConfiguration(params)` loads configuration and resolves the `Environment`.
2. `TestInfrastructure.CreateServiceProvider(config, env, configure?)` builds the service provider.
3. `AddInfrastructureServices` (this project) orchestrates registration by calling each project's own extension (`AddApiClients`, `AddFrontEndServices`, …).
4. A scoped provider is created per test (`TestBase`) for isolation.

## Boundaries

- Core is the **composition root** — it wires every project together. Keep registration modular: each consumed project exposes its own `Add*` extension and Core calls them.
- Config loading + DI composition only. No domain logic here.

Deeper: `kb lookup --topic framework:overview`.
