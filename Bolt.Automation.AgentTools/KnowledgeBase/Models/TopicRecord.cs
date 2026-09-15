namespace Bolt.Automation.AgentTools.KnowledgeBase.Models;

/// <summary>
/// One entry in the KB's <c>index.yml</c>. Mirrors the YAML schema —
/// keys remain lowercase / snake_case in JSON output via the serializer's
/// naming policy.
/// </summary>
public sealed record TopicRecord(
    string Topic,
    string Summary,
    string Primary,
    IReadOnlyList<string> Related);
