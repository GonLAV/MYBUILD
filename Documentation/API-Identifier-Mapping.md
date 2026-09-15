# API Response → Test Context Identifier Mapping

## Overview

When API responses return identifiers (external IDs, friendly IDs, applicant IDs), these must be captured into the test's `ScopeContext` so they automatically persist to MongoDB at test completion.

**Data flow:**

```
API Response → Extension Method → ScopeContext.Data.{field} → TestBase.DisposeAsync → TestRunWriter.SetIdentifiersAsync → MongoDB TestIdentifiers
```

## Available Identifiers

| Context Field     | MongoDB Field     | Description                                      |
|-------------------|-------------------|--------------------------------------------------|
| `ExternalId`      | `externalId`      | Platform-assigned external identifier             |
| `FriendlyId`      | `friendlyId`      | Human-readable quote/application identifier       |
| `ApplicantId`     | `applicantId`     | Applicant/customer identifier                     |
| `QuoteId`         | `quoteId`         | Internal quote identifier                         |
| `Source`          | `source`          | Originating source/channel                        |

These are defined in:
- **Context model**: `Bolt.Automation.Common\Context\TestContextData.cs`
- **MongoDB document**: `Bolt.Automation.Common\Logging\Mongo\Documents\TestRunDetailDocument.cs` → `TestIdentifiers`
- **Writer**: `Bolt.Automation.Common\Logging\Mongo\TestRunWriter.cs` → `SetIdentifiersAsync`

---

## Extension Methods

### 1. Platform API (ACORD) — `AcordResponseExtensions`

**File**: `Bolt.Automation.ApiClients\PlatformApi\Extensions\AcordResponseExtensions.cs`  
**Namespace**: `Bolt.Automation.ApiClients.PlatformApi.Extensions`

Applies to any response inheriting from `AcordBaseResponse` (QuoteStart, QuoteStatus, QuoteDeepLink, etc.).

| Response Field      | → Context Field |
|----------------------|-----------------|
| `BoltExternalId`     | `ExternalId`    |

**Usage:**
```csharp
using Bolt.Automation.ApiClients.PlatformApi.Extensions;

var response = await api.QuoteStartWithRetryAsync(request).EnsureSuccessContentAsync();
response?.MapExternalId(scopeContext);
```

### 2. GetQuote API (Application) — `ApplicationResponseExtensions`

**File**: `Bolt.Automation.ApiClients\GetQuoteApi\Extensions\ApplicationResponseExtensions.cs`  
**Namespace**: `Bolt.Automation.ApiClients.GetQuoteApi.Extensions`

Applies to `PostApplicationResponseModel<T>` returned by `CreateApplicationAsync` and `PatchApplicationAsync`.

| Response Field   | → Context Field |
|------------------|-----------------|
| `Id`             | `ExternalId`    |
| `FriendlyId`     | `FriendlyId`    |
| `ApplicantId`    | `ApplicantId`   |

**Usage:**
```csharp
using Bolt.Automation.ApiClients.GetQuoteApi.Extensions;

var response = await api.CreateApplicationAsync(request).EnsureSuccessContentAsync();
response.MapIdentifiers(scopeContext);
```

---

## Adding a New API Mapping

When a new API response contains identifiers that should be tracked:

### Step 1 — Add context field (if needed)

If the identifier doesn't map to an existing `TestContextData` field, add a new property:

```csharp
// Bolt.Automation.Common\Context\TestContextData.cs
public string? NewIdentifier { get; set; }
```

### Step 2 — Add MongoDB field (if needed)

Add the corresponding field to `TestIdentifiers`:

```csharp
// Bolt.Automation.Common\Logging\Mongo\Documents\TestRunDetailDocument.cs
[BsonElement("newIdentifier")]
public string? NewIdentifier { get; set; }
```

Update the mapping in `TestRunWriter.SetIdentifiersAsync`:

```csharp
var update = Builders<TestRunDetailDocument>.Update.Set(t => t.Identifiers, new TestIdentifiers
{
    // ... existing fields ...
    NewIdentifier = contextData.NewIdentifier,
});
```

### Step 3 — Create extension method

Create a new extension class in the appropriate API project `Extensions` folder:

