using Bolt.Automation.AgentTools.KnowledgeBase.Models;

namespace Bolt.Automation.AgentTools.KnowledgeBase.Search;

/// <summary>
/// Ranks leaves against a query by token overlap, with weighted boosts for
/// matches in the topic key, filename, and headings. Returns the top
/// <c>limit</c> hits (default 5) with a snippet from each leaf.
/// </summary>
internal sealed class Searcher
{
    // Scoring weights — chosen so that a single topic-key match outranks a
    // single body match by 3x, matching the plan's ranking intent.
    private const int WeightTopic   = 3;
    private const int WeightFile    = 2;
    private const int WeightHeading = 2;
    private const int WeightBody    = 1;

    private readonly IReadOnlyList<LeafIndexEntry> _entries;

    public Searcher(IReadOnlyList<LeafIndexEntry> entries) => _entries = entries;

    public IReadOnlyList<SearchHit> Search(IEnumerable<string> queryTerms, string? typeFilter = null, int limit = 5)
    {
        var queryTokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var term in queryTerms)
            Tokenizer.AddTokens(queryTokens, term);
        if (queryTokens.Count == 0) return Array.Empty<SearchHit>();

        var prefix = MapTypeToPrefix(typeFilter);
        var hits = new List<SearchHit>();
        foreach (var entry in _entries)
        {
            if (prefix.Length > 0 && !entry.File.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                continue;

            var topicTokens   = Tokenizer.Tokenize(entry.Topic);
            var fileTokens    = Tokenizer.Tokenize(entry.File);
            var headingTokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var h in entry.Headings) Tokenizer.AddTokens(headingTokens, h);

            var score = 0;
            foreach (var q in queryTokens)
            {
                var matched = false;
                if (topicTokens.Contains(q))   { score += WeightTopic;   matched = true; }
                if (fileTokens.Contains(q))    { score += WeightFile;    matched = true; }
                if (headingTokens.Contains(q)) { score += WeightHeading; matched = true; }
                if (!matched && entry.Tokens.Contains(q)) score += WeightBody;
            }
            if (score == 0) continue;

            hits.Add(new SearchHit(
                Topic: entry.Topic ?? string.Empty,
                File: entry.File,
                Score: score,
                Snippet: entry.Snippet));
        }

        return hits
            .OrderByDescending(h => h.Score)
            .ThenBy(h => h.File, StringComparer.Ordinal)
            .Take(Math.Max(1, limit)) // a non-positive --limit must not silently drop all hits
            .ToList();
    }

    private static string MapTypeToPrefix(string? type) => type?.ToLowerInvariant() switch
    {
        "framework"  => "framework/",
        "domain"     => "domain/",
        "recipe"     => "recipes/",
        "philosophy" => "philosophy/",
        _ => string.Empty,
    };
}
