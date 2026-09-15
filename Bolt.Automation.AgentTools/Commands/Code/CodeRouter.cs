using CommandLine;

namespace Bolt.Automation.AgentTools.Commands.Code;

internal static class CodeRouter
{
    public static Task<int> RunAsync(string[] args) =>
        Parser.Default
            .ParseArguments<FindSimilarOptions, FieldLookupOptions, FlowTraceOptions, DiffImpactOptions, WipStopOptions>(args)
            .MapResult(
                (FindSimilarOptions o) => FindSimilarCommand.ExecuteAsync(o),
                (FieldLookupOptions o) => FieldLookupCommand.ExecuteAsync(o),
                (FlowTraceOptions o)   => FlowTraceCommand.ExecuteAsync(o),
                (DiffImpactOptions o)  => DiffImpactCommand.ExecuteAsync(o),
                (WipStopOptions o)     => WipStopCommand.ExecuteAsync(o),
                _                      => Task.FromResult(3));
}
