using Bolt.Automation.AgentTools.Browser;
using CommandLine;

namespace Bolt.Automation.AgentTools.Commands.Browser;

[Verb("parse-recording", HelpText = "Parse a codegen recording (.cs from `browser record`) into a structured JSON action list — targets, values, ##...## verification markers, and replay-ready selector suggestions.")]
internal sealed class ParseRecordingOptions
{
    [Option("file", Required = true, HelpText = "Path to the recording .cs file.")]
    public string File { get; set; } = "";
}

internal static class ParseRecordingCommand
{
    public static async Task<int> ExecuteAsync(ParseRecordingOptions o)
    {
        if (!File.Exists(o.File))
            return await CommandBase.EmitErrorAsync("input_error", $"Recording file not found: {o.File}", exitCode: 3);

        var actions = RecordingParser.Parse(await File.ReadAllLinesAsync(o.File));

        return await CommandBase.EmitJsonAsync(new
        {
            file = Path.GetFullPath(o.File),
            action_count = actions.Count,
            marker_count = actions.Count(a => a.Action == "marker"),
            unparsed_count = actions.Count(a => a.Action == "unparsed"),
            actions,
        });
    }
}
