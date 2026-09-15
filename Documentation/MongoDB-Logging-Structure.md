# MongoDB Logging Structure Documentation

## Overview

The Bolt Automation Nexus framework provides a comprehensive MongoDB-based logging system that captures test execution data in a structured format. This document describes the MongoDB collections, document schemas, and all available data fields.

## MongoDB Collections

The logging system uses three primary collections in the `nexusAutomation` database:

1. **`run_summaries`** - High-level test run information
2. **`test_run_details`** - Individual test execution details
3. **`logs`** - Granular log entries for each test

These collection names are configurable via `MongoReportingOptions`.

---

## Collection Schemas

### 1. RunSummaryDocument (`run_summaries`)

Stores aggregate information about an entire test run (e.g., a CI/CD pipeline execution).

| Field | Type | Description |
|-------|------|-------------|
| `_id` | ObjectId | MongoDB unique identifier |
| `runId` | string | Unique identifier for the test run (from CI/CD or generated) |
| `runName` | string | Human-readable name for the run |
| `environment` | string | Primary environment (e.g., "qa", "staging") |
| `startedAt` | DateTime | When the run started (UTC) |
| `completedAt` | DateTime? | When the run completed (UTC), null if running |
| `durationMs` | long? | Total run duration in milliseconds |
| `environments` | List\<string\> | All unique environments encountered in tests |
| `tenants` | List\<string\> | All unique tenants encountered in tests |
| `categories` | List\<string\> | All unique test categories in the run |
| `plannedTests` | int? | Number of tests planned to execute |
| `totalTests` | int | Total number of tests started |
| `inProgressTests` | int | Number of tests currently running |
| `passed` | int | Number of passed tests |
| `failed` | int | Number of failed tests |
| `skipped` | int | Number of skipped tests |
| `passRate` | double | Pass rate percentage (0-100) |
| `failures` | List\<FailureSummary\> | Summary of all failures |
| `skippedTests` | List\<SkipSummary\> | Summary of all skipped tests |
| `cicd` | CiCdInfo | CI/CD context information |
| `executionContext` | ExecutionContext | Machine and framework details |
| `tags` | List\<string\> | Custom tags for the run |
| `status` | string | Run status: "Pending", "Running", "Completed", "CompletedWithFailures" |
| `metadata` | BsonDocument | Flexible key-value metadata |
| `orchestratorJobId` | string? | Id of the orchestrator job that produced this run (distributed execution) |

#### FailureSummary

| Field | Type | Description |
|-------|------|-------------|
| `testName` | string | Name of the failed test |
| `testCaseId` | int? | Associated test case ID (if any) |
| `errorMessage` | string? | Error message from the failure |
| `category` | string? | Test category |
| `tenant` | string? | Tenant under test |
| `durationMs` | long | Test duration in milliseconds |

#### SkipSummary

| Field | Type | Description |
|-------|------|-------------|
| `testName` | string | Name of the skipped test |
| `testCaseId` | int? | Associated test case ID (if any) |
| `reason` | string? | Reason for skipping |
| `category` | string? | Test category |
| `tenant` | string? | Tenant under test |

---

### 2. TestRunDetailDocument (`test_run_details`)

Stores detailed information about a single test execution.

| Field | Type | Description |
|-------|------|-------------|
| `_id` | ObjectId | MongoDB unique identifier |
| `runId` | string | Links to the parent run |
| `testId` | string | Unique identifier for this test execution |
| `testName` | string | Test method name |
| `testCaseId` | int? | Test case ID from test management system |
| `fullyQualifiedName` | string? | Fully qualified test name (namespace.class.method) |
| `className` | string? | Test class name |
| `categories` | List\<string\> | Test categories/tags |
| `tenant` | string? | Tenant under test (e.g., "PROGRESSIVEPL") |
| `environment` | string? | Environment (e.g., "qa", "staging") |
| `lob` | string? | Line of business (e.g., "Property") |
| `frontEnd` | string? | Frontend type (e.g., "HQXConsumer") |
| `startedAt` | DateTime | Test start time (UTC) |
| `completedAt` | DateTime? | Test completion time (UTC), null if running |
| `durationMs` | long? | Test duration in milliseconds |
| `outcome` | string | Test outcome: "Running", "Passed", "Failed", "Skipped" |
| `identifiers` | TestIdentifiers | Current test identifiers (latest values) |
| `identifierHistory` | List\<IdentifierSnapshotRecord\> | History of all identifier updates |
| `failure` | FailureInfo? | Failure details if test failed |
| `steps` | List\<StepRecord\> | All test steps executed |
| `artifacts` | ArtifactCollection | Screenshots, DOM snapshots, API payloads |
| `analysis` | FailureAnalysis? | Automated failure analysis |
| `cicd` | CiCdInfo | CI/CD context |
| `metadata` | BsonDocument | Flexible key-value metadata |

