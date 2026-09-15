using System.Text.Json;

namespace Bolt.Automation.AgentTools.Commands.Browser;

/// <summary>
/// Parses QA-supplied field data for <c>browser fill</c> / <c>browser navigate --data</c>:
/// a JSON file holding one flat object of field-name → value, and/or repeatable
/// <c>--set Field=Value</c> pairs (pairs win on collision). Values may be JSON strings,
/// numbers, or booleans — coerced to strings, since <c>FieldRegistry</c> interactions
/// are string-typed. Field-name case is preserved.
/// </summary>
internal static class QaFormData
{
    /// <returns>The merged ordered data, or an error message (data null) on bad input.</returns>
    public static (Dictionary<string, string>? Data, string? Error) Load(string? filePath, IEnumerable<string>? setPairs)
    {
        var data = new Dictionary<string, string>();

        if (!string.IsNullOrWhiteSpace(filePath))
        {
            if (!File.Exists(filePath))
                return (null, $"Data file not found: {filePath}");

            JsonDocument doc;
            try { doc = JsonDocument.Parse(File.ReadAllText(filePath)); }
            catch (JsonException ex) { return (null, $"Data file is not valid JSON: {ex.Message}"); }

            using (doc)
            {
                if (doc.RootElement.ValueKind != JsonValueKind.Object)
                    return (null, "Data file must hold a single JSON object of field-name → value.");

                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    if (prop.Value.ValueKind is JsonValueKind.Object or JsonValueKind.Array)
                        return (null, $"Field '{prop.Name}': nested objects/arrays are not supported — values must be scalars.");
                    data[prop.Name] = prop.Value.ValueKind == JsonValueKind.String
                        ? prop.Value.GetString() ?? string.Empty
                        : prop.Value.GetRawText();
                }
            }
        }

        foreach (var pair in setPairs ?? [])
        {
            var idx = pair.IndexOf('=');
            if (idx <= 0)
                return (null, $"--set expects Field=Value, got '{pair}'.");
            data[pair[..idx]] = pair[(idx + 1)..];
        }

        return (data, null);
    }
}
