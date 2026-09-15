# MongoDB Test Reporting — Implementation Guide

## Overview

Every test run automatically writes structured data to three MongoDB collections in the `nexusAutomation` database. This enables historical reporting, failure analysis, and drill-downs into test execution details. The dashboard UI/API that reads this data lives in a **separate solution**.

This document covers the **write-side** implementation that lives in `Bolt.Automation.Common\Logging\Mongo\`.

---

## Architecture

```
IAutomationLogger (interface)
   ├── AutomationLogger        (existing — NLog to console/file/test output)
   └── MongoLoggingDecorator    (wraps AutomationLogger, buffers to MongoDB)
```

The decorator pattern means:
- **Zero changes** to existing `IAutomationLogger` interface
- **Zero changes** to any test code — tests call `_logger.Info(...)` as before
- NLog continues to work exactly as before
- MongoDB writes happen transparently via the decorator
- Toggled on/off via config flag — all MongoDB code is swallowed on failure (never breaks tests)

---

## File Layout

All MongoDB reporting code lives in `Bolt.Automation.Common\Logging\Mongo\`:

```
Bolt.Automation.Common\Logging\Mongo\
├── MongoReportingOptions.cs         # Configuration POCO
├── MongoContext.cs                  # IMongoContext — singleton MongoDB client + collection accessors
├── MongoReportingExtensions.cs      # DI registration: services.AddMongoReporting(configuration)
├── MongoIndexInitializer.cs         # Creates indexes on first run (idempotent)
├── MongoLoggingDecorator.cs         # Wraps IAutomationLogger, buffers logs to MongoDB
├── ITestRunWriter.cs                # Write-side interface + TestRunMetadata
├── TestRunWriter.cs                 # Full implementation — buffer, flush, upsert
├── NoOpTestRunWriter.cs             # No-op when Enabled=false
├── CiCdContextProvider.cs           # Reads Azure DevOps env vars, resolves runId
├── FailureFingerprintGenerator.cs   # Normalizes stack traces → SHA-256 fingerprint
├── S3ArtifactUploader.cs            # Uploads test artifacts (screenshots, DOM snapshots) to S3
└── Documents\
    ├── RunSummaryDocument.cs        # One doc per CI/CD pipeline run (or local session)
    ├── TestRunDetailDocument.cs     # One doc per individual test execution
    ├── LogEntryDocument.cs          # Individual log entries (buffered, TTL 30 days)
    ├── ExecutionContext.cs          # Machine + framework details embedded in run summaries
    └── CiCdInfo.cs                  # CI/CD metadata (build ID, branch, trigger, etc.)
