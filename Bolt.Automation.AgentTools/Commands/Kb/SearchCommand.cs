using Bolt.Automation.AgentTools.KnowledgeBase;
using Bolt.Automation.AgentTools.KnowledgeBase.Search;
using CommandLine;

namespace Bolt.Automation.AgentTools.Commands.Kb;

[Verb("search", HelpText = "Free-text search across KB files; returns top hits with snippets.")]
internal sealed class SearchOptions
{
    [Value(0, MetaName = "terms", HelpText = "Search terms (space-separated).")]
    public IEnumerable<string> Terms { get; set; } = Enumerable.Empty<string>();

    [Option("type", HelpText = "Restrict to a KB subtree: framework | domain | recipe | philosophy.")]
    public string? Type { get; set; }

    [Option("limit", Default = 5, HelpText = "Maximum number of hits to return (default 5).")]
    public int Limit { get; set; } = 5;
}

internal static class SearchCommand
{
    public static Task<int> ExecuteAsync(SearchOptions options)
    {
        var terms = (options.Terms ?? Enumerable.Empty<string>())
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .ToList();
        if (terms.Count == 0)
        {
            return CommandBase.EmitErrorAsync(
                "input_error",
                "Pass at least one search term, e.g. `nexus-agent kb search FieldRegistry`.",
                exitCode: 3);
        }

        string kbRoot;
        try { kbRoot = KnowledgeBaseLocator.Resolve(); }
        catch (Exception ex)
        {
            return CommandBase.EmitErrorAsync("kb_root_not_found", ex.Message, exitCode: 2);
        }

        var resolver = new LeafResolver(kbRoot);
        var frontmatter = new FrontmatterParser();
        var indexer = new SimpleIndexer(kbRoot, resolver, frontmatter);
        var index = indexer.Build();

        var searcher = new Searcher(index);
        var hits = searcher.Search(terms, options.Type, options.Limit);

        if (hits.Count == 0)
        {
            return CommandBase.EmitJsonAsync(new
            {
                status = "no_hits",
                query = string.Join(" ", terms),
                type = options.Type,
                indexed = index.Count,
            }, exitCode: 2);
        }

        return CommandBase.EmitJsonAsync(new
        {
            query = string.Join(" ", terms),
            type = options.Type,
            indexed = index.Count,
            hit_count = hits.Count,
            hits,
        }, exitCode: 0);
    }
}
