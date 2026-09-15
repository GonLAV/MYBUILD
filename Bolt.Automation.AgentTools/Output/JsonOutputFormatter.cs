using System.Text.Json;

namespace Bolt.Automation.AgentTools.Output;

public sealed class JsonOutputFormatter : IOutputFormatter
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    public string Format(object payload)
        => JsonSerializer.Serialize(payload, Options);
}
