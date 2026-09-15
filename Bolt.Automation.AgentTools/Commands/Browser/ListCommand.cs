using Bolt.Automation.AgentTools.Browser.Client;
using CommandLine;

namespace Bolt.Automation.AgentTools.Commands.Browser;

[Verb("list", HelpText = "List active browser sessions hosted by nexus-agent.")]
internal sealed class ListOptions
{
}

internal static class ListCommand
{
    public static async Task<int> ExecuteAsync(ListOptions _)
    {
        var result = await HostClient.PostAsync("list", new { }, TimeSpan.FromSeconds(30));
        return BrowserClientOutput.Emit(result);
    }
}
