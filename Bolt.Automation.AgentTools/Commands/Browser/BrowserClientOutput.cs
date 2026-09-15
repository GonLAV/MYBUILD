using System.Text.Json;
using Bolt.Automation.AgentTools.Browser.Client;

namespace Bolt.Automation.AgentTools.Commands.Browser;

/// <summary>
/// Shared output mapping for browser client commands. The host is the single
/// source of JSON formatting (snake_case), so success bodies pass through to
/// stdout and error bodies to stderr — mirroring <see cref="CommandBase"/>'s
/// stdout/stderr + exit-code convention (0 success · 2 error).
/// </summary>
internal static class BrowserClientOutput
{
    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    public static int Emit(HostResult r)
    {
        if (r.TransportError != null)
        {
            Console.Error.WriteLine(JsonSerializer.Serialize(
                new { status = "host_unreachable", message = r.TransportError }, Json));
            return 2;
        }

        if (r.Ok) Console.Out.WriteLine(r.Json);
        else Console.Error.WriteLine(r.Json);
        return r.Ok ? 0 : 2;
    }
}
