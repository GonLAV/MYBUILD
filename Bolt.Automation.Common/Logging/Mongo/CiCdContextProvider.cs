using MongoDB.Bson;
using MongoDB.Driver;

namespace Bolt.Automation.Common.Logging.Mongo;

public static class CiCdContextProvider
{
    private static readonly Lazy<CiCdInfo> _cached = new(Resolve);
    private static string? _localRunId;
    private static readonly object _localRunIdLock = new();

    public static CiCdInfo Current => _cached.Value;

    private static CiCdInfo Resolve() => new()
    {
        BuildId = System.Environment.GetEnvironmentVariable("BUILD_BUILDID"),
        BuildUrl = System.Environment.GetEnvironmentVariable("BUILD_BUILDURI"),
        Branch = System.Environment.GetEnvironmentVariable("BUILD_SOURCEBRANCH"),
        CommitSha = System.Environment.GetEnvironmentVariable("BUILD_SOURCEVERSION"),
        TriggeredBy = System.Environment.GetEnvironmentVariable("BUILD_REQUESTEDFOR")
                      ?? System.Environment.UserName,
        TriggerType = System.Environment.GetEnvironmentVariable("BUILD_REASON")
                      ?? "Manual",
        AgentName = System.Environment.GetEnvironmentVariable("AGENT_NAME")
                    ?? System.Environment.MachineName,
        PipelineName = System.Environment.GetEnvironmentVariable("BUILD_DEFINITIONNAME"),
    };

    /// <summary>
    /// Returns the ORCHESTRATOR_JOB_ID env var if present, or null.
    /// Always available regardless of which value wins runId priority.
    /// </summary>
    public static string? ResolveOrchestratorJobId() =>
        System.Environment.GetEnvironmentVariable("ORCHESTRATOR_JOB_ID");

    /// <summary>
    /// Returns the ORCHESTRATOR_WORK_ITEM_ID env var if present, or null.
    /// Injected by the worker agent per work item execution.
    /// </summary>
    public static string? ResolveOrchestratorWorkItemId() =>
        System.Environment.GetEnvironmentVariable("ORCHESTRATOR_WORK_ITEM_ID");

    public static string ResolveRunId(IMongoContext? mongo = null)
    {
        // Priority 1: Azure DevOps CI build ID
        var buildId = System.Environment.GetEnvironmentVariable("BUILD_BUILDID");
        if (!string.IsNullOrEmpty(buildId))
            return buildId;

        // Priority 2: Orchestrator job ID — all workers for one job share this runId
        var orchestratorJobId = ResolveOrchestratorJobId();
        if (!string.IsNullOrEmpty(orchestratorJobId))
            return orchestratorJobId;

        // Priority 3: Local runs — sequential L-XXXXX, stable for the process lifetime
        if (_localRunId != null)
            return _localRunId;

        lock (_localRunIdLock)
        {
            if (_localRunId != null)
                return _localRunId;

            _localRunId = ResolveNextLocalRunId(mongo);
            return _localRunId;
        }
    }

    /// <summary>
    /// Resolves a human-readable run name.
    /// CI/CD: built from pipeline name and filter variables (tenant, category).
    /// Local: "Local Run".
    /// </summary>
    public static string ResolveRunName()
    {
        // Explicit override takes priority
        var explicit_ = System.Environment.GetEnvironmentVariable("RUN_NAME")
                       ?? System.Environment.GetEnvironmentVariable("TEST_RUN_NAME");
        if (!string.IsNullOrEmpty(explicit_))
            return explicit_;

        // CI/CD: build a descriptive name from pipeline + filters
        var pipeline = System.Environment.GetEnvironmentVariable("BUILD_DEFINITIONNAME");
        if (!string.IsNullOrEmpty(pipeline))
        {
            var parts = new List<string> { pipeline };

            var tenant = System.Environment.GetEnvironmentVariable("TEST_TENANT");
            if (!string.IsNullOrEmpty(tenant))
                parts.Add(tenant);

            var category = System.Environment.GetEnvironmentVariable("TEST_CATEGORY");
            if (!string.IsNullOrEmpty(category))
                parts.Add(category);

            var filter = System.Environment.GetEnvironmentVariable("TEST_FILTER");
            if (!string.IsNullOrEmpty(filter))
                parts.Add(filter);

            return string.Join(" | ", parts);
        }

        return "Local Run";
    }

    /// <summary>
    /// Resolves the primary environment name from standard environment variables.
    /// </summary>
    public static string ResolveEnvironment()
    {
        return System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? System.Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? System.Environment.GetEnvironmentVariable("ENVIRONMENT")
            ?? System.Environment.GetEnvironmentVariable("ENV")
            ?? "Unknown";
    }

    /// <summary>
    /// Atomically increments a counter document in the "counters" collection
    /// to produce a unique sequential local run ID (L-00001, L-00002, ...).
    /// Uses FindOneAndUpdate with $inc — safe for concurrent use by multiple
    /// team members running local tests at the same time.
    /// </summary>
    private static string ResolveNextLocalRunId(IMongoContext? mongo)
    {
        if (mongo?.IsEnabled != true)
            return $"L-{DateTime.UtcNow:HHmmss}";

        try
        {
            var database = mongo.RunSummaries.Database;
            var counters = database.GetCollection<BsonDocument>("counters");

            var filter = Builders<BsonDocument>.Filter.Eq("_id", "localRunId");
            var update = Builders<BsonDocument>.Update.Inc("sequence", 1);
            var options = new FindOneAndUpdateOptions<BsonDocument>
            {
                IsUpsert = true,
                ReturnDocument = ReturnDocument.After
            };

            var result = counters.FindOneAndUpdate(filter, update, options);
            var sequence = result["sequence"].AsInt32;
            return $"L-{sequence:D5}";
        }
        catch
        {
            return $"L-{DateTime.UtcNow:HHmmss}";
        }
    }
}