#### TestIdentifiers

Tracks business identifiers generated during test execution (updated as test progresses).

| Field | Type | Description |
|-------|------|-------------|
| `quoteId` | string? | Quote ID from the system under test |
| `friendlyId` | string? | Human-readable identifier |
| `externalId` | string? | External system identifier |
| `applicantId` | string? | Applicant/user identifier |
| `source` | string? | Source system that generated the ID |

#### IdentifierSnapshotRecord

Captures a point-in-time snapshot of identifiers (e.g., after each API call).

| Field | Type | Description |
|-------|------|-------------|
| `source` | string | Source of identifiers (e.g., "QuoteStartApi") |
| `externalId` | string? | External ID at this point |
| `friendlyId` | string? | Friendly ID at this point |
| `applicantId` | string? | Applicant ID at this point |
| `quoteId` | string? | Quote ID at this point |
| `capturedAt` | DateTime | When this snapshot was captured (UTC) |

#### FailureInfo

Details about test failures.

| Field | Type | Description |
|-------|------|-------------|
| `message` | string? | Exception message(s) |
| `stackTrace` | string? | Stack trace of the exception |
| `exceptionType` | string? | Exception type (e.g., "AssertionException") |
| `step` | string? | Step name where failure occurred |
| `screenshotUrl` | string? | URL to failure screenshot (if captured) |
| `domSnapshotUrl` | string? | URL to DOM snapshot (if captured) |

#### StepRecord

Represents a single test step.

| Field | Type | Description |
|-------|------|-------------|
| `name` | string | Step name |
| `status` | string | "Running", "Passed", "Failed", "Skipped", "Blocked", "Warning" |
| `startedAt` | DateTime | Step start time (UTC) |
| `durationMs` | long? | Step duration in milliseconds |
| `logCount` | int | Number of log entries in this step |

#### ArtifactCollection

Groups all test artifacts by type.

| Field | Type | Description |
|-------|------|-------------|
| `screenshots` | List\<ArtifactInfo\> | All screenshots captured |
| `domSnapshots` | List\<ArtifactInfo\> | All DOM snapshots captured |
| `apiPayloads` | List\<ArtifactInfo\> | All API request/response payloads |

#### ArtifactInfo

Information about a single artifact.

| Field | Type | Description |
|-------|------|-------------|
| `name` | string | Artifact filename |
| `localPath` | string? | Local file path (if available) |
| `s3Url` | string? | S3 URL (if uploaded to cloud storage) |
| `capturedAt` | DateTime | When artifact was captured (UTC) |

#### FailureAnalysis

Automated analysis of test failures.

| Field | Type | Description |
|-------|------|-------------|
| `failureFingerprint` | string? | Hash of exception type + stack trace for grouping similar failures |
| `similarTestIds` | List\<string\> | IDs of tests with similar failures |
| `suggestedCategory` | string? | Suggested failure category (e.g., "UI", "API") |
| `isKnownIssue` | bool | Whether this matches a known issue |
| `linkedBugId` | string? | ID of linked bug ticket |

---

### 3. LogEntryDocument (`logs`)

Stores individual log entries for granular test execution tracking.

| Field | Type | Description |
|-------|------|-------------|
| `_id` | ObjectId | MongoDB unique identifier |
| `runId` | string | Links to the parent run |
| `testId` | string | Links to the parent test |
| `timestamp` | DateTime | When the log was created (UTC) |
| `level` | string | Log level: "Trace", "Debug", "Info", "Warning", "Error", "Fatal" |
| `category` | string | Log category (see categories below) |
| `message` | string | Log message text |
| `stepName` | string? | Step name if logged within a step |
| `stepLevel` | int? | Nesting level of the step (for hierarchical steps) |
| `payload` | BsonDocument? | Structured data payload (varies by category) |
| `metadata` | BsonDocument? | Additional flexible metadata |

