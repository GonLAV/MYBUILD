using CommandLine;

namespace Bolt.Automation.AgentTools.Commands.Philosophy;

internal static class PhilosophyRouter
{
    public static Task<int> RunAsync(string[] args) =>
        Parser.Default
            .ParseArguments<LookupOptions>(args)
            .MapResult(
                (LookupOptions o) => LookupCommand.ExecuteAsync(o),
                _                 => Task.FromResult(3));
}
