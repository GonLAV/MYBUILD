using System.Text.Json;

namespace Bolt.Automation.Common.Services.Secrets;

/// <summary>
/// Single source of truth for the secrets bundle's on-disk identity and the key-derivation
/// rules shared by its two readers — <see cref="SecretsStore"/> (DB connection strings) and
/// <see cref="BoltSecretsConfigurationProvider"/> (app secrets). Centralizing these here is
/// what keeps the two readers from drifting: they resolve the SAME file name and, critically,
/// the SAME per-environment bundle key for a given environment.
/// </summary>
/// <remarks>
/// NO-SECRET-LOGGING INVARIANT: nothing here logs. The helpers only compute paths/keys and
/// walk JSON structure — they must never emit a secret value.
/// </remarks>
public static class SecretsBundle
{
    /// <summary>
    /// Canonical bundle file name. Must stay exactly this — both readers and the CI CSI mount
    /// depend on it (see the nexus-secrets skill). Do not re-declare this literal elsewhere.
    /// </summary>
    public const string FileName = "automation_nexus_secrets_store.json";

    /// <summary>
    /// Resolves a <c>BOLT_SECRETS_PATH</c> value to the bundle file: if it points at an existing
    /// directory, appends <see cref="FileName"/>; otherwise returns it unchanged (already a file).
    /// </summary>
    public static string ResolvePath(string path)
        => Directory.Exists(path) ? Path.Combine(path, FileName) : path;

    /// <summary>
    /// Derives the bundle's environment key from the parsed <see cref="Environment"/>. The bundle
    /// keys environments by the lowercase enum name (qa, dev, uat, staging, production).
    /// </summary>
    public static string ResolveEnvironmentKey(Environment environment)
        => environment.ToString().ToLowerInvariant();

    /// <summary>
    /// Derives the bundle's environment key from a raw environment string, normalizing common
    /// aliases (e.g. "Development" → "dev", "production" → "production") so the app-secrets
    /// provider lands on the SAME key <see cref="SecretsStore"/> uses for the same environment.
    /// Without this, an env named "Development" would query "development" while SecretsStore
    /// queries "dev", and the bundle's app secrets would silently resolve to null.
    /// Falls back to the lowercased input when the string matches no known environment.
    /// </summary>
    public static string ResolveEnvironmentKey(string? environment)
    {
        if (string.IsNullOrWhiteSpace(environment))
            return ResolveEnvironmentKey(Environment.Dev);

        if (System.Enum.TryParse<Environment>(environment, ignoreCase: true, out var parsed))
            return ResolveEnvironmentKey(parsed);

        return environment.ToLowerInvariant() switch
        {
            "dev" or "development" => ResolveEnvironmentKey(Environment.Dev),
            "qa" or "test" or "testing" => ResolveEnvironmentKey(Environment.Qa),
            "uat" or "user acceptance" or "useracceptance" => ResolveEnvironmentKey(Environment.Uat),
            "staging" or "stage" or "stg" => ResolveEnvironmentKey(Environment.Staging),
            "prod" or "production" or "live" => ResolveEnvironmentKey(Environment.Production),
            _ => environment.ToLowerInvariant(),
        };
    }

    /// <summary>
    /// Case-insensitive lookup of a JSON object property. Shared by the provider and the DevOps
    /// verification tests so the bundle-navigation semantics stay identical between them.
    /// </summary>
    public static bool TryGetPropertyIgnoreCase(JsonElement element, string name, out JsonElement value)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in element.EnumerateObject())
            {
                if (string.Equals(prop.Name, name, System.StringComparison.OrdinalIgnoreCase))
                {
                    value = prop.Value;
                    return true;
                }
            }
        }

        value = default;
        return false;
    }
}
