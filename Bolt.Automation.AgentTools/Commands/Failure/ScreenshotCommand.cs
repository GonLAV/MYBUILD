using Bolt.Automation.AgentTools.Failure;
using CommandLine;

namespace Bolt.Automation.AgentTools.Commands.Failure;

[Verb("screenshot", HelpText = "Return the path to the captured screenshot for a failed test, if any.")]
internal sealed class ScreenshotOptions
{
    [Option("test", Required = true, HelpText = "Fully-qualified test name (or bare method name).")]
    public string Test { get; set; } = string.Empty;
}

internal static class ScreenshotCommand
{
    public static Task<int> ExecuteAsync(ScreenshotOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Test))
            return CommandBase.EmitErrorAsync("input_error", "Pass --test <fqn>.", exitCode: 3);

        var artifacts = new ArtifactsLocator();
        var located = new PageSourceLocator(artifacts).Locate(options.Test);

        if (!located.Screenshot.Found || located.Screenshot.Path == null)
        {
            return CommandBase.EmitJsonAsync(new
            {
                status = "not_found",
                test = options.Test,
                method = located.Method,
                folder = located.FolderName,
                folder_found = located.FolderFound,
                suggestion = located.Suggestion,
                hint = "No screenshot (*_final_*.png) for this test. Run it locally or set NEXUS_TEST_RESULTS.",
            }, exitCode: 2);
        }

        return CommandBase.EmitJsonAsync(new
        {
            test = options.Test,
            method = located.Method,
            path = located.Screenshot.Path,
            captured_at = located.Screenshot.CapturedAt,
            bytes = located.Screenshot.Bytes,
        }, exitCode: 0);
    }
}
