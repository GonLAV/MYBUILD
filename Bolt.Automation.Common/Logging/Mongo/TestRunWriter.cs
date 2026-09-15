using System.Collections.Concurrent;
using System.Diagnostics;
using Bolt.Automation.Common.Context;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Bolt.Automation.Common.Logging.Mongo;

public class TestRunWriter : ITestRunWriter
{
    private readonly IMongoContext _mongo;
    private readonly MongoReportingOptions _options;
    private readonly IS3ArtifactUploader _s3;
    private readonly string _runId;
    private readonly CiCdInfo _cicd;

    private readonly ConcurrentDictionary<string, List<LogEntryDocument>> _logBuffers = new();
    private readonly ConcurrentDictionary<string, List<StepRecord>> _stepBuffers = new();
    private readonly ConcurrentDictionary<string, ArtifactCollection> _artifactBuffers = new();
    private readonly ConcurrentDictionary<string, DateTime> _testStartTimes = new();
    private readonly ConcurrentDictionary<string, List<Task>> _pendingFlushTasks = new();

    // Attempt number of the run currently in flight per test, seeded by StartTestAsync so that
    // BufferLog — which receives only a testId — can stamp each log entry with the attempt that
    // produced it. Absent means attempt 1. Used for nothing else: a retried test keeps a SINGLE
    // TestRunDetails document that every attempt overwrites, so the attempt number survives only
    // on the log entries, and a reader recovers the attempt count as max(attempt) over them.
    //
    // Not removed in CompleteTestAsync, unlike _testStartTimes: the next attempt overwrites the
    // entry anyway, while removing it would stamp anything logged after the outcome is recorded
    // as attempt 1. One small int per testId, for the lifetime of a single test run.
    private readonly ConcurrentDictionary<string, int> _testAttempts = new();

    // Outcomes that increment a run-summary counter, and so have to be decremented again when a
    // later [RetryOnFailure] attempt supersedes them. "Running" is not one of them.
    private static bool IsCountedOutcome(string? outcome)
        => outcome is "Passed" or "Failed" or "Skipped";

    private static bool _runSummaryInitialized;
    private static readonly SemaphoreSlim _runInitSemaphore = new(1, 1);

    public TestRunWriter(IMongoContext mongo, IOptions<MongoReportingOptions> options, IS3ArtifactUploader s3)
    {
        _mongo = mongo;
        _options = options.Value;
        _s3 = s3;
        _runId = CiCdContextProvider.ResolveRunId(mongo);
        _cicd = CiCdContextProvider.Current;
    }

