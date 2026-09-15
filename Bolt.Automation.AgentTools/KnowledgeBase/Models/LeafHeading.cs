namespace Bolt.Automation.AgentTools.KnowledgeBase.Models;

/// <summary>A heading line in a KB markdown leaf (level = 1..6, the # count).</summary>
public sealed record LeafHeading(int Level, string Text);
