using System.Text;
using System.Text.Json;

namespace Bolt.Automation.AgentTools.KnowledgeBase.Search;

/// <summary>
/// Builds (and caches) the KB search index: for each <c>.md</c> leaf, a token
/// set covering the filename, topic key, summary, headings, and first body
/// paragraph, plus a 200-char snippet for result display.
///
/// Cache: <c>%TMP%\nexus-agent\kb-index.json</c>. Invalidates when:
///  - the cached <c>kb_root</c> differs from the resolved root,
///  - the cached leaf-file set differs from what's on disk,
///  - any leaf's last-write mtime exceeds the cache's <c>indexed_at</c>.
/// </summary>
internal sealed class SimpleIndexer
{
    private const string CacheDirName = "nexus-agent";
    private const string CacheFileName = "kb-index.json";

    // Bump when the on-disk cache shape or the tokenizer rules change, so old
    // caches are auto-invalidated without manual cleanup.
    private const int CacheSchemaVersion = 2;

    private readonly string _kbRoot;
    private readonly LeafResolver _resolver;
    private readonly FrontmatterParser _frontmatter;

    public SimpleIndexer(string kbRoot, LeafResolver resolver, FrontmatterParser frontmatter)
    {
        _kbRoot = Path.GetFullPath(kbRoot);
        _resolver = resolver;
        _frontmatter = frontmatter;
    }

    public IReadOnlyList<LeafIndexEntry> Build()
    {
        var cachePath = GetCachePath();
        var leaves = _resolver.EnumerateLeaves().OrderBy(s => s, StringComparer.Ordinal).ToList();

        if (TryLoadCache(cachePath, leaves, out var cached))
            return cached!;

        // Stamp the cache with the time BEFORE reading the leaves, so a leaf edited
        // mid-build (mtime > indexed_at) is re-indexed next run instead of being
        // captured pre-edit yet recorded as freshly indexed.
        var indexStart = DateTime.UtcNow;
        var entries = leaves.Select(BuildEntry).ToList();
        TrySaveCache(cachePath, entries, indexStart);
        return entries;
    }

    private LeafIndexEntry BuildEntry(string kbRelative)
    {
        var abs = _resolver.ToAbsolute(kbRelative);
        var content = File.ReadAllText(abs);
        var (fm, body) = _frontmatter.Parse(content);

        var (headings, firstParagraph) = ExtractHeadingsAndFirstParagraph(body);

        string? topic = null;
        string? summary = null;
        if (fm != null)
        {
            fm.TryGetValue("topic", out topic);
            fm.TryGetValue("summary", out summary);
        }

        var tokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        Tokenizer.AddTokens(tokens, kbRelative);
        Tokenizer.AddTokens(tokens, topic);
        Tokenizer.AddTokens(tokens, summary);
        foreach (var h in headings) Tokenizer.AddTokens(tokens, h);
        Tokenizer.AddTokens(tokens, firstParagraph);

        var snippet = firstParagraph ?? summary ?? string.Empty;
        if (snippet.Length > 200) snippet = snippet[..200].TrimEnd() + "...";

        return new LeafIndexEntry(
            file: kbRelative,
            topic: topic,
            summary: summary,
            headings: headings,
            snippet: snippet,
            tokens: tokens);
    }