    public async Task StartTestAsync(string testId, string testName, TestRunMetadata metadata)
    {
        // Skip MongoDB test run creation during debugging
        if (Debugger.IsAttached)
        {
            return;
        }

        if (!_mongo.IsEnabled) return;

        try
        {
            await EnsureRunSummaryAsync(metadata.Environment).ConfigureAwait(false);

            var now = DateTime.UtcNow;
            _testStartTimes[testId] = now;
            _testAttempts[testId] = metadata.Attempt < 1 ? 1 : metadata.Attempt;

            var detail = new TestRunDetailDocument
            {
                RunId = _runId,
                TestId = testId,
                TestName = testName,
                TestCaseId = metadata.TestCaseId,
                FullyQualifiedName = metadata.FullyQualifiedName,
                ClassName = metadata.ClassName,
                Categories = metadata.Categories,
                Description = metadata.Description,
                Tenant = metadata.Tenant,
                Environment = metadata.Environment,
                Lob = metadata.Lob,
                FrontEnd = metadata.FrontEnd,
                StartedAt = now,
                Outcome = "Running",
                CiCd = _cicd,
                OrchestratorWorkItemId = CiCdContextProvider.ResolveOrchestratorWorkItemId()
            };

            // Insert, NOT upsert, and it must stay that way: on a [RetryOnFailure] re-run the
            // unique index on runId+testId rejects this, which is precisely what keeps the two
            // counters below from advancing a second time for the same test. Make this tolerate
            // an existing document and TotalTests starts counting attempts while
            // UpdateRunSummaryCountersAsync counts tests, leaving InProgressTests permanently
            // above zero and the run permanently Running.
            await _mongo.TestRunDetails.InsertOneAsync(detail).ConfigureAwait(false);

            var filter = Builders<RunSummaryDocument>.Filter.Eq(r => r.RunId, _runId);
            var update = Builders<RunSummaryDocument>.Update
                .Inc(r => r.TotalTests, 1)
                .Inc(r => r.InProgressTests, 1);

            if (!string.IsNullOrEmpty(metadata.Tenant))
                update = update.AddToSet(r => r.Tenants, metadata.Tenant);

            if (!string.IsNullOrEmpty(metadata.Environment))
                update = update.AddToSet(r => r.Environments, metadata.Environment);

            var validCategories = metadata.Categories.Where(c => !string.IsNullOrEmpty(c)).ToList();
            if (validCategories.Count > 0)
                update = update.AddToSetEach(r => r.Categories, validCategories);

            await _mongo.RunSummaries.UpdateOneAsync(filter, update).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MongoReporting] StartTest FAILED: {ex.GetType().Name}: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[MongoReporting] StartTest failed: {ex.GetType().Name}: {ex.Message}");
        }
    }

    public async Task CompleteTestAsync(string testId, string outcome, TestFailureData? failure = null)
    {
        // Skip MongoDB test run completion during debugging
        if (Debugger.IsAttached)
        {
            return;
        }

        if (!_mongo.IsEnabled) return;

        try
        {
            await FlushLogsAsync(testId).ConfigureAwait(false);

            var now = DateTime.UtcNow;
            _testStartTimes.TryGetValue(testId, out var startedAt);
            var durationMs = startedAt != default ? (long)(now - startedAt).TotalMilliseconds : 0;

            var filter = Builders<TestRunDetailDocument>.Filter.Eq(t => t.RunId, _runId)
                       & Builders<TestRunDetailDocument>.Filter.Eq(t => t.TestId, testId);
            var updateBuilder = Builders<TestRunDetailDocument>.Update
                .Set(t => t.CompletedAt, now)
                .Set(t => t.DurationMs, durationMs)
                .Set(t => t.Outcome, outcome);

            // A test that passes on a [RetryOnFailure] retry writes into the SAME document the
            // failed attempt already filled in, and the `failure != null` branch below only ever
            // SETS these two. Left alone, a green test keeps the previous attempt's error message
            // and its failure fingerprint — which then groups it with real failures.
            // Only the fingerprint is cleared, not the whole `analysis` subdocument: its other
            // fields (isKnownIssue, linkedBugId, suggestedCategory, similarTestIds) are written
            // by triage outside this writer, and unsetting the parent would silently drop them.
            if (failure == null)
                updateBuilder = updateBuilder
                    .Unset(t => t.Failure)
                    .Unset(t => t.Analysis!.FailureFingerprint);

            // Steps
            if (_stepBuffers.TryRemove(testId, out var steps))
                updateBuilder = updateBuilder.Set(t => t.Steps, steps);

            // Artifacts
            if (_artifactBuffers.TryRemove(testId, out var artifacts))
                updateBuilder = updateBuilder.Set(t => t.Artifacts, artifacts);

            // Failure info
            if (failure != null)
            {
                var currentStep = steps?.LastOrDefault(s => s.Status == "Running")?.Name;
                var failureInfo = new FailureInfo
                {
                    Message = failure.CombinedMessage,
                    StackTrace = failure.PrimaryStackTrace,
                    ExceptionType = failure.PrimaryExceptionType,
                    Step = currentStep
                };
                updateBuilder = updateBuilder.Set(t => t.Failure, failureInfo);

                var fingerprint = FailureFingerprintGenerator.Generate(
                    failure.PrimaryExceptionType, failure.PrimaryStackTrace, failure.CombinedMessage);
                if (!string.IsNullOrEmpty(fingerprint))
                {
                    updateBuilder = updateBuilder.Set(t => t.Analysis, new FailureAnalysis
                    {
                        FailureFingerprint = fingerprint
                    });
                }
            }

            // FindOneAndUpdate, not UpdateOne: the pre-image says — atomically, in the same
            // operation that overwrites it — what this test had ALREADY been counted as. A
            // retried test keeps one document (the unique index on runId+testId makes the retry's
            // insert fail), but CompleteTestAsync still runs once per [RetryOnFailure] attempt, so
            // without this every attempt incremented its own bucket: a 2-test run with one retry
            // reported 2 total, 2 passed and 1 failed. Reading the outcome separately cannot do
            // this — it races the attempt that is writing it.
            var before = await _mongo.TestRunDetails.FindOneAndUpdateAsync(
                filter,
                updateBuilder,
                new FindOneAndUpdateOptions<TestRunDetailDocument> { ReturnDocument = ReturnDocument.Before })
                .ConfigureAwait(false);

            // Only a terminal pre-image outcome was ever counted; "Running" — or no document at
            // all, when the first attempt's insert was lost to its write guard — means there is
            // nothing to undo.
            var supersededOutcome = before != null && IsCountedOutcome(before.Outcome)
                ? before.Outcome
                : null;

            // Update run summary counters
            await UpdateRunSummaryCountersAsync(testId, outcome, failure, durationMs, supersededOutcome)
                .ConfigureAwait(false);

            _testStartTimes.TryRemove(testId, out _);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MongoReporting] CompleteTest FAILED: {ex.GetType().Name}: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[MongoReporting] CompleteTest failed: {ex.GetType().Name}: {ex.Message}");
        }
    }

    public void BufferLog(string testId, LogEntryDocument entry)
    {
        // Skip MongoDB log buffering during debugging
        if (Debugger.IsAttached)
        {
            return;
        }

        if (!_mongo.IsEnabled) return;

        entry.RunId = _runId;
        entry.TestId = testId;
        entry.Attempt = _testAttempts.TryGetValue(testId, out var attempt) ? attempt : 1;

        var buffer = _logBuffers.GetOrAdd(testId, _ => []);
        lock (buffer)
        {
            buffer.Add(entry);
            if (buffer.Count >= _options.LogBufferSize)
            {
                var toFlush = new List<LogEntryDocument>(buffer);
                buffer.Clear();
                var task = FlushLogBatchAsync(toFlush);
                var pending = _pendingFlushTasks.GetOrAdd(testId, _ => []);
                lock (pending) { pending.Add(task); }
            }
        }
    }

    public void RecordStep(string testId, StepRecord step)
    {
        // Skip MongoDB step recording during debugging
        if (Debugger.IsAttached)
        {
            return;
        }

        if (!_mongo.IsEnabled) return;

        var steps = _stepBuffers.GetOrAdd(testId, _ => []);
        lock (steps)
        {
            steps.Add(step);
        }
    }

    public void CompleteStep(string testId, string stepName, string status, long durationMs)
    {
        // Skip MongoDB step completion during debugging
        if (Debugger.IsAttached)
        {
            return;
        }

        if (!_mongo.IsEnabled) return;

        if (_stepBuffers.TryGetValue(testId, out var steps))
        {
            lock (steps)
            {
                for (var i = steps.Count - 1; i >= 0; i--)
                {
                    if (steps[i].Name == stepName && steps[i].Status == "Running")
                    {
                        steps[i].Status = status;
                        steps[i].DurationMs = durationMs;
                        break;
                    }
                }
            }
        }
    }

    public void AddArtifact(string testId, ArtifactInfo artifact, ArtifactType type)
    {
        // Skip MongoDB artifact recording during debugging
        if (Debugger.IsAttached)
        {
            return;
        }

        if (!_mongo.IsEnabled) return;

        var artifacts = _artifactBuffers.GetOrAdd(testId, _ => new ArtifactCollection());
        lock (artifacts)
        {
            switch (type)
            {
                case ArtifactType.Screenshot:
                    artifacts.Screenshots.Add(artifact);
                    break;
                case ArtifactType.DomSnapshot:
                    artifacts.DomSnapshots.Add(artifact);
                    break;
                case ArtifactType.ApiPayload:
                    artifacts.ApiPayloads.Add(artifact);
                    break;
                case ArtifactType.Email:
                    artifacts.Emails.Add(artifact);
                    break;
                case ArtifactType.AccessibilityReport:
                    artifacts.AccessibilityReports.Add(artifact);
                    break;
            }
        }
    }

    public async Task<string?> UploadAndAddArtifactAsync(string testId, string localPath, string name, ArtifactType type)
    {
        // Skip MongoDB artifact uploads during debugging
        if (Debugger.IsAttached)
        {
            return null;
        }

        if (!_mongo.IsEnabled) return null;

        string? s3Url = null;
        try
        {
            if (_s3.IsEnabled)
            {
                s3Url = await _s3.UploadFileAsync(localPath, _runId, testId).ConfigureAwait(false);
                if (s3Url != null)
                    Console.WriteLine($"[MongoReporting] Uploaded artifact to S3: {s3Url}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MongoReporting] S3 upload failed for {localPath}: {ex.GetType().Name}: {ex.Message}");
        }

        var artifact = new ArtifactInfo
        {
            Name = name,
            LocalPath = localPath,
            S3Url = s3Url,
            CapturedAt = DateTime.UtcNow
        };
        AddArtifact(testId, artifact, type);
        return s3Url;
    }

    public async Task<string?> UploadAndAddArtifactAsync(string testId, byte[] data, string fileName, string contentType, ArtifactType type)
    {
        if (!_mongo.IsEnabled) return null;

        string? s3Url = null;
        try
        {
            if (_s3.IsEnabled)
            {
                s3Url = await _s3.UploadBytesAsync(data, _runId, testId, fileName, contentType).ConfigureAwait(false);
                if (s3Url != null)
                    Console.WriteLine($"[MongoReporting] Uploaded artifact to S3 (in-memory): {s3Url}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MongoReporting] S3 bytes upload failed for {fileName}: {ex.GetType().Name}: {ex.Message}");
        }

        var artifact = new ArtifactInfo
        {
            Name = fileName,
            S3Url = s3Url,
            CapturedAt = DateTime.UtcNow
        };
        AddArtifact(testId, artifact, type);
        return s3Url;
    }

    public async Task SetIdentifiersAsync(string testId, TestContextData contextData)
    {
        // Skip MongoDB identifier updates during debugging
        if (Debugger.IsAttached)
        {
            return;
        }

        if (!_mongo.IsEnabled) return;

        try
        {
            var filter = Builders<TestRunDetailDocument>.Filter.Eq(t => t.RunId, _runId)
                       & Builders<TestRunDetailDocument>.Filter.Eq(t => t.TestId, testId);
            var update = Builders<TestRunDetailDocument>.Update
                .Set(t => t.Identifiers, new TestIdentifiers
                {
                    QuoteId = contextData.QuoteId,
                    FriendlyId = contextData.FriendlyId,
                    ExternalId = contextData.ExternalId,
                    ApplicantId = contextData.ApplicantId,
                    Source = contextData.Source
                })
                .Set(t => t.IdentifierHistory, contextData.IdentifierHistory
                    .Select(s => new IdentifierSnapshotRecord
                    {
                        Source = s.Source,
                        ExternalId = s.ExternalId,
                        FriendlyId = s.FriendlyId,
                        ApplicantId = s.ApplicantId,
                        QuoteId = s.QuoteId,
                        CapturedAt = s.CapturedAt
                    }).ToList());
            await _mongo.TestRunDetails.UpdateOneAsync(filter, update).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MongoReporting] SetIdentifiers FAILED: {ex.GetType().Name}: {ex.Message}");
        }
    }

    public async Task FlushLogsAsync(string testId)
    {
        // Skip MongoDB log flushing during debugging
        if (Debugger.IsAttached)
        {
            return;
        }

        if (!_mongo.IsEnabled)
        {
            Console.WriteLine($"[MongoReporting] FlushLogs skipped — MongoDB not enabled (testId: {testId})");
            return;
        }

        // Await any in-flight intermediate flushes for this test
        if (_pendingFlushTasks.TryRemove(testId, out var pending))
        {
            Task[] tasks;
            lock (pending) { tasks = pending.ToArray(); }
            if (tasks.Length > 0)
            {
                Console.WriteLine($"[MongoReporting] Awaiting {tasks.Length} pending flush task(s) for testId: {testId}");
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
        }

        // Flush any remaining buffered entries
        List<LogEntryDocument>? toFlush = null;

        if (_logBuffers.TryGetValue(testId, out var buffer))
        {
            lock (buffer)
            {
                if (buffer.Count > 0)
                {
                    toFlush = new List<LogEntryDocument>(buffer);
                    buffer.Clear();
                }
            }
        }

        var count = toFlush?.Count ?? 0;
        Console.WriteLine($"[MongoReporting] FlushLogs for testId: {testId} — {count} remaining entries to flush");

        if (toFlush is { Count: > 0 })
            await FlushLogBatchAsync(toFlush).ConfigureAwait(false);

        _logBuffers.TryRemove(testId, out _);
    }

    private async Task FlushLogBatchAsync(List<LogEntryDocument> entries)
    {
        // Skip MongoDB log batch flushing during debugging
        if (Debugger.IsAttached)
        {
            return;
        }

        try
        {
            await _mongo.Logs.InsertManyAsync(entries).ConfigureAwait(false);
            Console.WriteLine($"[MongoReporting] Successfully flushed {entries.Count} log entries to MongoDB");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MongoReporting] FlushLogs FAILED ({entries.Count} entries): {ex.GetType().Name}: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[MongoReporting] FlushLogs failed ({entries.Count} entries): {ex.GetType().Name}: {ex.Message}");
        }
    }

    private async Task EnsureRunSummaryAsync(string? metadataEnvironment = null)
    {
        // Skip MongoDB run summary initialization during debugging
        if (Debugger.IsAttached)
        {
            return;
        }

        if (_runSummaryInitialized) return;

        if (!await _runInitSemaphore.WaitAsync(TimeSpan.FromSeconds(15)).ConfigureAwait(false))
        {
            Console.WriteLine("[MongoReporting] EnsureRunSummary timed out waiting for semaphore — skipping");
            return;
        }
        try
        {
            if (_runSummaryInitialized) return;

            // Create indexes once per process
            await MongoIndexInitializer.EnsureIndexesAsync(_mongo).ConfigureAwait(false);

            var executionContext = ExecutionContext.Capture();

            var runName = CiCdContextProvider.ResolveRunName();
            var environment = !string.IsNullOrEmpty(metadataEnvironment)
                ? metadataEnvironment
                : CiCdContextProvider.ResolveEnvironment();

            var filter = Builders<RunSummaryDocument>.Filter.Eq(r => r.RunId, _runId);
            var update = Builders<RunSummaryDocument>.Update
                .SetOnInsert(r => r.RunId, _runId)
                .SetOnInsert(r => r.RunName, runName)
                .SetOnInsert(r => r.Environment, environment)
                .SetOnInsert(r => r.StartedAt, DateTime.UtcNow)
                .SetOnInsert(r => r.Status, RunStatus.Running)
                .SetOnInsert(r => r.CiCd, _cicd)
                .SetOnInsert(r => r.ExecutionContext, executionContext)
                .SetOnInsert(r => r.OrchestratorJobId, CiCdContextProvider.ResolveOrchestratorJobId());

            await _mongo.RunSummaries.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true }).ConfigureAwait(false);

            _runSummaryInitialized = true;
        }
        finally
        {
            _runInitSemaphore.Release();
        }
    }

    /// <param name="supersededOutcome">
    /// Outcome this test already contributed on an earlier [RetryOnFailure] attempt, or null when
    /// nothing was counted yet. Its bucket is decremented so a retried test is counted exactly
    /// once — previously every attempt incremented, which is how a run reported more
    /// passed+failed than it had tests.
    /// </param>
    private async Task UpdateRunSummaryCountersAsync(string testId, string outcome, TestFailureData? failure, long durationMs, string? supersededOutcome = null)
    {
        // Skip MongoDB run summary updates during debugging
        if (Debugger.IsAttached)
        {
            return;
        }

        var filter = Builders<RunSummaryDocument>.Filter.Eq(r => r.RunId, _runId);
        // InProgressTests is not decremented here — it is recomputed from the totals below.
        var updateBuilder = Builders<RunSummaryDocument>.Update
            .Set(r => r.CompletedAt, DateTime.UtcNow);

        switch (outcome)
        {
            case "Passed":
                updateBuilder = updateBuilder.Inc(r => r.Passed, 1);
                break;
            case "Failed":
                updateBuilder = updateBuilder.Inc(r => r.Failed, 1);
                break;
            case "Skipped":
                updateBuilder = updateBuilder.Inc(r => r.Skipped, 1);
                break;
        }

        // Undo the increment the superseded attempt made, so the retried test occupies exactly
        // one bucket rather than one per attempt.
        switch (supersededOutcome)
        {
            case "Passed":
                updateBuilder = updateBuilder.Inc(r => r.Passed, -1);
                break;
            case "Failed":
                updateBuilder = updateBuilder.Inc(r => r.Failed, -1);
                break;
            case "Skipped":
                updateBuilder = updateBuilder.Inc(r => r.Skipped, -1);
                break;
        }

        // Drop the summary entry the superseded attempt pushed. Without this, a test that failed
        // and then passed on retry stayed listed in Failures forever — the row every triage
        // surface reads — even though the run no longer counts it as a failure.
        //
        // Its OWN update, deliberately, and issued BEFORE the one below: MongoDB refuses an
        // update document that touches the same path twice (ConflictingUpdateOperators), so
        // folding this $pull into a builder that also $pushes Failures threw for the commonest
        // retry case of all — a test that fails, retries, and fails again — and took the entire
        // summary write down with it, leaving the run stuck on Running.
        //
        // Keyed on testId alone: it is unique per test within a run (see the unique index on
        // runId+testId), unlike testName, which two fixtures may share when neither test
        // carries a [TestCaseId].
        if (supersededOutcome is "Failed" or "Skipped")
        {
            var pullUpdate = supersededOutcome == "Failed"
                ? Builders<RunSummaryDocument>.Update.PullFilter(r => r.Failures,
                    Builders<FailureSummary>.Filter.Eq(f => f.TestId, testId))
                : Builders<RunSummaryDocument>.Update.PullFilter(r => r.SkippedTests,
                    Builders<SkipSummary>.Filter.Eq(s => s.TestId, testId));

            await _mongo.RunSummaries.UpdateOneAsync(filter, pullUpdate).ConfigureAwait(false);
        }

        // Read the test detail to get metadata for failure/skip summaries. The
        // condition must match the two consumers below exactly: a skipped test also
        // carries failure data (to hold its skip reason), so keying off `failure !=
        // null` alone spent a Mongo round-trip per inconclusive test and discarded it.
        TestRunDetailDocument? testDetail = null;
        if ((outcome == "Failed" && failure != null) || outcome == "Skipped")
        {
            var testDetailFilter = Builders<TestRunDetailDocument>.Filter.Eq(t => t.RunId, _runId)
                                  & Builders<TestRunDetailDocument>.Filter.Eq(t => t.TestId, testId);
            testDetail = await _mongo.TestRunDetails
                .Find(testDetailFilter)
                .FirstOrDefaultAsync().ConfigureAwait(false);
        }

        // Gate on the outcome, not on the presence of failure data: a skipped or
        // inconclusive test carries a TestFailureData purely to hold its skip reason
        // (see TestMetadataResolver.DetectOutcome). Keying off `failure != null` alone
        // wrote that test into Failures as well as SkippedTests, so every surface
        // reading Failures reported a test that never ran as a failure.
        if (outcome == "Failed" && failure != null && testDetail != null)
        {
            var failureSummary = new FailureSummary
            {
                TestId = testId,
                TestName = testDetail.TestName,
                TestCaseId = testDetail.TestCaseId,
                ErrorMessage = failure.CombinedMessage,
                Category = testDetail.Categories.FirstOrDefault(),
                Tenant = testDetail.Tenant,
                DurationMs = durationMs
            };

            updateBuilder = updateBuilder.Push(r => r.Failures, failureSummary);
        }

        if (outcome == "Skipped" && testDetail != null)
        {
            var skipSummary = new SkipSummary
            {
                TestId = testId,
                TestName = testDetail.TestName,
                TestCaseId = testDetail.TestCaseId,
                Reason = failure?.CombinedMessage,
                Category = testDetail.Categories.FirstOrDefault(),
                Tenant = testDetail.Tenant
            };

            updateBuilder = updateBuilder.Push(r => r.SkippedTests, skipSummary);
        }

        // Recalculate pass rate
        await _mongo.RunSummaries.UpdateOneAsync(filter, updateBuilder).ConfigureAwait(false);

        // Update pass rate and status in a second query (needs current totals)
        var summary = await _mongo.RunSummaries
            .Find(filter)
            .FirstOrDefaultAsync().ConfigureAwait(false);

        if (summary != null)
        {
            var executed = summary.Passed + summary.Failed;
            var passRate = executed > 0
                ? Math.Round((double)summary.Passed / executed * 100, 2)
                : 0;

            var completedTests = summary.Passed + summary.Failed + summary.Skipped;

            // Derived, not incremented in one place and decremented in another. Balancing it by
            // hand meant one miscount anywhere left the number wrong for the rest of the run: a
            // [RetryOnFailure] test completing twice while starting only once (its retry insert
            // is refused by the unique index on runId+testId) drove it to -1, and a lost outcome
            // write left a finished run stuck on Running. "Started but not yet finished" is
            // exactly this subtraction, so recomputing makes every write self-correcting.
            var inProgress = Math.Max(0, summary.TotalTests - completedTests);
            var isRunComplete = inProgress == 0 && completedTests > 0;

            var status = isRunComplete
                ? (summary.Failed > 0 ? RunStatus.CompletedWithFailures : RunStatus.Completed)
                : RunStatus.Running;

            var passRateUpdate = Builders<RunSummaryDocument>.Update
                .Set(r => r.InProgressTests, inProgress)
                .Set(r => r.PassRate, passRate)
                .Set(r => r.Status, status);

            if (isRunComplete)
            {
                var durationTotal = (long)(DateTime.UtcNow - summary.StartedAt).TotalMilliseconds;
                passRateUpdate = passRateUpdate
                    .Set(r => r.DurationMs, durationTotal)
                    .Set(r => r.PlannedTests, summary.TotalTests);
            }

            await _mongo.RunSummaries.UpdateOneAsync(filter, passRateUpdate).ConfigureAwait(false);
        }
    }
}
