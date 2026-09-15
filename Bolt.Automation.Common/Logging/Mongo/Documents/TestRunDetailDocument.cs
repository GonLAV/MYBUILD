using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Bolt.Automation.Common.Logging.Mongo;

public class TestRunDetailDocument
{
    [BsonId]
    public ObjectId Id { get; set; }

    [BsonElement("runId")]
    public string RunId { get; set; } = string.Empty;

    [BsonElement("testId")]
    public string TestId { get; set; } = string.Empty;

    [BsonElement("testName")]
    public string TestName { get; set; } = string.Empty;

    [BsonElement("testCaseId")]
    public int? TestCaseId { get; set; }

    [BsonElement("fullyQualifiedName")]
    public string? FullyQualifiedName { get; set; }

    [BsonElement("className")]
    public string? ClassName { get; set; }

    [BsonElement("categories")]
    public List<string> Categories { get; set; } = [];

    [BsonElement("description")]
    public string? Description { get; set; }

    [BsonElement("tenant")]
    public string? Tenant { get; set; }

    [BsonElement("environment")]
    public string? Environment { get; set; }

    [BsonElement("lob")]
    public string? Lob { get; set; }

    [BsonElement("frontEnd")]
    public string? FrontEnd { get; set; }

    [BsonElement("startedAt")]
    public DateTime StartedAt { get; set; }

    [BsonElement("completedAt")]
    public DateTime? CompletedAt { get; set; }

    [BsonElement("durationMs")]
    public long? DurationMs { get; set; }

    [BsonElement("outcome")]
    public string Outcome { get; set; } = "Running";

    [BsonElement("identifiers")]
    public TestIdentifiers Identifiers { get; set; } = new();

    [BsonElement("identifierHistory")]
    public List<IdentifierSnapshotRecord> IdentifierHistory { get; set; } = [];

    [BsonElement("failure")]
    public FailureInfo? Failure { get; set; }

    [BsonElement("steps")]
    public List<StepRecord> Steps { get; set; } = [];

    [BsonElement("artifacts")]
    public ArtifactCollection Artifacts { get; set; } = new();

    [BsonElement("analysis")]
    public FailureAnalysis? Analysis { get; set; }

    [BsonElement("cicd")]
    public CiCdInfo CiCd { get; set; } = new();

    [BsonElement("metadata")]
    public BsonDocument Metadata { get; set; } = new();

    [BsonElement("orchestratorWorkItemId")]
    public string? OrchestratorWorkItemId { get; set; }
}

public class TestIdentifiers
{
    [BsonElement("quoteId")]
    public string? QuoteId { get; set; }

    [BsonElement("friendlyId")]
    public string? FriendlyId { get; set; }

    [BsonElement("externalId")]
    public string? ExternalId { get; set; }

    [BsonElement("applicantId")]
    public string? ApplicantId { get; set; }

    [BsonElement("source")]
    public string? Source { get; set; }
}

public class IdentifierSnapshotRecord
{
    [BsonElement("source")]
    public string Source { get; set; } = string.Empty;

    [BsonElement("externalId")]
    public string? ExternalId { get; set; }

    [BsonElement("friendlyId")]
    public string? FriendlyId { get; set; }

    [BsonElement("applicantId")]
    public string? ApplicantId { get; set; }

    [BsonElement("quoteId")]
    public string? QuoteId { get; set; }

    [BsonElement("capturedAt")]
    public DateTime CapturedAt { get; set; }
}

public class FailureInfo
{
    [BsonElement("message")]
    public string? Message { get; set; }

    [BsonElement("stackTrace")]
    public string? StackTrace { get; set; }

    [BsonElement("exceptionType")]
    public string? ExceptionType { get; set; }

    [BsonElement("step")]
    public string? Step { get; set; }

    [BsonElement("screenshotUrl")]
    public string? ScreenshotUrl { get; set; }

    [BsonElement("domSnapshotUrl")]
    public string? DomSnapshotUrl { get; set; }
}

public class StepRecord
{
    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("status")]
    public string Status { get; set; } = "Running";

    [BsonElement("startedAt")]
    public DateTime StartedAt { get; set; }

    [BsonElement("durationMs")]
    public long? DurationMs { get; set; }

    [BsonElement("logCount")]
    public int LogCount { get; set; }
}

public class ArtifactCollection
{
    [BsonElement("screenshots")]
    public List<ArtifactInfo> Screenshots { get; set; } = [];

    [BsonElement("domSnapshots")]
    public List<ArtifactInfo> DomSnapshots { get; set; } = [];

    [BsonElement("apiPayloads")]
    public List<ArtifactInfo> ApiPayloads { get; set; } = [];

    [BsonElement("emails")]
    public List<ArtifactInfo> Emails { get; set; } = [];

    [BsonElement("accessibilityReports")]
    public List<ArtifactInfo> AccessibilityReports { get; set; } = [];
}

public class ArtifactInfo
{
    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("localPath")]
    public string? LocalPath { get; set; }

    [BsonElement("s3Url")]
    public string? S3Url { get; set; }

    [BsonElement("capturedAt")]
    public DateTime CapturedAt { get; set; }
}

public class FailureAnalysis
{
    [BsonElement("failureFingerprint")]
    public string? FailureFingerprint { get; set; }

    [BsonElement("similarTestIds")]
    public List<string> SimilarTestIds { get; set; } = [];

    [BsonElement("suggestedCategory")]
    public string? SuggestedCategory { get; set; }

    [BsonElement("isKnownIssue")]
    public bool IsKnownIssue { get; set; }

    [BsonElement("linkedBugId")]
    public string? LinkedBugId { get; set; }
}
