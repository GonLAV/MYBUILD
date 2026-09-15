using CommandLine;

namespace Bolt.Automation.AgentTools.Commands.Secrets;

[Verb("status", HelpText = "Show local secrets cache status (no API call).")]
internal sealed class StatusOptions { }

internal static class StatusCommand
{
    public static Task<int> ExecuteAsync(StatusOptions _)
    {
        var cacheDir = SecretsCache.Dir;
        var bundlePath = SecretsCache.BundlePath;

        var bundleExists = File.Exists(bundlePath);
        string? syncedAt = null;
        int? ageMinutes = null;
        double? ttlHours = null;
        int? ttlRemainingMinutes = null;
        string? secretId = null;

        // Read the same sidecar sync wrote — including the EFFECTIVE TTL — so status and sync
        // never disagree about freshness (a bundle synced with --ttl 1 reports a ~1h window here,
        // not a hardcoded 12h).
        if (SecretsCache.Read() is { } entry)
        {
            syncedAt = entry.SyncedAt.ToString("O");
            ttlHours = entry.TtlHours;
            secretId = entry.SecretId;
            var age = DateTimeOffset.UtcNow - entry.SyncedAt;
            ageMinutes = (int)age.TotalMinutes;
            var remaining = TimeSpan.FromHours(entry.TtlHours) - age;
            ttlRemainingMinutes = Math.Max(0, (int)remaining.TotalMinutes);
        }

        return CommandBase.EmitJsonAsync(new
        {
            cacheDir,
            bundleFile = bundlePath,
            bundleExists,
            syncedAt,
            ageMinutes,
            ttlHours,
            ttlRemainingMinutes,
            secretId,
            boltSecretsPathExport = $"BOLT_SECRETS_PATH={cacheDir}",
        }, exitCode: 0);
    }
}
