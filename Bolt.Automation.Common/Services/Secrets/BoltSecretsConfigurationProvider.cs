using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace Bolt.Automation.Common.Services.Secrets;

/// <remarks>
/// NO-SECRET-LOGGING INVARIANT: this provider handles raw secret values, so it must never
/// log a value or the bundle payload. Every Console line below emits only non-sensitive
/// metadata — file paths, env names, and key *counts*. The flattened secret values live only
/// in <see cref="ConfigurationProvider.Data"/> for the binding layer; they are never written
/// to any log/console. If you add diagnostics here, redact values with <c>[REDACTED]</c>.
/// </remarks>
public sealed class BoltSecretsConfigurationProvider(string? path, string environment, bool optional)
    : ConfigurationProvider
{
    public override void Load()
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            Console.WriteLine("[BoltSecrets] BOLT_SECRETS_PATH not set, skipping");
            return;
        }

        var filePath = SecretsBundle.ResolvePath(path);

        if (!File.Exists(filePath))
        {
            if (optional)
            {
                Console.WriteLine($"[BoltSecrets] Secrets file not found at '{filePath}', skipping (optional)");
                return;
            }
            throw new FileNotFoundException($"[BoltSecrets] Secrets file not found at '{filePath}'.", filePath);
        }

        Console.WriteLine($"[BoltSecrets] Loading app secrets from '{filePath}' for env '{environment}'");

        JsonElement root;
        try
        {
            var json = File.ReadAllText(filePath);
            using var doc = JsonDocument.Parse(json);
            root = doc.RootElement.Clone();
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"[BoltSecrets] Failed to parse secrets file at '{filePath}': {ex.Message}", ex);
        }

        if (!SecretsBundle.TryGetPropertyIgnoreCase(root, "environments", out var envMap))
        {
            Console.WriteLine("[BoltSecrets] No 'environments' key in secrets file, skipping");
            return;
        }

        // Normalize via the shared helper so this reader and SecretsStore resolve the SAME key
        // for a given environment (e.g. "Development" → "dev"), not two divergent keys.
        var envKey = SecretsBundle.ResolveEnvironmentKey(environment);
        if (!SecretsBundle.TryGetPropertyIgnoreCase(envMap, envKey, out var envNode))
        {
            Console.WriteLine($"[BoltSecrets] No environment key '{envKey}' in secrets file, skipping");
            return;
        }

        if (!SecretsBundle.TryGetPropertyIgnoreCase(envNode, "appSecrets", out var appSecrets))
        {
            Console.WriteLine($"[BoltSecrets] No 'appSecrets' block for environment '{envKey}', skipping");
            return;
        }

        var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        Flatten(appSecrets, "", data);
        Data = data;

        Console.WriteLine($"[BoltSecrets] Loaded {data.Count} app-secret config keys for env '{envKey}'");
    }

    private static void Flatten(JsonElement element, string prefix, Dictionary<string, string?> data)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var prop in element.EnumerateObject())
                {
                    var key = prefix == "" ? prop.Name : $"{prefix}:{prop.Name}";
                    Flatten(prop.Value, key, data);
                }
                break;

            case JsonValueKind.Array:
                var i = 0;
                foreach (var item in element.EnumerateArray())
                {
                    Flatten(item, $"{prefix}:{i}", data);
                    i++;
                }
                break;

            case JsonValueKind.String:
                // GetString() decodes JSON escapes (\", \\, \n, \uXXXX) — GetRawText() would leave them raw
                data[prefix] = element.GetString();
                break;

            case JsonValueKind.Number:
            case JsonValueKind.True:
            case JsonValueKind.False:
                data[prefix] = element.GetRawText();
                break;

            case JsonValueKind.Null:
                // Skip — let appsettings/defaults stand
                break;
        }
    }
}
