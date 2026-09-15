# Docker Test Execution Guide

## Overview

The repo-root `Dockerfile` produces a self-contained test image: it restores and builds the solution **inside the image** and installs Playwright Chrome, so a container can run tests with no host toolchain. The image's entrypoint is `scripts/test-runner.ps1`, which runs a single-shot `dotnet test` pass driven entirely by environment variables.

This is the *standalone* container mode. Distributed execution (orchestrator + `Bolt.Automation.WorkerAgent` pods) is a different path — see [CI-CD-TestExecution-Guide.md](./CI-CD-TestExecution-Guide.md).

## Quick start

```bash
# Build the image (solution builds inside the image; needs access to the Bolt NuGet feed)
docker build -t nexus-tests .

# Run with defaults (QA, headless, all tests)
docker run --rm -v "$(pwd)/artifacts:/app/artifacts" nexus-tests

# Run a filtered set against a specific environment
docker run --rm \
  -e ASPNETCORE_ENVIRONMENT=UAT \
  -e TEST_FILTER="Tenant=USAA&Category=SSO" \
  -v "$(pwd)/artifacts:/app/artifacts" \
  nexus-tests
```

## How the image is built

From the actual `Dockerfile`:

1. Base image `mcr.microsoft.com/dotnet/sdk:10.0` + the Chromium runtime libraries.
2. NuGet sources added: nuget.org + the Bolt feed (`boltnuget.boltqa.com`); `dotnet restore` against `NuGet.config`.
3. `dotnet build Bolt.Automation.Tests -c Release` inside the image.
4. Playwright Chrome installed via the built project's `playwright.ps1 install --with-deps chrome`.
5. Defaults baked in: `ASPNETCORE_ENVIRONMENT=QA`, `HEADLESS=true`, `ARTIFACTS_PATH=/app/artifacts`, `CI=1`.
6. `ENTRYPOINT` = `pwsh /app/scripts/test-runner.ps1`.

There are no separate build/run wrapper scripts — plain `docker build` / `docker run` is the interface.

## Runtime flow (`scripts/test-runner.ps1`)

- Reads `ASPNETCORE_ENVIRONMENT` (default `QA`), `HEADLESS` (default `true`), `TEST_FILTER`, `ARTIFACTS_PATH` (default `/app/artifacts`), `LOGLEVEL` (default `Warning`), `CONSOLE_VERBOSITY` (default `minimal`).
- **Single filter (or none)** → one `dotnet test -c Release --no-build` run, TRX + console loggers, results into `ARTIFACTS_PATH`.
- **Comma-separated `TEST_FILTER`** (e.g. `"Sanity,API,D2C"`) → parallel mode: `scripts/start-parallel-tests.ps1` runs one `dotnet test` process per filter concurrently (PowerShell jobs), each with its own artifacts subdirectory.

## Environment variables

| Variable | Default | Purpose |
|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | `QA` | Target environment |
| `HEADLESS` | `true` | Playwright headless mode |
| `TEST_FILTER` | — (all tests) | NUnit filter; commas trigger parallel mode |
| `ARTIFACTS_PATH` | `/app/artifacts` | TRX, screenshots, DOM snapshots |
| `LOGLEVEL` | `Warning` | NLog console level inside the container |
| `CONSOLE_VERBOSITY` | `minimal` | `dotnet test` console logger verbosity |
| `BOLT_SECRETS_PATH` | — | Mount + point at the secrets bundle if tests need secrets |

Test filter syntax is the standard `dotnet test --filter` grammar — examples in the root [CLAUDE.md](../CLAUDE.md#build-and-test-commands).

## Results

Mount a host directory over `/app/artifacts` to collect:
- `test-results-<timestamp>.trx` — TRX result file(s)
- screenshots / DOM snapshots captured on failure

MongoDB reporting (if configured/reachable) records runs the same way as any other execution — see [MongoReporting-Implementation.md](./MongoReporting-Implementation.md).

## Network diagnostics

`test-docker-network.ps1` (repo root) checks that target services are reachable from inside a container — use it when tests fail with connection errors that don't reproduce on the host (DNS, VPN-only endpoints, Docker network mode).

## Troubleshooting

| Symptom | Check |
|---|---|
| Restore fails during `docker build` | Access to the Bolt NuGet feed from the build machine; `NUGET_HTTP_REQUEST_TIMEOUT` is already raised to 600s |
| Playwright "Executable doesn't exist" | Image built successfully through the `playwright.ps1 install` layer; don't override `PLAYWRIGHT_BROWSERS_PATH` |
| Tests can't reach the environment | Run `test-docker-network.ps1`; VPN-only endpoints aren't reachable from Docker Desktop containers without host networking/DNS setup |
| Secrets-dependent tests fail | Mount the secrets bundle and set `BOLT_SECRETS_PATH` (see the `nexus-secrets` skill) |
