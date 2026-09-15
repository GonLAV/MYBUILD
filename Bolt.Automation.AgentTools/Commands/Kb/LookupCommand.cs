using Bolt.Automation.AgentTools.KnowledgeBase;
using CommandLine;

namespace Bolt.Automation.AgentTools.Commands.Kb;

[Verb("lookup", HelpText = "Resolve a KB topic key to its leaf file + summary.")]
internal sealed class LookupOptions
{
    [Option("topic", Required = true, HelpText = "Topic key, e.g. 'framework:overview' or 'partner:KRAFTLAKEX'.")]
    public string Topic { get; set; } = string.Empty;
}

internal static class LookupCommand
{
    public static Task<int> ExecuteAsync(LookupOptions options)
    {
        string kbRoot;
        try { kbRoot = KnowledgeBaseLocator.Resolve(); }
        catch (Exception ex)
        {
            return CommandBase.EmitErrorAsync("kb_root_not_found", ex.Message, exitCode: 2);
        }

        var index = new IndexLoader(kbRoot);
        var record = index.Get(options.Topic);
        if (record == null)
        {
            return CommandBase.EmitJsonAsync(new
            {
                status = "not_found",
                topic = options.Topic,
                hint = "Try `nexus-agent kb search <terms>` or `kb describe --file <path>`.",
            }, exitCode: 2);
        }

        var resolver = new LeafResolver(kbRoot);
        var abs = resolver.ToAbsolute(record.Primary);
        var exists = resolver.Exists(record.Primary);

        return CommandBase.EmitJsonAsync(new
        {
            topic = record.Topic,
            summary = record.Summary,
            primary = record.Primary,
            path = abs,
            exists,
            related = record.Related,
        }, exitCode: 0);
    }
}
