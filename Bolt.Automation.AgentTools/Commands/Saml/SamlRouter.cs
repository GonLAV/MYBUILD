using CommandLine;

namespace Bolt.Automation.AgentTools.Commands.Saml;

internal static class SamlRouter
{
    public static Task<int> RunAsync(string[] args) =>
        Parser.Default
            .ParseArguments<MintOptions>(args)
            .MapResult(
                (MintOptions o) => MintCommand.ExecuteAsync(o),
                _               => Task.FromResult(3));
}
