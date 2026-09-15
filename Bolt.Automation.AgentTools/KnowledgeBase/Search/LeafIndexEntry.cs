namespace Bolt.Automation.AgentTools.KnowledgeBase.Search;

/// <summary>
/// One entry in the in-memory KB search index. Built by <see cref="SimpleIndexer"/>
/// from a single leaf file; consumed by <see cref="Searcher"/>.
/// </summary>
internal sealed class LeafIndexEntry
{
    public string File { get; }
    public string? Topic { get; }
    public string? Summary { get; }
    public IReadOnlyList<string> Headings { get; }
    public string Snippet { get; }
    public HashSet<string> Tokens { get; }

    public LeafIndexEntry(
        string file,
        string? topic,
        string? summary,
        IReadOnlyList<string> headings,
        string snippet,
        HashSet<string> tokens)
    {
        File = file;
        Topic = topic;
        Summary = summary;
        Headings = headings;
        Snippet = snippet;
        Tokens = tokens;
    }
}