```

---

## Configuration

### appsettings.json

```json
{
  "MongoReporting": {
    "Enabled": true,
    "ConnectionString": "mongodb+srv://autouser:***@mongo-auto-pl-0.muolnb.mongodb.net/nexusAutomation",
    "DatabaseName": "nexusAutomation",
    "Collections": {
      "RunSummaries": "run_summaries",
      "TestRunDetails": "test_run_details",
      "Logs": "logs"
    },
    "LogBufferSize": 100,
    "LogFlushIntervalMs": 5000
  }
}
```

| Setting | Description | Default |
|---|---|---|
| `Enabled` | Master toggle. `false` = all MongoDB code is no-op | `false` |
| `ConnectionString` | MongoDB Atlas connection string | — |
| `DatabaseName` | Target database | `nexusAutomation` |
| `Collections.*` | Collection names | `run_summaries`, `test_run_details`, `logs` |
| `LogBufferSize` | Flush log buffer to MongoDB every N entries | `100` |
| `LogFlushIntervalMs` | (Reserved for future time-based flush) | `5000` |

### CI/CD Override

Override via environment variable in the pipeline if needed:

```
MongoReporting__Enabled=true
MongoReporting__ConnectionString=mongodb+srv://...
```

---

## ID Scheme

### Run ID (`runId`)

Groups all tests in a single pipeline execution or local session.

| Context | Source | Example |
|---|---|---|
| **CI/CD** | `BUILD_BUILDID` env var | `"12345"` |
| **Local** | Random 5-digit number, stable per process | `"48723"` |

### Test ID (`testId`)

Identifies each test case. Resolved in priority order:

| Priority | Source | Example |
|---|---|---|
| 1 | `[TestCaseId(227409)]` attribute (NUnit property) | `"227409"` |
| 2 | First argument from the parameterized test's display name | `"212975"` |
| 3 | `[TestCaseId]` via attribute reflection (fallback) | `"227409"` |
| 4 | Sequential counter (last resort) | `"1"` |

For plain `[Test]` methods: testId comes from `[TestCaseId(227409)]` → `"227409"`

For parameterized tests (`[TestCase]`/`[TestCaseSource]`): testId is extracted from the display name, e.g.:
```
PGR_HQX2_CovMod_Dropdown_Values_PlymouthRock(testCaseId: "212975", addressKey: NJ_NewEgypt, carrier: PlymouthRock)
→ testId = "212975"
```

The compound index `{ runId, testId }` is unique — the same testCaseId can appear across runs but not within the same run.

---

## MongoDB Collections

### `run_summaries` — One doc per run

```jsonc
{
  "runId": "12345",
  "startedAt": ISODate,
  "completedAt": ISODate,
  "environment": "QA",
  "tenant": "PROGRESSIVEPL",
  "totalTests": 42,
  "passed": 38,
  "failed": 3,
  "skipped": 1,
  "passRate": 90.48,
  "failures": [                          // denormalized for dashboard speed
    { "testName": "...", "testCaseId": 227409, "errorMessage": "Expected: True But was: False", "durationMs": 12345 }
  ],
  "cicd": {
    "buildId": "12345", "branch": "RatesService", "triggeredBy": "helenl", "triggerType": "Manual", ...
  }
}
```

### `test_run_details` — One doc per test execution

```jsonc
{
  "runId": "12345",
  "testId": "227409",
  "testName": "PGR_HQX2_3PQ_Displayed_Property_Summary_Updated_After_Second_Call",
  "testCaseId": 227409,
  "fullyQualifiedName": "Bolt.Automation.Tests...PgrPrefillTests.PGR_HQX2_3PQ_...",
  "className": "PgrPrefillTests",
  "categories": ["Prefill"],
  "tenant": "PROGRESSIVEPL",
  "environment": "QA",
  "outcome": "Passed | Failed | Skipped | Error",
  "startedAt": ISODate,
  "completedAt": ISODate,
  "durationMs": 34567,
  "identifiers": { "quoteId": "Q-12345", "friendlyId": "FR-67890", "externalId": "EXT-111", "customerId": null },
  "failure": {
    "message": "Expected: True But was: False",
    "stackTrace": "...",
    "exceptionType": "NUnit.Framework.AssertionException",
    "step": "UI: Validate property section values in overview"
  },
  "steps": [
    { "name": "UI: Check for 'What year was this home built?' field", "status": "Running", "startedAt": ISODate }
  ],
  "analysis": { "failureFingerprint": "sha256-of-normalized-stack" },
  "cicd": { "buildId": "12345", "branch": "RatesService", "triggeredBy": "helenl" }
}
```

### `logs` — Individual log entries (TTL: 30 days)

```jsonc
{
  "runId": "12345",
  "testId": "227409",
  "timestamp": ISODate,
  "level": "Info",
  "category": "General | Step | UiAction | ApiCall | BusinessRule | DataValidation | Exception | Attachment",
  "message": "Starting step: UI: Check for 'What year was this home built?' field",
  "stepName": "UI: Check for 'What year was this home built?' field",
  "payload": { ... }    // category-specific structured data (see below)
}
```

---

## Log Categories & Payload Fields

Every log entry has a `category` that determines the shape of its `payload`. The decorator intercepts each `IAutomationLogger` method and writes the corresponding category.

### `General`

Basic log messages — `Info()`, `Debug()`, `Warning()`, `Error()`, `Fatal()`, `Trace()`, and JSON logging methods.

| Field | Value |
|---|---|
| `level` | `Info`, `Debug`, `Warning`, `Error`, `Fatal`, `Trace` |
| `category` | `"General"` |
| `message` | The formatted log message |
| `payload` | `null` |

**Logger call**: `_logger.Info("Starting test: PGR_HQX2_3PQ...")` 
**Logger call**: `_logger.LogJson("Request data", requestObj)`

### `Step`

Step start markers — produced by `StartStep()`.

| Field | Value |
|---|---|
| `level` | `Info` |
| `category` | `"Step"` |
| `message` | `"Starting step: UI: Check for 'What year was this home built?' field"` |
| `stepName` | The current step name (also set on all subsequent logs until `EndStep()`) |
| `payload` | `null` |

**Logger call**: `_logger.StartStep("UI: Check for 'What year was this home built?' field")`

> **Note**: `StartStep()` also records a `StepRecord` in `test_run_details.steps[]` with `name`, `status`, and `startedAt`.

### `UiAction`

UI interactions — produced by `LogUiAction()`.

| Field | Value |
|---|---|
| `level` | `Info` |
| `category` | `"UiAction"` |
| `message` | `"UI Action [Check Value] on PLYearBuilt: Value: 2022"` |
| `payload.actionType` | Action performed: `"Check Value"`, `"Click"`, `"SetValue"`, `"SelectOption"`, etc. |
| `payload.element` | Element identifier: `"PLYearBuilt"`, `"ArchitectureStyle"`, etc. |
| `payload.details` | Additional info: `"Value: 2022"`, `"Selected: Basic"`, etc. |

**Logger call**: `_logger.LogUiAction("Check Value", "PLYearBuilt", "Value: 2022")`

### `ApiCall`

HTTP API calls — produced by `LogApiCallAsync()`.

| Field | Value |
|---|---|
| `level` | `Info` |
| `category` | `"ApiCall"` |
| `message` | `"API Call: POST https://api.example.com/quote - Status: OK - Duration: 456ms"` |
| `payload.httpMethod` | `"GET"`, `"POST"`, `"PUT"`, `"DELETE"` |
| `payload.requestUri` | Full request URL |
| `payload.statusCode` | HTTP status code as integer: `200`, `400`, `500` |
| `payload.durationMs` | Round-trip duration in milliseconds |

**Logger call**: `_logger.LogApiCallAsync(request, response, 456)`

### `BusinessRule`

Business rule validation — produced by `LogBusinessRule()`.

| Field | Value |
|---|---|
| `level` | `Info` (passed) or `Error` (failed) |
| `category` | `"BusinessRule"` |
| `message` | `"Business Rule [MinPremiumCheck] PASSED: Premium meets minimum threshold"` |
| `payload.ruleName` | Rule identifier: `"MinPremiumCheck"`, `"CoverageRequired"`, etc. |
| `payload.ruleStatus` | `"PASSED"` or `"FAILED"` |
| `payload.details` | Descriptive text about the rule outcome |

**Logger call**: `_logger.LogBusinessRule("MinPremiumCheck", true, "Premium meets minimum threshold")`

### `DataValidation`

Data comparison validations — produced by `LogDataValidation()`.

| Field | Value |
|---|---|
| `level` | `Info` (passed) or `Error` (failed) |
| `category` | `"DataValidation"` |
| `message` | `"Validation [FieldValue] PASSED - Expected: 2022, Actual: 2022"` |
| `payload.validationType` | Type of validation: `"FieldValue"`, `"DropdownOptions"`, `"SectionContent"`, etc. |
| `payload.passed` | `true` or `false` |
| `payload.expected` | Expected value as string |
| `payload.actual` | Actual value as string |
| `payload.details` | Additional context |

**Logger call**: `_logger.LogDataValidation("FieldValue", true, "2022", "2022", "Year built field")`

### `Exception`

Logged exceptions — produced by `LogException()`.

| Field | Value |
|---|---|
| `level` | `Error` |
| `category` | `"Exception"` |
| `message` | `"Failed to load page - Exception: Timeout waiting for selector"` |
| `payload` | `null` |

**Logger call**: `_logger.LogException(ex, "Failed to load page")`

### `Attachment`

File attachments — produced by `AttachFile()`.

| Field | Value |
|---|---|
| `level` | `Info` |
| `category` | `"Attachment"` |
| `message` | `"File: /path/to/screenshot.png - Failure screenshot"` |
| `payload` | `null` |

**Logger call**: `_logger.AttachFile("/path/to/screenshot.png", "Failure screenshot")`

### Step Context

All log entries include `stepName` when logged inside an active `StartStep()`/`EndStep()` block. This enables filtering logs by step in the dashboard.

```csharp
using (_logger.StartStep("UI: Enter values and continue"))
{
    _logger.LogUiAction("SetValue", "PLYearBuilt", "Value: 2022");
    // ↑ This log entry will have stepName = "UI: Enter values and continue"
}
```

---

## Indexes

Created automatically on first test run via `MongoIndexInitializer`:

```
// run_summaries
{ runId: 1 }                                      — unique
{ environment: 1, tenant: 1, startedAt: -1 }      — dashboard filtering
{ startedAt: -1 }                                  — recent runs

