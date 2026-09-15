using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Bolt.Automation.Common.Logging.Mongo;

public class LogEntryDocument
{
    [BsonId]
    public ObjectId Id { get; set; }

    [BsonElement("runId")]
    public string RunId { get; set; } = string.Empty;

    [BsonElement("testId")]
    public string TestId { get; set; } = string.Empty;

    // Which [RetryOnFailure] attempt produced this entry — 1 for the first (and, for a test
    // that is not retried, only) run. Every attempt of a test writes its own log entries but
    // shares one runId+testId pair, so without this field the logs of a failed attempt and
    // those of the passing retry after it are indistinguishable and a green test renders full
    // of the previous attempt's errors.
    // Writer-side only: entries from a superseded attempt are kept, not deleted, so a reader
    // that wants just the winning attempt has to filter on max(attempt) itself.
    [BsonElement("attempt")]
    public int Attempt { get; set; } = 1;

    [BsonElement("timestamp")]
    public DateTime Timestamp { get; set; }

    [BsonElement("level")]
    public string Level { get; set; } = string.Empty;

    [BsonElement("category")]
    public string Category { get; set; } = "General";

    [BsonElement("message")]
    public string Message { get; set; } = string.Empty;

    [BsonElement("stepName")]
    public string? StepName { get; set; }

    [BsonElement("stepLevel")]
    public int? StepLevel { get; set; }

    [BsonElement("payload")]
    public BsonDocument? Payload { get; set; }

    [BsonElement("metadata")]
    public BsonDocument? Metadata { get; set; }
}
