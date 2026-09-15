using CommandLine;

namespace Bolt.Automation.AgentTools.Commands.Secrets;

internal static class SecretsRouter
{
    public static Task<int> RunAsync(string[] args) =>
        Parser.Default
            .ParseArguments<SyncOptions, StatusOptions>(args)
            .MapResult(
                (SyncOptions o)   => SyncCommand.ExecuteAsync(o),
                (StatusOptions o) => StatusCommand.ExecuteAsync(o),
                _                 => Task.FromResult(3));
}
