using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Bolt.Automation.Common.Logging.Mongo;

public class RunSummaryDocument
{
    [BsonId]
    public ObjectId Id { get; set; }

    [BsonElement("runId")]
    public string RunId { get; set; } = string.Empty;

    [BsonElement("runName")]
    public string RunName { get; set; } = string.Empty;

    [BsonElement("environment")]
    public string Environment { get; set; } = string.Empty;

    [BsonElement("startedAt")]
    public DateTime StartedAt { get; set; }

    [BsonElement("completedAt")]
    public DateTime? CompletedAt { get; set; }

    [BsonElement("durationMs")]
    public long? DurationMs { get; set; }

    [BsonElement("environments")]
    public List<string> Environments { get; set; } = [];

    [BsonElement("tenants")]
    public List<string> Tenants { get; set; } = [];

    [BsonElement("categories")]
    public List<string> Categories { get; set; } = [];

    [BsonElement("plannedTests")]
    public int? PlannedTests { get; set; }

    [BsonElement("totalTests")]
    public int TotalTests { get; set; }

    [BsonElement("inProgressTests")]
    public int InProgressTests { get; set; }

    [BsonElement("passed")]
    public int Passed { get; set; }

    [BsonElement("failed")]
    public int Failed { get; set; }

    [BsonElement("skipped")]
    public int Skipped { get; set; }

    [BsonElement("passRate")]
    public double PassRate { get; set; }

    [BsonElement("failures")]
    public List<FailureSummary> Failures { get; set; } = [];

    [BsonElement("skippedTests")]
    public List<SkipSummary> SkippedTests { get; set; } = [];

    [BsonElement("cicd")]
    public CiCdInfo CiCd { get; set; } = new();

    [BsonElement("executionContext")]
    public ExecutionContext ExecutionContext { get; set; } = new();

    [BsonElement("tags")]
    public List<string> Tags { get; set; } = [];

    [BsonElement("status")]
    public string Status { get; set; } = RunStatus.Pending;

    [BsonElement("metadata")]
    public BsonDocument Metadata { get; set; } = new();

    [BsonElement("orchestratorJobId")]
    public string? OrchestratorJobId { get; set; }
}

public static class RunStatus
{
    public const string Pending = "Pending";
    public const string Running = "Running";
    public const string Completed = "Completed";
    public const string CompletedWithFailures = "CompletedWithFailures";
}

public class FailureSummary
{
    // The TestRunDetails testId this entry came from. Nothing displays it — it is what lets a
    // later [RetryOnFailure] attempt pull its own superseded entry back out, which testName
    // cannot do on its own (two fixtures may hold a same-named test with no [TestCaseId]).
    [BsonElement("testId")]
    public string? TestId { get; set; }

    [BsonElement("testName")]
    public string TestName { get; set; } = string.Empty;

    [BsonElement("testCaseId")]
    public int? TestCaseId { get; set; }

    [BsonElement("errorMessage")]
    public string? ErrorMessage { get; set; }

    [BsonElement("category")]
    public string? Category { get; set; }

    [BsonElement("tenant")]
    public string? Tenant { get; set; }

    [BsonElement("durationMs")]
    public long DurationMs { get; set; }
}

public class SkipSummary
{
    // Same purpose as FailureSummary.TestId — the key a superseded attempt is pulled by.
    [BsonElement("testId")]
    public string? TestId { get; set; }

    [BsonElement("testName")]
    public string TestName { get; set; } = string.Empty;

    [BsonElement("testCaseId")]
    public int? TestCaseId { get; set; }

    [BsonElement("reason")]
    public string? Reason { get; set; }

    [BsonElement("category")]
    public string? Category { get; set; }

    [BsonElement("tenant")]
    public string? Tenant { get; set; }
}