#### Log Categories

The `category` field indicates the type of log entry:

- **`General`** - Standard log messages
- **`Step`** - Step start/end events
- **`Attachment`** - File attachments
- **`BusinessRule`** - Business rule validation results
- **`ApiCall`** - HTTP API calls
- **`UiAction`** - UI interactions (clicks, inputs, etc.)
- **`DataValidation`** - Data validation results
- **`Exception`** - Exception logs

#### Payload Structures by Category

##### BusinessRule Payload
```json
{
  "ruleName": "string",
  "ruleStatus": "PASSED | FAILED",
  "details": "string"
}
```

##### ApiCall Payload
```json
{
  "httpMethod": "GET | POST | PUT | DELETE",
  "requestUri": "string",
  "statusCode": 200,
  "durationMs": 1234,
  "requestHeaders": { /* header dictionary, sensitive values redacted */ },
  "responseHeaders": { /* header dictionary, sensitive values redacted */ },
  "requestContent": "string (truncated if > 5000 chars)",
  "responseContent": "string (truncated if > 5000 chars)",
  "payloadArtifactUrl": "string (S3 URL if content > 5000 chars)"
}
```

**Note:** API headers like `Authorization`, `X-Api-Key`, and `Ocp-Apim-Subscription-Key` are automatically redacted as `[REDACTED]`.

##### UiAction Payload
```json
{
  "actionType": "Click | Type | Select | Navigate | etc.",
  "element": "string (selector or element description)",
  "details": "string"
}
```

##### DataValidation Payload
```json
{
  "validationType": "string",
  "passed": true,
  "expected": "string",
  "actual": "string",
  "details": "string"
}
```

---

## Supporting Data Structures

### CiCdInfo

Contains CI/CD pipeline context information.

| Field | Type | Description |
|-------|------|-------------|
| `buildId` | string? | CI/CD build ID |
| `buildUrl` | string? | URL to build in CI/CD system |
| `branch` | string? | Git branch name |
| `commitSha` | string? | Git commit SHA |
| `triggeredBy` | string? | User or system that triggered the build |
| `triggerType` | string? | Trigger type (e.g., "manual", "scheduled", "commit") |
| `agentName` | string? | Build agent machine name |
| `pipelineName` | string? | Pipeline/workflow name |
| `IsFromCiCd` | bool | Computed property: true if `buildId` is not null |

### ExecutionContext

Contains runtime environment information.

| Field | Type | Description |
|-------|------|-------------|
| `machineName` | string | Machine name where test executed |
| `userName` | string? | OS username |
| `os` | string? | Operating system description |
| `framework` | string? | .NET framework version |
| `testFramework` | string? | Test framework and version (e.g., "NUnit 4.2.2") |
| `isLocal` | bool | True if running locally, false if in CI/CD |

---

## Configuration

### MongoReportingOptions

Configuration options (typically in `appsettings.json`):

```json
{
  "MongoReporting": {
    "Enabled": true,
    "ConnectionString": "mongodb://localhost:27017",
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

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `Enabled` | bool | false | Enables/disables MongoDB logging |
| `ConnectionString` | string | "" | MongoDB connection string |
| `DatabaseName` | string | "nexusAutomation" | Database name |
| `Collections.RunSummaries` | string | "run_summaries" | Collection name for run summaries |
| `Collections.TestRunDetails` | string | "test_run_details" | Collection name for test details |
| `Collections.Logs` | string | "logs" | Collection name for log entries |
| `LogBufferSize` | int | 100 | Number of log entries to buffer before flushing |
| `LogFlushIntervalMs` | int | 5000 | Max time in ms before forcing log flush |

---

## Logging API

### Core Logger Interface (IAutomationLogger)

The framework provides specialized logging methods that automatically populate MongoDB with structured data.

#### Basic Logging Methods

```csharp
void Info(string message, params object[] args)
void Debug(string message, params object[] args)
void Trace(string message, params object[] args)
void Warning(string message, params object[] args)
void Error(string message, params object[] args)
void Fatal(string message, params object[] args)
void LogException(Exception exception, string? message = null, params object[] args)
```

**Important:** Use `$` string interpolation (e.g., `_logger.Info($"value={value}")`) instead of NLog-style named placeholders. The MongoLoggingDecorator resolves `$` interpolation correctly, while named placeholders produce unresolved template strings in MongoDB.

#### Step Management

```csharp
// Manual step control
IStepScope StartStep(string stepName, string? description = null)
void EndStep(StepStatus status = StepStatus.Passed)

