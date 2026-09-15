using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Bolt.Automation.Common.Logging.Mongo;

public class CiCdInfo
{
    [BsonElement("buildId")]
    public string? BuildId { get; set; }

    [BsonElement("buildUrl")]
    public string? BuildUrl { get; set; }

    [BsonElement("branch")]
    public string? Branch { get; set; }

    [BsonElement("commitSha")]
    public string? CommitSha { get; set; }

    [BsonElement("triggeredBy")]
    public string? TriggeredBy { get; set; }

    [BsonElement("triggerType")]
    public string? TriggerType { get; set; }

    [BsonElement("agentName")]
    public string? AgentName { get; set; }

    [BsonElement("pipelineName")]
    public string? PipelineName { get; set; }

    public bool IsFromCiCd => !string.IsNullOrEmpty(BuildId);
}
