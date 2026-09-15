using Bolt.Automation.AgentTools.Browser.Client;
using CommandLine;

namespace Bolt.Automation.AgentTools.Commands.Browser;

[Verb("pause", HelpText = "Cooperative pause: persist session state, surface reason, return so the user can drive the browser manually.")]
internal sealed class PauseOptions
{
    [Option("session", Required = true, HelpText = "Session ID.")]
    public string Session { get; set; } = "";

    [Option("reason", Required = true, HelpText = "Human-readable description of what the user needs to do (shown back via AskUserQuestion).")]
    public string Reason { get; set; } = "";
}

internal static class PauseCommand
{
    public static async Task<int> ExecuteAsync(PauseOptions o)
    {
        if (string.IsNullOrWhiteSpace(o.Session))
            return await CommandBase.EmitErrorAsync("input_error", "Pass --session <id>.", exitCode: 3);
        if (string.IsNullOrWhiteSpace(o.Reason))
            return await CommandBase.EmitErrorAsync("input_error", "Pass --reason <text>.", exitCode: 3);

        var payload = new { session = o.Session, reason = o.Reason };
        var result = await HostClient.PostAsync("pause", payload, TimeSpan.FromSeconds(30));
        return BrowserClientOutput.Emit(result);
    }
}