// test_run_details
{ runId: 1, testId: 1 }                           — unique (compound)
{ testName: 1, environment: 1, startedAt: -1 }    — historical per-test
{ outcome: 1, runId: 1 }                          — failures in a run
{ testCaseId: 1 }                                  — lookup by test case ID (sparse)
{ analysis.failureFingerprint: 1 }                — similar failure grouping (sparse)
{ identifiers.quoteId: 1 }                        — business ID search (sparse)
{ identifiers.friendlyId: 1 }                     — business ID search (sparse)
{ identifiers.externalId: 1 }                     — business ID search (sparse)

// logs
{ testId: 1, timestamp: 1 }                       — logs for a test in order
{ runId: 1, level: 1 }                            — errors in a run
{ timestamp: 1 }, expireAfterSeconds: 30 days      — TTL auto-cleanup
```

---

## Test Lifecycle Flow

```
TestBase constructor
  │
  ├─ Resolve testId from [TestCaseId] trait or theory display name
  ├─ Resolve ITestRunWriter from DI
  ├─ Wrap IAutomationLogger with MongoLoggingDecorator (if enabled)
  ├─ Upsert RunSummaryDocument (increment totalTests)
  ├─ Insert TestRunDetailDocument (outcome: "Running")
  ├─ Register FirstChanceException handler (auto-detect failures)
  │
  │  [test method executes]
  │    ├─ _logger.Info(...)       → NLog + buffer LogEntryDocument
  │    ├─ _logger.StartStep(...) → NLog + track StepRecord
  │    ├─ _logger.LogUiAction(...)  → NLog + buffer with structured payload
  │    ├─ _logger.LogApiCallAsync() → NLog + buffer with HTTP details
  │    └─ Buffer auto-flushes every 100 entries
  │
  │  [if test fails]
  │    └─ NUnit outcome (TestContext.CurrentContext.Result) marks the failure
  │
  UITestBase.Dispose()
  │  ├─ Capture screenshot (always)
  │  ├─ Capture page source + DOM snapshot (only on failure)
  │  └─ base.Dispose()
  │
  TestBase.Dispose()
     ├─ Flush identifiers (QuoteId, FriendlyId, etc.) to MongoDB
     ├─ Flush remaining log buffer to logs collection
     ├─ Update TestRunDetailDocument (outcome, duration, steps, failure, fingerprint)
     └─ Update RunSummaryDocument (increment passed/failed/skipped, update passRate)