```csharp
// Bolt.Automation.ApiClients\{ApiName}\Extensions\{ResponseType}Extensions.cs
public static class MyResponseExtensions
{
    public static MyResponseModel MapIdentifiers(
        this MyResponseModel response,
        IScopeContext scopeContext)
    {
        if (response is null) return response;

        if (!string.IsNullOrEmpty(response.SomeId))
            scopeContext.Set(ctx => ctx.ExternalId, response.SomeId);

        return response;
    }
}
```

### Step 4 — Call it after the API call

In the helper or test that calls the API:

```csharp
var response = await api.SomeEndpointAsync(request).EnsureSuccessContentAsync();
response.MapIdentifiers(scopeContext);
```

No changes needed in `TestBase` or `TestRunWriter` — the existing disposal pipeline handles persistence automatically.

---

## Where Mappings Are Currently Used

| Helper                        | API Call                 | Extension Used      |
|-------------------------------|--------------------------|---------------------|
| `ProgressiveTestHelper`       | `QuoteStartWithRetryAsync` | `MapExternalId`   |
| `GetQuoteApiHelper`           | `CreateApplicationAsync`   | `MapIdentifiers`  |
| `PaaTestHelper`               | `CreateApplicationAsync`   | `MapIdentifiers`  |

---

## How It Reaches MongoDB

1. Extension method sets values on `ScopeContext.Data` (e.g., `ExternalId`, `FriendlyId`, `ApplicantId`)
2. Extension method also appends an `IdentifierSnapshot` to `ScopeContext.Data.IdentifierHistory`
3. `TestBase.DisposeAsync` calls `_testRunWriter.SetIdentifiersAsync(testId, ScopeContext.Data)`
4. `TestRunWriter.SetIdentifiersAsync` writes both the primary `identifiers` and the full `identifierHistory` to MongoDB
5. MongoDB `test_run_details` collection stores both:

```json
{
  "identifiers": {
    "quoteId": "...",
    "friendlyId": "HQ-12345",
    "externalId": "app-ext-id",
    "applicantId": "app-123",
    "source": "..."
  },
  "identifierHistory": [
    {
      "source": "QuoteStart",
      "externalId": "qs-ext-id",
      "friendlyId": null,
      "applicantId": null,
      "quoteId": null,
      "capturedAt": "2025-01-15T10:30:00Z"
    },
    {
      "source": "CreateApplication",
      "externalId": "app-ext-id",
      "friendlyId": "HQ-12345",
      "applicantId": "app-123",
      "quoteId": null,
      "capturedAt": "2025-01-15T10:30:05Z"
    }
  ]
}
```

---

## Multiple Identifiers Per Test

A single test may call multiple APIs that each return different identifiers (e.g., QuoteStart then CreateApplication). When this happens:

- **Primary fields** (`identifiers`) always hold the **latest** values — last call wins
- **`identifierHistory`** preserves **every** snapshot with its source and timestamp

This means:
- Simple queries against `identifiers.externalId` find by the most recent value
- Full history queries against `identifierHistory` can find a test by **any** identifier it used

### MongoDB Query Examples

Find by latest identifier:
```javascript
db.test_run_details.find({ "identifiers.externalId": "abc-123" })
```

Find by any identifier in history:
```javascript
db.test_run_details.find({ "identifierHistory.externalId": "qs-ext-id" })
```

Find tests that used both QuoteStart and CreateApplication:
```javascript
db.test_run_details.find({
  "identifierHistory.source": { $all: ["QuoteStart", "CreateApplication"] }
})
```

### Data Model

**Context** (`TestContextData`):
```csharp
// Primary fields (latest values)
public string? ExternalId { get; set; }
public string? FriendlyId { get; set; }
public string? ApplicantId { get; set; }

// Full history
public List<IdentifierSnapshot> IdentifierHistory { get; set; } = [];
```

**MongoDB** (`TestRunDetailDocument`):
```csharp
[BsonElement("identifiers")]
public TestIdentifiers Identifiers { get; set; } = new();

[BsonElement("identifierHistory")]
public List<IdentifierSnapshotRecord> IdentifierHistory { get; set; } = [];
```
