using Bolt.Automation.AgentTools.Browser.Client;
using CommandLine;

namespace Bolt.Automation.AgentTools.Commands.Browser;

[Verb("screenshot", HelpText = "Capture a screenshot of an active browser session.")]
internal sealed class ScreenshotOptions
{
    [Option("session", Required = true, HelpText = "Session ID returned by 'browser navigate'.")]
    public string Session { get; set; } = "";

    [Option("scope", Default = "viewport", HelpText = "Capture scope: full | viewport | element. Default: viewport.")]
    public string Scope { get; set; } = "viewport";

    [Option("selector", HelpText = "CSS selector when --scope element.")]
    public string? Selector { get; set; }
}

internal static class ScreenshotCommand
{
    public static async Task<int> ExecuteAsync(ScreenshotOptions o)
    {
        if (string.IsNullOrWhiteSpace(o.Session))
            return await CommandBase.EmitErrorAsync("input_error", "Pass --session <id>.", exitCode: 3);

        var payload = new { session = o.Session, scope = o.Scope, selector = o.Selector };
        var result = await HostClient.PostAsync("screenshot", payload, TimeSpan.FromSeconds(60));
        return BrowserClientOutput.Emit(result);
    }
}