```

---

## Failure Detection

Test failures are detected **automatically** — no changes to test code required.

**Mechanism**: NUnit's `TestContext.CurrentContext.Result.Outcome` is checked in teardown (`TestBase.HasTestFailed`); the failure message and stack trace come from the NUnit result. Parallel tests don't interfere because `TestContext` is per-test.

---

## Failure Fingerprinting

When a test fails, the stack trace is normalized and hashed:

1. Remove line numbers (`:line XX`)
2. Replace GUIDs with `<GUID>`
3. Replace long numeric IDs with `<ID>`
4. Collapse whitespace
5. SHA-256 hash → `failureFingerprint`

This enables "similar failures" grouping across runs without ML — tests that fail in the same code path get the same fingerprint.

---

## Artifact Capture

| Artifact | On Pass | On Failure | Storage |
|---|---|---|---|
| Screenshot | ✅ | ✅ | Local disk |
| Page source (HTML) | ❌ | ✅ | Local disk |
| DOM snapshot | ❌ | ✅ | Local disk |

DOM snapshots use `page.ContentAsync()` via `IScreenshotManager.CaptureDomSnapshotAsync()`.

**Future (Phase 2)**: S3 upload with pre-signed URLs stored in `TestRunDetailDocument.artifacts`.

---

## DI Registration

Registered in `InfrastructureServiceCollectionExtensions.AddInfrastructureServices()`:

```csharp
services.AddAutomationLogger();       // existing — registers IAutomationLogger (NLog)
services.AddMongoReporting(configuration);  // new — registers IMongoContext, ITestRunWriter
```

`AddMongoReporting` registers:
- `IMongoContext` → `MongoContext` (singleton) or no-op if disabled
- `ITestRunWriter` → `TestRunWriter` (singleton) or `NoOpTestRunWriter` if disabled

The `MongoLoggingDecorator` is created manually in `TestBase` constructor (not via DI) because it wraps the per-test logger instance.

---

## CI/CD Context

`CiCdContextProvider` reads Azure DevOps pipeline environment variables:

| Field | Env Var |
|---|---|
| `buildId` | `BUILD_BUILDID` |
| `buildUrl` | `BUILD_BUILDURI` |
| `branch` | `BUILD_SOURCEBRANCH` |
| `commitSha` | `BUILD_SOURCEVERSION` |
| `triggeredBy` | `BUILD_REQUESTEDFOR` |
| `triggerType` | `BUILD_REASON` (Manual, IndividualCI, Schedule, PullRequest) |
| `agentName` | `AGENT_NAME` |
| `pipelineName` | `BUILD_DEFINITIONNAME` |

When running locally, these are all `null` and `cicd.isFromCiCd` returns `false`.

---

## Theory / Permutation Support

For `[Theory]` / `[RetryTheory]` tests with `[MemberData]`, each permutation is a separate document:

```csharp
[RetryTheory(1)]
[Tenant(Tenant.PROGRESSIVEPL)]
[MemberData(nameof(PlymouthRockTestCases))]
public async Task PGR_HQX2_CovMod_Dropdown_Values_PlymouthRock(
    string testCaseId, AddressKey addressKey, CarrierEnums carrier)
