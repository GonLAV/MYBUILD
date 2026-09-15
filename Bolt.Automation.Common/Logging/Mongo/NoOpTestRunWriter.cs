using Bolt.Automation.Common.Context;

namespace Bolt.Automation.Common.Logging.Mongo;

public class NoOpTestRunWriter : ITestRunWriter
{
    public Task StartTestAsync(string testId, string testName, TestRunMetadata metadata) => Task.CompletedTask;
    public Task CompleteTestAsync(string testId, string outcome, TestFailureData? failure = null) => Task.CompletedTask;
    public Task FlushLogsAsync(string testId) => Task.CompletedTask;
    public void BufferLog(string testId, LogEntryDocument entry) { }
    public void RecordStep(string testId, StepRecord step) { }
    public void CompleteStep(string testId, string stepName, string status, long durationMs) { }
    public void AddArtifact(string testId, ArtifactInfo artifact, ArtifactType type) { }
    public Task<string?> UploadAndAddArtifactAsync(string testId, string localPath, string name, ArtifactType type)
        => Task.FromResult<string?>(null);
    public Task<string?> UploadAndAddArtifactAsync(string testId, byte[] data, string fileName, string contentType, ArtifactType type)
        => Task.FromResult<string?>(null);
    public Task SetIdentifiersAsync(string testId, TestContextData contextData) => Task.CompletedTask;
}
