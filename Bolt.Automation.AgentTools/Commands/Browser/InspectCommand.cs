using Bolt.Automation.AgentTools.Browser.Client;
using CommandLine;

namespace Bolt.Automation.AgentTools.Commands.Browser;

[Verb("inspect", HelpText = "Return the current DOM (scoped) for an active browser session.")]
internal sealed class InspectOptions
{
    [Option("session", Required = true, HelpText = "Session ID.")]
    public string Session { get; set; } = "";

    [Option("scope", Default = "form", HelpText = "DOM scope: form | page | all. Default: form.")]
    public string Scope { get; set; } = "form";
}

internal static class InspectCommand
{
    public static async Task<int> ExecuteAsync(InspectOptions o)
    {
        if (string.IsNullOrWhiteSpace(o.Session))
            return await CommandBase.EmitErrorAsync("input_error", "Pass --session <id>.", exitCode: 3);

        var payload = new { session = o.Session, scope = o.Scope };
        var result = await HostClient.PostAsync("inspect", payload, TimeSpan.FromSeconds(60));
        return BrowserClientOutput.Emit(result);
    }
}
