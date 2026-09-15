using Bolt.Automation.AgentTools.Browser.Client;
using CommandLine;

namespace Bolt.Automation.AgentTools.Commands.Browser;

[Verb("fill", HelpText = "Fill named FieldRegistry fields on the live session's current page — only the fields you name, no default-data injection.")]
internal sealed class FillOptions
{
    [Option("session", Required = true, HelpText = "Live session id (from `browser navigate` / `browser list`).")]
    public string Session { get; set; } = "";

    [Option("data", HelpText = "Path to a JSON file holding one flat object of field-name → value.")]
    public string? DataFile { get; set; }

    [Option("set", HelpText = "Inline Field=Value pairs (repeatable: --set Vin=123 ZipCode=33101). Override the data file on collision.")]
    public IEnumerable<string>? Set { get; set; }

    [Option("timeout", Default = 120, HelpText = "Max seconds for the whole fill before the host returns fill_timeout (browser left open). Default 120.")]
    public int TimeoutSeconds { get; set; } = 120;
}

internal static class FillCommand
{
    public static async Task<int> ExecuteAsync(FillOptions o)
    {
        var (data, error) = QaFormData.Load(o.DataFile, o.Set);
        if (error != null)
            return await CommandBase.EmitErrorAsync("input_error", error, exitCode: 3);
        if (data == null || data.Count == 0)
            return await CommandBase.EmitErrorAsync("input_error",
                "fill needs field data — pass --data <file.json> and/or --set Field=Value.", exitCode: 3);

        var payload = new
        {
            session = o.Session,
            data,
            timeout_seconds = o.TimeoutSeconds,
        };

        var result = await HostClient.PostAsync("fill", payload, TimeSpan.FromSeconds(o.TimeoutSeconds + 60));
        return BrowserClientOutput.Emit(result);
    }
}
