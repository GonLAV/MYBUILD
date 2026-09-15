using CommandLine;

namespace Bolt.Automation.AgentTools.Commands.Kb;

internal static class KbRouter
{
    public static Task<int> RunAsync(string[] args) =>
        Parser.Default
            .ParseArguments<LookupOptions, SearchOptions, DescribeOptions>(args)
            .MapResult(
                (LookupOptions o)   => LookupCommand.ExecuteAsync(o),
                (SearchOptions o)   => SearchCommand.ExecuteAsync(o),
                (DescribeOptions o) => DescribeCommand.ExecuteAsync(o),
                _                   => Task.FromResult(3));
}
