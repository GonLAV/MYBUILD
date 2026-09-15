# Distributed Test Execution Guide

How nexus tests execute at scale. The system has two halves:

- **This repo (nexus)** — the tests, the `Bolt.Automation.WorkerAgent` that runs them, and the Docker image they run in.
- **The orchestrator repo (`automation-orchestrator`)** — the TypeScript service that creates jobs, splits them into work items, dispatches work to workers, and aggregates results/analytics.

This guide covers the nexus side. For the end-to-end mechanics — job creation paths, the dispatch state machine, lease/heartbeat/reaper semantics, build coordination — the orchestrator repo's docs are canonical:

| Topic | Orchestrator repo doc |
|---|---|
| Full dispatch state machine (jobs, work items, leases, drain, zombie recovery) | `documents/DISPATCH_STATE_MACHINE.md` |
| Worker agent component overview | `documents/WORKER_AGENT.md` |
| Every env var, incl. per-state fan-out (`INJECTED_PS_STATE`) | `documents/ENVIRONMENT_VARIABLES.md` |
| MongoDB collections | `documents/MONGODB.md` |
| MCP server (`nexus-logger`) | `documents/MCP_GUIDE.md` |

## Execution model

1. A **job** is created in the orchestrator (manually via the UI/API, from a template, via an Azure DevOps webhook, or on a schedule).
2. The orchestrator splits the job into **work items** (per test, per category, or per-state fan-out) and exposes them on a lease-based work queue (`GET /api/work`, 10-minute visibility timeout).
3. **Workers** — Kubernetes pods running `Bolt.Automation.WorkerAgent` — long-poll that queue, check out the job's branch/commit, build once per branch change, run each leased item with `dotnet test`, and submit TRX-parsed results back.
4. Results aggregate into MongoDB (see [MongoDB-Logging-Structure.md](./MongoDB-Logging-Structure.md)); run summaries carry the `orchestratorJobId` linking both systems.

There are no pipeline YAML files in this repo — jobs are created in the orchestrator, not by Azure Pipelines checking out this repo per run. (An ADO webhook path exists for PR-triggered runs; it also just creates an orchestrator job.)

## The worker agent (`Bolt.Automation.WorkerAgent/`)

.NET Generic Host with three `BackgroundService`s plus git/build/test services:

| Service | Responsibility |
|---|---|
| `WorkerService` | Register with the orchestrator, spawn `WORKER_CONCURRENCY` work loops, graceful drain, deregister on shutdown |
| `HeartbeatService` | Periodic heartbeat, extends leases on active items, handles orchestrator-initiated drain |
| `LogForwarderService` | Batches NLog output and POSTs it to the orchestrator |
| `GitService` / `BuildService` | Checkout + `dotnet build` with timeouts and process-kill safety; build is coordinated so only one loop builds per branch/commit change |
| `TestExecutor` | `dotnet test` per work item, TRX parsing, artifact/results cleanup |

Health + Prometheus metrics are served on `HEALTH_PORT` (default 8080).

### Worker env vars (bound in `WorkerOptions.FromEnvironment`)

Core: `ORCHESTRATOR_URL`, `API_KEY`, `WORKER_ID`, `WORKER_CONCURRENCY`, `REPO_ROOT_PATH`, `SOLUTION_PATH`, `TEST_PROJECT_PATH`, `RESULTS_DIRECTORY`.
Timeouts/tuning: `BUILD_TIMEOUT_SECONDS`, `TEST_TIMEOUT_SECONDS`, `GIT_TIMEOUT_SECONDS`, `HEARTBEAT_INTERVAL`, `LOG_BATCH_INTERVAL_MS`, `LOG_BATCH_SIZE`, `LOG_LEVEL`.
Identity/misc: `POD_NAME`, `NODE_NAME`, `WORKER_VERSION`, `AZURE_DEVOPS_PAT`.

Per-item variables (e.g. `ORCHESTRATOR_WORK_ITEM_ID`, `ENVIRONMENT`, `TEST_FILTER`, injected `INJECTED_*` fan-out vars) arrive with each leased work item and are applied to the test subprocess — see the orchestrator repo's `ENVIRONMENT_VARIABLES.md`.

Kubernetes deployment manifests are owned by DevOps, not tracked in either repo.

## Test-side configuration

Tests read configuration through the standard hierarchy (see [Configuration.md](./Configuration.md)). The variables that matter in container/CI context:

| Variable | Default | Purpose |
|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | `QA` | Target environment (QA/UAT/...) |
| `HEADLESS` | `true` | Playwright headless mode |
| `TEST_FILTER` | — | NUnit filter expression passed to `dotnet test` |
| `ARTIFACTS_PATH` | `/app/artifacts` | Screenshots, DOM snapshots, TRX output |
| `BOLT_SECRETS_PATH` | — | Secrets bundle location (CI mounts it; locally see the `nexus-secrets` skill) |

## Docker image

`Dockerfile` (repo root) builds from `mcr.microsoft.com/dotnet/sdk:10.0`, restores and builds the solution **inside the image**, installs Playwright browsers, and sets `scripts/test-runner.ps1` as the entrypoint. That script reads the env vars above and runs a single `dotnet test` pass — it is the container's single-shot mode, used for standalone containerized runs; the worker-agent path does not go through it. See [Docker-Deployment-Guide.md](./Docker-Deployment-Guide.md).

`scripts/start-parallel-tests.ps1` runs several `dotnet test` processes with different filters inside one pod/machine (PowerShell jobs) — a utility for coarse parallelism without the orchestrator.

## Running tests directly

Local and ad-hoc CI execution is plain `dotnet test` — the command reference lives in the root [CLAUDE.md](../CLAUDE.md#build-and-test-commands). Examples:

```bash
dotnet test Bolt.Automation.Tests/Bolt.Automation.Tests.csproj --filter "Tenant=USAA&Category=SSO"
dotnet test -e Environment=QA --filter "FullyQualifiedName~UsaaSsoAgentSsoToInterview"
```

## Reporting

Runs and per-test details land in MongoDB via the Mongo reporting layer ([MongoReporting-Implementation.md](./MongoReporting-Implementation.md)); artifacts (screenshots, DOM snapshots) upload to S3. The orchestrator UI and the `nexus-logger` MCP server (runs, failures, flaky tests, environment health) sit on top of the same data.
