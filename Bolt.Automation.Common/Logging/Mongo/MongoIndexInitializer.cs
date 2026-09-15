using MongoDB.Driver;

namespace Bolt.Automation.Common.Logging.Mongo;

public static class MongoIndexInitializer
{
    private static bool _initialized;
    private static readonly SemaphoreSlim _semaphore = new(1, 1);

    public static async Task EnsureIndexesAsync(IMongoContext context)
    {
        if (!context.IsEnabled || _initialized) return;

        if (!await _semaphore.WaitAsync(TimeSpan.FromSeconds(15)).ConfigureAwait(false))
        {
            Console.WriteLine("[MongoReporting] EnsureIndexes timed out waiting for semaphore — skipping");
            return;
        }
        try
        {
            if (_initialized) return;

            await DropStaleIndexes(context);
            await CreateRunSummaryIndexes(context);
            await CreateTestRunDetailIndexes(context);
            await CreateLogIndexes(context);

            _initialized = true;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private static async Task DropStaleIndexes(IMongoContext context)
    {
        // Drop stale indexes that were replaced or whose key definitions changed.
        await DropStaleIndexesFromCollection(context.TestRunDetails, ["idx_testId"]);

        // idx_env_tenant_started was redefined: split parallel array index into separate indexes
        await DropStaleIndexesFromCollection(context.RunSummaries, ["idx_env_tenant_started"]);
    }

    private static async Task DropStaleIndexesFromCollection<T>(IMongoCollection<T> collection, string[] indexNames)
    {
        try
        {
            var existing = await collection.Indexes.ListAsync();
            var indexList = await existing.ToListAsync();
            foreach (var idx in indexList)
            {
                var name = idx.GetElement("name").Value.AsString;
                if (indexNames.Contains(name))
                {
                    await collection.Indexes.DropOneAsync(name);
                    Console.WriteLine($"[MongoReporting] Dropped stale index '{name}' from {collection.CollectionNamespace.CollectionName}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MongoReporting] DropStaleIndexes ({collection.CollectionNamespace.CollectionName}): {ex.Message}");
        }
    }

    private static async Task CreateRunSummaryIndexes(IMongoContext context)
    {
        var collection = context.RunSummaries;
        var indexes = new List<CreateIndexModel<RunSummaryDocument>>
        {
            new(Builders<RunSummaryDocument>.IndexKeys.Ascending(r => r.RunId),
                new CreateIndexOptions { Unique = true, Name = "idx_runId" }),

            new(Builders<RunSummaryDocument>.IndexKeys
                .Ascending(r => r.Environments)
                .Descending(r => r.StartedAt),
                new CreateIndexOptions { Name = "idx_env_started" }),

            new(Builders<RunSummaryDocument>.IndexKeys
                .Ascending(r => r.Tenants)
                .Descending(r => r.StartedAt),
                new CreateIndexOptions { Name = "idx_tenant_started" }),

            new(Builders<RunSummaryDocument>.IndexKeys.Descending(r => r.StartedAt),
                new CreateIndexOptions { Name = "idx_started" })
        };

        await collection.Indexes.CreateManyAsync(indexes);
    }

    private static async Task CreateTestRunDetailIndexes(IMongoContext context)
    {
        var collection = context.TestRunDetails;
        var indexes = new List<CreateIndexModel<TestRunDetailDocument>>
        {
            new(Builders<TestRunDetailDocument>.IndexKeys
                .Ascending(t => t.RunId)
                .Ascending(t => t.TestId),
                new CreateIndexOptions { Unique = true, Name = "idx_runId_testId" }),

            new(Builders<TestRunDetailDocument>.IndexKeys
                .Ascending(t => t.TestName)
                .Ascending(t => t.Environment)
                .Descending(t => t.StartedAt),
                new CreateIndexOptions { Name = "idx_testName_env_started" }),

            new(Builders<TestRunDetailDocument>.IndexKeys
                .Ascending(t => t.Outcome)
                .Ascending(t => t.RunId),
                new CreateIndexOptions { Name = "idx_outcome_runId" }),

            new(Builders<TestRunDetailDocument>.IndexKeys.Ascending(t => t.TestCaseId),
                new CreateIndexOptions { Name = "idx_testCaseId", Sparse = true }),

            new(Builders<TestRunDetailDocument>.IndexKeys.Ascending("analysis.failureFingerprint"),
                new CreateIndexOptions { Name = "idx_fingerprint", Sparse = true }),

            new(Builders<TestRunDetailDocument>.IndexKeys.Ascending("identifiers.quoteId"),
                new CreateIndexOptions { Name = "idx_quoteId", Sparse = true }),

            new(Builders<TestRunDetailDocument>.IndexKeys.Ascending("identifiers.friendlyId"),
                new CreateIndexOptions { Name = "idx_friendlyId", Sparse = true }),

            new(Builders<TestRunDetailDocument>.IndexKeys.Ascending("identifiers.externalId"),
                new CreateIndexOptions { Name = "idx_externalId", Sparse = true })
        };

        await collection.Indexes.CreateManyAsync(indexes);
    }

    private static async Task CreateLogIndexes(IMongoContext context)
    {
        var collection = context.Logs;
        var indexes = new List<CreateIndexModel<LogEntryDocument>>
        {
            new(Builders<LogEntryDocument>.IndexKeys
                .Ascending(l => l.TestId)
                .Ascending(l => l.Timestamp),
                new CreateIndexOptions { Name = "idx_testId_timestamp" }),

            new(Builders<LogEntryDocument>.IndexKeys
                .Ascending(l => l.RunId)
                .Ascending(l => l.Level),
                new CreateIndexOptions { Name = "idx_runId_level" }),

            new(Builders<LogEntryDocument>.IndexKeys.Ascending(l => l.Timestamp),
                new CreateIndexOptions { Name = "idx_ttl", ExpireAfter = TimeSpan.FromDays(30) })
        };

        await collection.Indexes.CreateManyAsync(indexes);
    }
}