// Automatic step execution (recommended)
Task ExecuteStepAsync(string stepName, Func<Task> action, string? description = null)
Task<T> ExecuteStepAsync<T>(string stepName, Func<Task<T>> func, string? description = null)
```

**Example:**
```csharp
await _logger.ExecuteStepAsync("Verify user login", async () =>
{
    await loginPage.EnterCredentials("user@example.com", "password");
    await loginPage.ClickLogin();
    Assert.That(await homePage.IsLoaded(), Is.True);
});
```

#### Specialized Logging Methods

```csharp
// Business rule validation
void LogBusinessRule(string ruleName, bool passed, string? details = null)

// API call logging (automatically captures request/response)
Task LogApiCallAsync(HttpRequestMessage request, HttpResponseMessage response, long durationMs)

// UI action logging
void LogUiAction(string actionType, string element, string? details = null)

// Data validation logging
void LogDataValidation(string validationType, bool passed, string expected, string actual, string? details = null)

// File attachment
void AttachFile(string filePath, string? description = null)

// JSON logging (for debugging complex objects)
void LogJson(string message, object data, LogLevel level = LogLevel.Debug)
void LogJsonWithPreview(string message, object data, LogLevel level = LogLevel.Debug)
void LogJsonStyled(string message, object data, LogLevel level = LogLevel.Debug)
```

#### Extension Method Examples

**LogDataValidation:**
```csharp
_logger.LogDataValidation(
    validationType: "Year Built Empty",
    passed: string.IsNullOrEmpty(yearBuiltValue),
    expected: "empty",
    actual: yearBuiltValue ?? "empty",
    details: "Year built should be empty on first load"
);
```

**LogBusinessRule:**
```csharp
_logger.LogBusinessRule(
    ruleName: "Premium Calculation",
    passed: calculatedPremium == expectedPremium,
    details: $"Expected: ${expectedPremium}, Actual: ${calculatedPremium}"
);
```

**LogUiAction:**
```csharp
_logger.LogUiAction(
    actionType: "Click",
    element: "Continue Button",
    details: "Navigating to next page"
);
```

---

## Test Context Integration

Tests can set business identifiers that are automatically tracked in MongoDB:

```csharp
// Set identifiers (captured in TestRunDetailDocument.identifiers)
ScopeContext.Set(ctx => ctx.QuoteId, "Q-12345");
ScopeContext.Set(ctx => ctx.FriendlyId, "PL-2024-001");
ScopeContext.Set(ctx => ctx.ApplicantId, "APP-999");
ScopeContext.Set(ctx => ctx.ExternalId, "EXT-789");

// These are automatically written to MongoDB by the framework
```

The `identifierHistory` field maintains a chronological record of how these values change throughout test execution (e.g., after each API call).

---

## Artifact Management

### Automatic Artifact Handling

The framework automatically uploads artifacts to S3 (if configured) and references them in MongoDB:

- **Screenshots**: Captured on failure or manually via test code
- **DOM Snapshots**: Browser DOM state at point of capture
- **API Payloads**: Large API request/response bodies (> 5KB)

### Manual Artifact Upload

```csharp
// Via ITestRunWriter (injected via DI)
string? s3Url = await _testRunWriter.UploadAndAddArtifactAsync(
    testId: testId,
    localPath: "/path/to/file.png",
    name: "failure_screenshot.png",
    type: ArtifactType.Screenshot
);
```

---

## Querying MongoDB Data

### Common Queries

**Find all failed tests in a run:**
```javascript
db.test_run_details.find({
  runId: "2024-01-15_12-30-45",
  outcome: "Failed"
})
```

**Find tests for a specific tenant:**
```javascript
db.test_run_details.find({
  tenant: "PROGRESSIVEPL",
  outcome: "Passed"
})
```

**Find all API call logs for a test:**
```javascript
db.logs.find({
  testId: "test_abc123",
  category: "ApiCall"
}).sort({ timestamp: 1 })
```

**Find tests with a specific failure fingerprint (similar failures):**
```javascript
db.test_run_details.find({
  "analysis.failureFingerprint": "abc123def456"
})
```

**Find all data validation failures:**
```javascript
db.logs.find({
  category: "DataValidation",
  "payload.passed": false
})
```

**Get test duration statistics:**
```javascript
db.test_run_details.aggregate([
  { $match: { outcome: "Passed" } },
  { $group: {
    _id: "$testName",
    avgDuration: { $avg: "$durationMs" },
    maxDuration: { $max: "$durationMs" },
    minDuration: { $min: "$durationMs" }
  }}
])
```

---

## MongoDB Indexes

The framework automatically creates the following indexes for optimal query performance:

### run_summaries
- `runId` (unique)
- `startedAt` (descending)
- `status`

### test_run_details
- `runId` + `testId` (compound, unique)
- `runId` + `outcome` (compound)
- `runId` + `tenant` (compound)
- `testCaseId`
- `outcome`
- `tenant`
- `startedAt` (descending)
- `analysis.failureFingerprint`

### logs
- `runId` + `testId` + `timestamp` (compound)
- `category`
- `level`

These indexes are created automatically on first use via `MongoIndexInitializer`.

---

## Best Practices

### 1. Use Step-Based Organization
```csharp
await _logger.ExecuteStepAsync("Setup: Navigate to login page", async () => {
    await browser.Navigate(loginUrl);
});

