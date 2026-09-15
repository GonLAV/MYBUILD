using YamlDotNet.Serialization;

namespace Bolt.Automation.AgentTools.KnowledgeBase;

/// <summary>
/// Parses YAML frontmatter from a markdown file: text between the first two
/// <c>---</c> fence lines (the opening fence MUST be the first line of the file).
/// Returns the parsed frontmatter as a string dictionary plus the markdown body
/// after the closing fence. If no frontmatter is present, returns
/// <c>(null, fileContent)</c>.
/// </summary>
internal sealed class FrontmatterParser
{
    public (IReadOnlyDictionary<string, string>? Frontmatter, string Body) Parse(string fileContent)
    {
        // Normalize line endings so the scanner doesn't care about CRLF vs LF.
        var content = fileContent.Replace("\r\n", "\n").Replace('\r', '\n');
        var lines = content.Split('\n');

        // Opening fence must be the first line.
        if (lines.Length == 0 || lines[0] != "---")
            return (null, fileContent);

        // Closing fence: the next line that is exactly "---". Scanning from line 1
        // means an empty frontmatter block (--- immediately followed by ---) parses
        // correctly (fmText = "") instead of being misread as having no frontmatter.
        var close = -1;
        for (var i = 1; i < lines.Length; i++)
        {
            if (lines[i] == "---") { close = i; break; }
        }
        if (close < 0) return (null, fileContent); // no closing fence

        var fmText = string.Join('\n', lines[1..close]);
        var body = close + 1 < lines.Length ? string.Join('\n', lines[(close + 1)..]) : string.Empty;

        try
        {
            var deserializer = new DeserializerBuilder().Build();
            var raw = deserializer.Deserialize<Dictionary<string, object?>?>(fmText);
            var stringDict = raw == null
                ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                : raw.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.ToString() ?? string.Empty,
                    StringComparer.OrdinalIgnoreCase);
            return (stringDict, body);
        }
        catch
        {
            // Malformed YAML in frontmatter → treat the whole file as body.
            return (null, fileContent);
        }
    }
}
