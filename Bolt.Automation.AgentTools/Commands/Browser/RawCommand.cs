using Bolt.Automation.AgentTools.Browser.Client;
using CommandLine;

namespace Bolt.Automation.AgentTools.Commands.Browser;

[Verb("raw", HelpText = "Execute one raw Playwright action on the live session — the replay primitive for recorded exploratory steps whose fields are not (yet) in the FieldRegistry.")]
internal sealed class RawOptions
{
    [Option("session", Required = true, HelpText = "Live session id.")]
    public string Session { get; set; } = "";

    [Option("action", Required = true, HelpText = "click | dblclick | fill | check | uncheck | select | press | type | hover.")]
    public string Action { get; set; } = "";

    [Option("selector", Required = true, HelpText = "Playwright selector-engine string (css, text=, role=, #id). XPath is rejected.")]
    public string Selector { get; set; } = "";

    [Option("value", HelpText = "Value for fill/select, or key for press (e.g. Enter).")]
    public string? Value { get; set; }

    [Option("timeout", Default = 30, HelpText = "Max seconds for the action before raw_timeout. Default 30.")]
    public int TimeoutSeconds { get; set; } = 30;
}

internal static class RawCommand
{
    public static async Task<int> ExecuteAsync(RawOptions o)
    {
        var payload = new
        {
            session = o.Session,
            action = o.Action,
            selector = o.Selector,
            value = o.Value,
            timeout_seconds = o.TimeoutSeconds,
        };

        var result = await HostClient.PostAsync("raw", payload, TimeSpan.FromSeconds(o.TimeoutSeconds + 60));
        return BrowserClientOutput.Emit(result);
    }
}
