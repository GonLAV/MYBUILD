using CommandLine;

namespace Bolt.Automation.AgentTools.Commands.Browser;

internal static class BrowserRouter
{
    public static Task<int> RunAsync(string[] args) =>
        Parser.Default
            .ParseArguments<NavigateOptions, OpenOptions, OpenQuoteOptions, QuoteStartOptions, FillOptions, ContinueOptions, RecordOptions, ParseRecordingOptions, RawOptions, ScreenshotOptions, InspectOptions, PauseOptions, ResumeOptions, CloseOptions, ListOptions>(args)
            .MapResult(
                (NavigateOptions o)   => NavigateCommand.ExecuteAsync(o),
                (OpenOptions o)       => OpenCommand.ExecuteAsync(o),
                (OpenQuoteOptions o)  => OpenQuoteCommand.ExecuteAsync(o),
                (QuoteStartOptions o) => QuoteStartCommand.ExecuteAsync(o),
                (FillOptions o)       => FillCommand.ExecuteAsync(o),
                (ContinueOptions o)   => ContinueCommand.ExecuteAsync(o),
                (RecordOptions o)     => RecordCommand.ExecuteAsync(o),
                (ParseRecordingOptions o) => ParseRecordingCommand.ExecuteAsync(o),
                (RawOptions o)        => RawCommand.ExecuteAsync(o),
                (ScreenshotOptions o) => ScreenshotCommand.ExecuteAsync(o),
                (InspectOptions o)    => InspectCommand.ExecuteAsync(o),
                (PauseOptions o)      => PauseCommand.ExecuteAsync(o),
                (ResumeOptions o)     => ResumeCommand.ExecuteAsync(o),
                (CloseOptions o)      => CloseCommand.ExecuteAsync(o),
                (ListOptions o)       => ListCommand.ExecuteAsync(o),
                _                     => Task.FromResult(3));
}
