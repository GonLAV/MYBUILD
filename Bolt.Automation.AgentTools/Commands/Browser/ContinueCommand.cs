using Bolt.Automation.AgentTools.Browser.Client;
using CommandLine;

namespace Bolt.Automation.AgentTools.Commands.Browser;

[Verb("continue", HelpText = "Advance the live session exactly one page: click the current page's continue control and validate the next page when it is known.")]
internal sealed class ContinueOptions
{
    [Option("session", Required = true, HelpText = "Live session id (from `browser navigate` / `browser list`).")]
    public string Session { get; set; } = "";

    [Option("page", HelpText = "Current page type name (e.g. D2C_HouseDetailsPage). Only needed when the session doesn't know where it is (e.g. after open-quote).")]
    public string? Page { get; set; }

    [Option("expect", HelpText = "Page type name expected after the click; validated before returning. Defaults to the flow's next page when resolvable.")]
    public string? Expect { get; set; }

    [Option("timeout", Default = 90, HelpText = "Max seconds for click + next-page validation before the host returns continue_timeout (browser left open). Default 90.")]
    public int TimeoutSeconds { get; set; } = 90;
}

internal static class ContinueCommand
{
    public static async Task<int> ExecuteAsync(ContinueOptions o)
    {
        var payload = new
        {
            session = o.Session,
            page = o.Page,
            expect = o.Expect,
            timeout_seconds = o.TimeoutSeconds,
        };

        var result = await HostClient.PostAsync("continue", payload, TimeSpan.FromSeconds(o.TimeoutSeconds + 60));
        return BrowserClientOutput.Emit(result);
    }
}
