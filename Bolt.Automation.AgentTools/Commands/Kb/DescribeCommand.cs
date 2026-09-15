using Bolt.Automation.AgentTools.KnowledgeBase;
using Bolt.Automation.AgentTools.KnowledgeBase.Models;
using CommandLine;

namespace Bolt.Automation.AgentTools.Commands.Kb;

[Verb("describe", HelpText = "Return a KB file's frontmatter summary and outline.")]
internal sealed class DescribeOptions
{
    [Option("file", Required = true, HelpText = "KB-relative path, e.g. 'framework/overview.md'.")]
    public string File { get; set; } = string.Empty;
}

internal static class DescribeCommand
{
    public static Task<int> ExecuteAsync(DescribeOptions options)
    {
        string kbRoot;
        try { kbRoot = KnowledgeBaseLocator.Resolve(); }
        catch (Exception ex)
        {
            return CommandBase.EmitErrorAsync("kb_root_not_found", ex.Message, exitCode: 2);
        }

        var resolver = new LeafResolver(kbRoot);
        var frontmatter = new FrontmatterParser();

        var kbRelative = options.File.Replace('\\', '/').TrimStart('/');
        var abs = resolver.ToAbsolute(kbRelative);
        if (!resolver.IsUnderKbRoot(abs))
        {
            return CommandBase.EmitErrorAsync(
                "input_error",
                "Path must be relative to the KB root (Documentation/agent-knowledge/).",
                exitCode: 3,
                detail: new { file = options.File });
        }
        if (!System.IO.File.Exists(abs))
        {
            return CommandBase.EmitJsonAsync(new
            {
                status = "not_found",
                file = kbRelative,
                path = abs,
            }, exitCode: 2);
        }

        var content = System.IO.File.ReadAllText(abs);
        var (fm, body) = frontmatter.Parse(content);

        var headings = ExtractHeadings(body);
        string? topic = null, summary = null, statusField = null;
        if (fm != null)
        {
            fm.TryGetValue("topic", out topic);
            fm.TryGetValue("summary", out summary);
            fm.TryGetValue("status", out statusField);
        }

        return CommandBase.EmitJsonAsync(new
        {
            file = kbRelative,
            path = abs,
            topic,
            summary,
            frontmatter_status = statusField,
            heading_count = headings.Count,
            headings,
        }, exitCode: 0);
    }

    private static IReadOnlyList<LeafHeading> ExtractHeadings(string body)
    {
        var result = new List<LeafHeading>();
        var inFence = false;
        foreach (var rawLine in body.Replace("\r\n", "\n").Split('\n'))
        {
            var line = rawLine.TrimEnd();
            if (line.StartsWith("```", StringComparison.Ordinal))
            {
                inFence = !inFence;
                continue;
            }
            if (inFence) continue;
            if (!line.StartsWith('#')) continue;

            var level = 0;
            while (level < line.Length && line[level] == '#') level++;
            if (level < 1 || level > 6) continue;
            if (level >= line.Length || line[level] != ' ') continue;
            var text = line[(level + 1)..].Trim();
            if (string.IsNullOrEmpty(text)) continue;
            result.Add(new LeafHeading(level, text));
        }
        return result;
    }
}
