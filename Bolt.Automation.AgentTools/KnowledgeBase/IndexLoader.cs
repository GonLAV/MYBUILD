using Bolt.Automation.AgentTools.KnowledgeBase.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Bolt.Automation.AgentTools.KnowledgeBase;

/// <summary>
/// Loads (and caches in-process) <c>index.yml</c> via YamlDotNet.
/// One instance per CLI invocation — the cache is process-lifetime only.
/// </summary>
internal sealed class IndexLoader
{
    private readonly string _kbRoot;
    private List<TopicRecord>? _topics;
    private Dictionary<string, TopicRecord>? _byKey;

    public IndexLoader(string kbRoot) => _kbRoot = kbRoot;

    public IReadOnlyList<TopicRecord> All
    {
        get { EnsureLoaded(); return _topics!; }
    }

    public TopicRecord? Get(string topicKey)
    {
        EnsureLoaded();
        return _byKey!.TryGetValue(topicKey, out var t) ? t : null;
    }

    private void EnsureLoaded()
    {
        if (_topics != null) return;

        var indexPath = Path.Combine(_kbRoot, "index.yml");
        var yaml = File.ReadAllText(indexPath);

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        var doc = deserializer.Deserialize<IndexDocument?>(yaml);
        _topics = doc?.Topics?
            .Where(t => !string.IsNullOrWhiteSpace(t.Topic))
            .Select(t => new TopicRecord(
                Topic: t.Topic,
                Summary: t.Summary ?? string.Empty,
                Primary: t.Primary ?? string.Empty,
                Related: (IReadOnlyList<string>)(t.Related ?? new List<string>())))
            .ToList() ?? new List<TopicRecord>();

        _byKey = _topics.ToDictionary(t => t.Topic, StringComparer.Ordinal);
    }

    // ---- YAML binding shapes ------------------------------------------------

    private sealed class IndexDocument
    {
        public int Version { get; set; }
        public string? Generated { get; set; }
        public string? Notes { get; set; }
        public List<TopicYaml>? Topics { get; set; }
    }

    private sealed class TopicYaml
    {
        public string Topic { get; set; } = string.Empty;
        public string? Summary { get; set; }
        public string? Primary { get; set; }
        public List<string>? Related { get; set; }
    }
}
