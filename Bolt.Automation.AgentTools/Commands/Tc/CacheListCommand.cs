using Bolt.Automation.AgentTools.ApiClients;
using CommandLine;

namespace Bolt.Automation.AgentTools.Commands.Tc;

[Verb("cache-list", HelpText = "List cached test cases (also accepts 'tc cache list').")]
internal sealed class CacheListOptions
{
}

internal static class CacheListCommand
{
    public static Task<int> ExecuteAsync(CacheListOptions _)
    {
        var entries = new TcCache().List();
        return CommandBase.EmitJsonAsync(new
        {
            count = entries.Count,
            entries,
        }, exitCode: 0);
    }
}