```

With test data:
```csharp
yield return new object[] { "212975", AddressKey.NJ_NewEgypt, CarrierEnums.PlymouthRock };
yield return new object[] { "212973", AddressKey.CT,          CarrierEnums.PlymouthRock };
```

Produces in MongoDB:
```
test_run_details:
  { runId: "48723", testId: "212975", testName: "PGR_HQX2_CovMod_...(testCaseId: \"212975\", ...)" }
  { runId: "48723", testId: "212973", testName: "PGR_HQX2_CovMod_...(testCaseId: \"212973\", ...)" }
```

---

## TestCaseId Attribute

`[TestCaseId(227409)]` is an NUnit `PropertyAttribute` that stamps the `"TestCaseId"` property onto the test.

```
Bolt.Automation.Tests\TestExtension\Attributes\
└── TestCaseIdAttribute.cs       # [TestCaseId(int)] : PropertyAttribute("TestCaseId", ...)
```

---

## Modified Files Summary

| File | Change |
|---|---|
| `Bolt.Automation.Common\Bolt.Automation.Common.csproj` | Added `MongoDB.Driver`, `Microsoft.Extensions.Options` packages |
| `Bolt.Automation.Common\Context\TestContextData.cs` | Added `CustomerId` property |
| `Bolt.Automation.Core\Infrastructure\InfrastructureServiceCollectionExtensions.cs` | Added `services.AddMongoReporting(configuration)` |
| `Bolt.Automation.Tests\TestExtension\Base\TestBase.cs` | Decorator wiring, `StartTestAsync`, `CompleteTestAsync`, failure detection, testId resolution |
| `Bolt.Automation.Tests\TestExtension\Base\UITestBase.cs` | DOM snapshot capture on failure only, `HasTestFailed` check |
| `Bolt.Automation.Tests\TestExtension\Attributes\TestCaseIdDiscoverer.cs` | New — missing discoverer for `[TestCaseId]` trait |
| `Bolt.Automation.FrontEnds\PlaywrightBase\Infrastructure\Screenshot\IScreenshotManager.cs` | Added `CaptureDomSnapshotAsync()` |
| `Bolt.Automation.FrontEnds\PlaywrightBase\Infrastructure\Screenshot\ScreenshotManager.cs` | Implemented `CaptureDomSnapshotAsync()` |
| `Bolt.Automation.Tests\appsettings.json` | Added `MongoReporting` and `Aws` config sections |
| `Bolt.Automation.InfraTests\appsettings.json` | Same config sections |

---

## Diagnostics

All MongoDB write errors are logged to `Debug.WriteLine` and `Console.Error` with the prefix `[MongoReporting]`. Check the **Debug Output** window in Visual Studio or stderr in CI/CD.

Example:
```
[MongoReporting] FlushLogs failed (23 entries): MongoWriteException: E11000 duplicate key error
[MongoReporting] StartTest failed: TimeoutException: A timeout occurred after 30000ms
```

---

## Toggling Off

Set `"Enabled": false` in `appsettings.json`. This causes:
- `ITestRunWriter` resolves to `NoOpTestRunWriter` (all methods are empty)
- `MongoLoggingDecorator` is NOT created — `_logger` is the raw `AutomationLogger`
- Zero MongoDB connections, zero network calls, zero overhead

---

## Remaining Work

### Phase 2: S3 Artifact Upload
- [ ] `IS3ArtifactUploader` / `S3ArtifactUploader` — upload screenshots + DOM snapshots to S3
- [ ] Integrate into `ScreenshotManager` — after local save, upload to S3
- [ ] Store S3 URLs in `TestRunDetailDocument.artifacts`
- [ ] AWS config is already in `appsettings.json` under `Aws` section

### Phase 3: Dashboard UI (Separate Solution)
- [ ] Minimal API / Blazor app reading from `nexusAutomation` database
- [ ] Run Overview, Failure Drilldown, Test History, Search by identifiers
- [ ] Failure Analysis — group by fingerprint, known issue linking

### Phase 4: Smart Analysis
- [ ] Rule-based failure classification (Timeout, ElementNotFound, AssertionFailure, Infrastructure)
- [ ] Similar failure grouping via `failureFingerprint`
- [ ] Known issue linking (`analysis.linkedBugId`)
