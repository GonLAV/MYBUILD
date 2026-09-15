using System.Text.Json;
using Bolt.Automation.Common.Services.Secrets;

namespace Bolt.Automation.AgentTools.Commands.Secrets;

/// <summary>
/// Single owner of the local secrets-cache layout and freshness contract shared by
/// <c>secrets sync</c> (writer) and <c>secrets status</c> (reader): the cache directory, the
/// bundle + sidecar paths, the default TTL, and the read/write of the <c>.synced-at</c> sidecar.
/// Centralizing this is what stops sync and status from drifting on file names, the TTL value,
/// or the parse logic — the drift that previously let status report a 12h window for a bundle
/// synced with <c>--ttl 1</c>.
/// </summary>
/// <remarks>
/// NO-SECRET-LOGGING / NO-SECRET-PERSISTENCE: the sidecar records only non-sensitive metadata
/// (timestamp, TTL, the secret-id and region the bundle was fetched for). It never contains a
/// secret value — the bundle payload itself is the only place values live, and it is written
/// verbatim to <see cref="BundlePath"/>.
/// </remarks>
internal static class SecretsCache
{
    /// <summary>Default cache lifetime, in hours, when <c>--ttl</c> is not supplied.</summary>
    public const double DefaultTtlHours = 12;

    private const string SyncedAtFileName = ".synced-at";

    /// <summary>
    /// Repo-external cache directory: <c>%LOCALAPPDATA%\BoltAutomation\secrets</c> on Windows,
    /// <c>~/.local/share/BoltAutomation/secrets</c> elsewhere. Outside any repo so it can never
    /// be committed.
    /// </summary>
    public static readonly string Dir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "BoltAutomation", "secrets");

    /// <summary>Absolute path to the cached bundle file.</summary>
    public static string BundlePath => Path.Combine(Dir, SecretsBundle.FileName);

    /// <summary>Absolute path to the sidecar that records when/how the bundle was fetched.</summary>
    public static string SyncedAtPath => Path.Combine(Dir, SyncedAtFileName);

    /// <summary>
    /// What <c>secrets sync</c> recorded the last time it wrote the cache. <see cref="TtlHours"/>
    /// is the TTL that was effective at write time and governs freshness for later reads, so
    /// sync and status always agree. <see cref="SecretId"/>/<see cref="Region"/> identify which
    /// bundle the cache holds, so a sync for a *different* secret-id/region is not served stale.
    /// </summary>
    public sealed record Entry(DateTimeOffset SyncedAt, double TtlHours, string? SecretId, string? Region);

    /// <summary>
    /// Persists the bundle payload and the sidecar atomically-enough for our needs (bundle first,
    /// then sidecar). Throws on any IO failure — the caller wraps this so a write failure maps to
    /// the documented <c>aws_fetch_failed</c> / exit-2 contract rather than an uncaught crash.
    /// </summary>
    public static async Task WriteAsync(
        string payload, DateTimeOffset syncedAt, double ttlHours, string secretId, string region)
    {
        Directory.CreateDirectory(Dir);
        await File.WriteAllTextAsync(BundlePath, payload);
        var sidecar = JsonSerializer.Serialize(new Entry(syncedAt, ttlHours, secretId, region));
        await File.WriteAllTextAsync(SyncedAtPath, sidecar);
    }

    /// <summary>
    /// Reads the sidecar. Returns <c>null</c> when it is absent, empty, or unparseable. Tolerates
    /// a legacy plain-ISO-8601-timestamp sidecar by assuming <see cref="DefaultTtlHours"/> and no
    /// recorded secret-id/region.
    /// </summary>
    public static Entry? Read()
    {
        if (!File.Exists(SyncedAtPath))
            return null;

        var raw = File.ReadAllText(SyncedAtPath).Trim();
        if (raw.Length == 0)
            return null;

        if (raw.StartsWith('{'))
        {
            try
            {
                var entry = JsonSerializer.Deserialize<Entry>(raw);
                if (entry is not null && entry.TtlHours > 0)
                    return entry;
                return null;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        // Legacy/plain sidecar: a bare ISO-8601 timestamp written before the JSON format.
        return DateTimeOffset.TryParse(raw, out var ts)
            ? new Entry(ts, DefaultTtlHours, SecretId: null, Region: null)
            : null;
    }
}
