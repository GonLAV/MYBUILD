using CommandLine;

namespace Bolt.Automation.AgentTools.Commands.Failure;

internal static class FailureRouter
{
    public static Task<int> RunAsync(string[] args) =>
        Parser.Default
            .ParseArguments<SummarizeOptions, PageSourceOptions, ScreenshotOptions>(args)
            .MapResult(
                (SummarizeOptions o)  => SummarizeCommand.ExecuteAsync(o),
                (PageSourceOptions o) => PageSourceCommand.ExecuteAsync(o),
                (ScreenshotOptions o) => ScreenshotCommand.ExecuteAsync(o),
                _                     => Task.FromResult(3));
}
