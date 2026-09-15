namespace Bolt.Automation.AgentTools.KnowledgeBase.Models;

/// <summary>One ranked hit returned by <c>kb search</c>.</summary>
public sealed record SearchHit(
    string Topic,
    string File,
    int Score,
    string Snippet);
