using Bolt.Automation.AgentTools.Browser.Client;
using CommandLine;

namespace Bolt.Automation.AgentTools.Commands.Browser;

[Verb("resume", HelpText = "Resume a paused session so subsequent screenshot/inspect calls proceed.")]
internal sealed class ResumeOptions
{
    [Option("session", Required = true, HelpText = "Session ID to resume.")]
    public string Session { get; set; } = "";
}

internal static class ResumeCommand
{
    public static async Task<int> ExecuteAsync(ResumeOptions o)
    {
        if (string.IsNullOrWhiteSpace(o.Session))
            return await CommandBase.EmitErrorAsync("input_error", "Pass --session <id>.", exitCode: 3);

        var payload = new { session = o.Session };
        var result = await HostClient.PostAsync("resume", payload, TimeSpan.FromSeconds(30));
        return BrowserClientOutput.Emit(result);
    }
}
