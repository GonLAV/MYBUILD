using System.Text.RegularExpressions;

namespace Bolt.Automation.AgentTools.KnowledgeBase.Search;

/// <summary>
/// Splits text into lowercase tokens (alphanumeric runs ≥ 2 chars),
/// dropping a small English stopword list. Used by the KB indexer and
/// query-side parsing — same rules on both sides so query tokens match
/// indexed tokens deterministically.
/// </summary>
internal static class Tokenizer
{
    private static readonly Regex Splitter = new(@"[^A-Za-z0-9]+", RegexOptions.Compiled);

    // Splits "FieldRegistry" → "Field", "Registry"; "VINDecode" → "VIN", "Decode";
    // "HTMLDocument" → "HTML", "Document"; "ID" stays one token.
    private static readonly Regex CamelCaseSplit = new(
        @"(?<=[a-z0-9])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])",
        RegexOptions.Compiled);

    private static readonly HashSet<string> Stopwords = new(StringComparer.OrdinalIgnoreCase)
    {
        "the", "a", "an", "and", "or", "of", "to", "in", "for", "is", "are",
        "was", "were", "be", "by", "with", "from", "on", "at", "this", "that",
        "it", "as", "if", "but", "not", "all", "any", "no", "so", "do", "does",
        "did", "has", "have", "had", "you", "your", "we", "our", "i",
        "md", // .md filename suffix shouldn't earn a match
    };

    public static void AddTokens(HashSet<string> set, string? text)
    {
        if (string.IsNullOrEmpty(text)) return;
        foreach (var raw in Splitter.Split(text))
        {
            if (raw.Length == 0) continue;

            // Add the token whole (e.g. "FieldRegistry" itself).
            AddOne(set, raw);

            // Plus CamelCase splits when present, so query "Registry" can match
            // an indexed "FieldRegistry" identifier and vice versa.
            var parts = CamelCaseSplit.Split(raw);
            if (parts.Length > 1)
            {
                foreach (var p in parts) AddOne(set, p);
            }
        }
    }

    private static void AddOne(HashSet<string> set, string token)
    {
        if (token.Length < 2) return;
        var lower = token.ToLowerInvariant();
        if (Stopwords.Contains(lower)) return;
        set.Add(lower);
    }

    public static HashSet<string> Tokenize(string? text)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        AddTokens(set, text);
        return set;
    }
}