    private static (IReadOnlyList<string> Headings, string? FirstParagraph) ExtractHeadingsAndFirstParagraph(string body)
    {
        var headings = new List<string>();
        string? firstParagraph = null;
        var sb = new StringBuilder();
        var inFence = false;

        foreach (var rawLine in body.Replace("\r\n", "\n").Split('\n'))
        {
            var line = rawLine.TrimEnd();

            if (line.StartsWith("```", StringComparison.Ordinal))
            {
                if (firstParagraph == null && sb.Length > 0)
                {
                    firstParagraph = sb.ToString().Trim();
                    sb.Clear();
                }
                inFence = !inFence;
                continue;
            }
            if (inFence) continue;

            if (line.StartsWith('#'))
            {
                if (firstParagraph == null && sb.Length > 0)
                {
                    firstParagraph = sb.ToString().Trim();
                    sb.Clear();
                }
                var level = 0;
                while (level < line.Length && line[level] == '#') level++;
                if (level >= 1 && level <= 6 && level < line.Length && line[level] == ' ')
                {
                    var text = line[(level + 1)..].Trim();
                    if (!string.IsNullOrEmpty(text)) headings.Add(text);
                }
                continue;
            }

            if (line.StartsWith('>'))
            {
                // Block-quote callout — skip; treat as paragraph terminator.
                if (firstParagraph == null && sb.Length > 0)
                {
                    firstParagraph = sb.ToString().Trim();
                    sb.Clear();
                }
                continue;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                if (firstParagraph == null && sb.Length > 0)
                {
                    firstParagraph = sb.ToString().Trim();
                    sb.Clear();
                }
                continue;
            }

            if (firstParagraph == null)
            {
                if (sb.Length > 0) sb.Append(' ');
                sb.Append(line);
            }
        }

        if (firstParagraph == null && sb.Length > 0)
            firstParagraph = sb.ToString().Trim();

        return (headings, firstParagraph);
    }

    // ---- Cache I/O ----------------------------------------------------------

    private static string GetCachePath()
    {
        var dir = Path.Combine(Path.GetTempPath(), CacheDirName);
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, CacheFileName);
    }

    private bool TryLoadCache(string cachePath, List<string> currentLeaves, out IReadOnlyList<LeafIndexEntry>? entries)
    {
        entries = null;
        if (!File.Exists(cachePath)) return false;

        try
        {
            var json = File.ReadAllText(cachePath);
            var cache = JsonSerializer.Deserialize<CacheShape>(json);
            if (cache == null || cache.Leaves == null) return false;

            if (cache.SchemaVersion != CacheSchemaVersion) return false;
            if (!string.Equals(cache.KbRoot, _kbRoot, StringComparison.OrdinalIgnoreCase))
                return false;

            // Same leaf set?
            var cachedFiles = cache.Leaves.Select(l => l.File).ToHashSet(StringComparer.Ordinal);
            if (cachedFiles.Count != currentLeaves.Count) return false;
            if (!currentLeaves.All(cachedFiles.Contains)) return false;

            // Any leaf newer than the cache?
            foreach (var leaf in currentLeaves)
            {
                var mtime = File.GetLastWriteTimeUtc(_resolver.ToAbsolute(leaf));
                if (mtime > cache.IndexedAt) return false;
            }

            entries = cache.Leaves.Select(l => new LeafIndexEntry(
                file: l.File,
                topic: l.Topic,
                summary: l.Summary,
                headings: l.Headings ?? new List<string>(),
                snippet: l.Snippet ?? string.Empty,
                tokens: new HashSet<string>(l.Tokens ?? new List<string>(), StringComparer.OrdinalIgnoreCase))).ToList();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void TrySaveCache(string cachePath, IReadOnlyList<LeafIndexEntry> entries, DateTime indexedAt)
    {
        try
        {
            var cache = new CacheShape
            {
                SchemaVersion = CacheSchemaVersion,
                KbRoot = _kbRoot,
                IndexedAt = indexedAt,
                Leaves = entries.Select(e => new CacheLeaf
                {
                    File = e.File,
                    Topic = e.Topic,
                    Summary = e.Summary,
                    Headings = e.Headings.ToList(),
                    Snippet = e.Snippet,
                    Tokens = e.Tokens.OrderBy(t => t, StringComparer.Ordinal).ToList(),
                }).ToList(),
            };
            File.WriteAllText(cachePath, JsonSerializer.Serialize(cache));
        }
        catch
        {
            // Cache writes are best-effort. A failure to persist must not break the command.
        }
    }

    private sealed class CacheShape
    {
        public int SchemaVersion { get; set; }
        public string KbRoot { get; set; } = string.Empty;
        public DateTime IndexedAt { get; set; }
        public List<CacheLeaf>? Leaves { get; set; }
    }

    private sealed class CacheLeaf
    {
        public string File { get; set; } = string.Empty;
        public string? Topic { get; set; }
        public string? Summary { get; set; }
        public List<string>? Headings { get; set; }
        public string? Snippet { get; set; }
        public List<string>? Tokens { get; set; }
    }
}
