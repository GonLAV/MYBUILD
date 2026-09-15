using Bolt.Automation.AgentTools.Browser.Client;
using CommandLine;

namespace Bolt.Automation.AgentTools.Commands.Browser;

[Verb("close", HelpText = "Close a live session cleanly (dispose its browser and DI scope). Use --all to close every session.")]
internal sealed class CloseOptions
{
    [Option("session", HelpText = "Session id to close.")]
    public string? Session { get; set; }

    [Option("all", HelpText = "Close every live session.")]
    public bool All { get; set; }
}

internal static class CloseCommand
{
    public static async Task<int> ExecuteAsync(CloseOptions o)
    {
        if (string.IsNullOrWhiteSpace(o.Session) && !o.All)
            return await CommandBase.EmitErrorAsync("input_error", "close requires --session <id> or --all.", exitCode: 3);

        var result = await HostClient.PostAsync("close", new { session = o.Session, all = o.All });
        return BrowserClientOutput.Emit(result);
    }
}
