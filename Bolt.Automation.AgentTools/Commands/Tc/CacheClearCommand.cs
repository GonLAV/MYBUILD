using Bolt.Automation.AgentTools.ApiClients;
using CommandLine;

namespace Bolt.Automation.AgentTools.Commands.Tc;

[Verb("cache-clear", HelpText = "Clear cached test cases (also accepts 'tc cache clear').")]
internal sealed class CacheClearOptions
{
    [Option("older-than", HelpText = "Drop only entries older than this duration (e.g. '7d', '24h', '30m').")]
    public string? OlderThan { get; set; }
}

internal static class CacheClearCommand
{
    public static Task<int> ExecuteAsync(CacheClearOptions options)
    {
        if (!TcCache.TryParseDuration(options.OlderThan, out var olderThan))
        {
            return CommandBase.EmitErrorAsync(
                "input_error",
                "Could not parse --older-than. Use a number + unit, e.g. '7d', '24h', '30m', '90s'.",
                exitCode: 3,
                detail: new { older_than = options.OlderThan });
        }

        var (removed, remaining) = new TcCache().Clear(olderThan);
        return CommandBase.EmitJsonAsync(new
        {
            status = "cleared",
            older_than = options.OlderThan,
            removed,
            remaining,
        }, exitCode: 0);
    }
}
