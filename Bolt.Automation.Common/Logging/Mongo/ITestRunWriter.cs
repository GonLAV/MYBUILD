using Bolt.Automation.Common.Context;

namespace Bolt.Automation.Common.Logging.Mongo;

public interface ITestRunWriter
{
    Task StartTestAsync(string testId, string testName, TestRunMetadata metadata);
    Task CompleteTestAsync(string testId, string outcome, TestFailureData? failure = null);
    Task FlushLogsAsync(string testId);
    void BufferLog(string testId, LogEntryDocument entry);
    void RecordStep(string testId, StepRecord step);
    void CompleteStep(string testId, string stepName, string status, long durationMs);
    void AddArtifact(string testId, ArtifactInfo artifact, ArtifactType type);
    Task<string?> UploadAndAddArtifactAsync(string testId, string localPath, string name, ArtifactType type);
    Task<string?> UploadAndAddArtifactAsync(string testId, byte[] data, string fileName, string contentType, ArtifactType type);
    Task SetIdentifiersAsync(string testId, TestContextData contextData);
}

public class TestFailureData
{
    public string[] Messages { get; set; } = [];
    public string[] StackTraces { get; set; } = [];
    public string[] ExceptionTypes { get; set; } = [];

    public string CombinedMessage => string.Join(System.Environment.NewLine, Messages);
    public string? PrimaryStackTrace => StackTraces.Length > 0 ? StackTraces[0] : null;
    public string? PrimaryExceptionType => ExceptionTypes.Length > 0 ? ExceptionTypes[0] : null;
}

public class TestRunMetadata
{
    public int? TestCaseId { get; set; }
    public string? FullyQualifiedName { get; set; }
    public string? ClassName { get; set; }
    public List<string> Categories { get; set; } = [];
    public string? Tenant { get; set; }
    public string? Environment { get; set; }
    public string? Lob { get; set; }
    public string? FrontEnd { get; set; }
    public string? Description { get; set; }

    /// <summary>
    /// 1-based [RetryOnFailure] attempt number, used only to stamp the log entries this attempt
    /// writes. The writer lives in Common and cannot see NUnit's execution context, so the
    /// resolver in the Tests project supplies it. Left at 1 by any caller that does not retry,
    /// which keeps the single-run path unchanged.
    /// </summary>
    public int Attempt { get; set; } = 1;
}

public enum ArtifactType
{
    Screenshot,
    DomSnapshot,
    ApiPayload,
    Email,
    AccessibilityReport
}