await _logger.ExecuteStepAsync("Action: Submit login form", async () => {
    await loginPage.Login("user", "pass");
});

await _logger.ExecuteStepAsync("Verify: User is logged in", async () => {
    Assert.That(await homePage.IsDisplayed(), Is.True);
});
```

### 2. Log Business Rules Explicitly
```csharp
_logger.LogBusinessRule(
    "Minimum Premium Rule",
    passed: premium >= 100,
    details: $"Premium must be at least $100. Actual: ${premium}"
);
```

### 3. Use LogDataValidation for Assertions
```csharp
_logger.LogDataValidation(
    validationType: "Address Validation",
    passed: actualAddress == expectedAddress,
    expected: expectedAddress,
    actual: actualAddress,
    details: "Addresses should match after prefill"
);
Assert.That(actualAddress, Is.EqualTo(expectedAddress));
```

### 4. Always Use $ Interpolation
```csharp
// ✅ Correct - uses $ interpolation
_logger.Info($"Processing quote {quoteId} for tenant {tenant}");

// ❌ Incorrect - NLog named placeholders don't resolve in MongoDB
_logger.Info("Processing quote {quoteId} for tenant {tenant}", quoteId, tenant);
```

### 5. Let the Framework Handle Identifiers
```csharp
// The framework automatically captures identifiers from API responses
// Just use ScopeContext.Set when you have them
ScopeContext.Set(ctx => ctx.QuoteId, response.QuoteId);
// This will be automatically persisted to MongoDB
```

---

## Troubleshooting

### Logs Not Appearing in MongoDB

1. **Check configuration**: Ensure `MongoReporting.Enabled` is `true` in `appsettings.json`
2. **Verify connection**: Check that MongoDB is accessible from the test machine
3. **Check testId**: Logs require a valid `testId` - check console for warnings:
   ```
   [MongoReporting] WARNING: _testId is null — MongoDB log entries will not be buffered
   ```

### Large API Payloads

API payloads larger than 5KB are automatically uploaded to S3 and referenced via `payloadArtifactUrl` in the log entry. Only a truncated preview (1KB) is stored inline.

### String Interpolation Issues

If log messages show unresolved placeholders like `{value}` in MongoDB, you're using NLog-style named placeholders. Switch to `$` interpolation:

```csharp
// Wrong
_logger.Info("Value: {value}", myValue);

// Right
_logger.Info($"Value: {myValue}");
```

---

## Summary

The MongoDB logging structure provides comprehensive test execution tracking with:

- **Run-level aggregation** in `run_summaries`
- **Test-level details** in `test_run_details` with steps, artifacts, and identifiers
- **Granular logging** in `logs` with structured payloads for different log types
- **Automatic artifact management** with S3 integration
- **Rich querying capabilities** via MongoDB indexes
- **CI/CD integration** with automatic context capture

This structured approach enables powerful test analytics, failure analysis, trend tracking, and debugging capabilities.
