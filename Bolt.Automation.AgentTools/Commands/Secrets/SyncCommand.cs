using Amazon;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using CommandLine;
using Microsoft.Extensions.Configuration;

namespace Bolt.Automation.AgentTools.Commands.Secrets;

[Verb("sync", HelpText = "Fetch the secrets bundle from AWS Secrets Manager and cache locally.")]
internal sealed class SyncOptions
{
    [Option("region", HelpText = "AWS region (overrides BoltSecrets:Region from config).")]
    public string? Region { get; set; }

    [Option("secret-id", HelpText = "AWS secret ID (overrides BoltSecrets:SecretId from config).")]
    public string? SecretId { get; set; }

    [Option("ttl", HelpText = "Cache TTL in hours (default 12).")]
    public double? TtlHours { get; set; }

    [Option("force", HelpText = "Ignore TTL and always fetch from AWS.")]
    public bool Force { get; set; }
}

internal static class SyncCommand
{
    public static async Task<int> ExecuteAsync(SyncOptions options)
    {
        var ttlHours = options.TtlHours ?? SecretsCache.DefaultTtlHours;

        // Resolve secret-id/region BEFORE the cache check so the cache can be keyed on them:
        // a fresh cache for the default bundle must not be served when the caller asked for a
        // different --secret-id/--region.
        var (region, secretId) = await ResolveConfigAsync(options);

        if (string.IsNullOrWhiteSpace(region))
            return await CommandBase.EmitErrorAsync("config_missing",
                "AWS region not configured. Set BoltSecrets:Region in appsettings or pass --region.", exitCode: 2);

        if (string.IsNullOrWhiteSpace(secretId))
            return await CommandBase.EmitErrorAsync("config_missing",
                "Secret ID not configured. Set BoltSecrets:SecretId in appsettings or pass --secret-id.", exitCode: 2);

        if (!options.Force && File.Exists(SecretsCache.BundlePath) && SecretsCache.Read() is { } entry
            && string.Equals(entry.SecretId, secretId, StringComparison.Ordinal)
            && string.Equals(entry.Region, region, StringComparison.OrdinalIgnoreCase))
        {
            var age = DateTimeOffset.UtcNow - entry.SyncedAt;
            // Freshness is governed by the TTL that was effective when the bundle was written,
            // so status (which reads the same sidecar) never disagrees with sync.
            if (age < TimeSpan.FromHours(entry.TtlHours))
            {
                Console.WriteLine($"[secrets] cache hit (synced {FormatAge(age)}), use --force to refresh");
                Console.WriteLine($"[secrets] BOLT_SECRETS_PATH={SecretsCache.Dir}");
                return 0;
            }
        }

        Console.WriteLine($"[secrets] fetching bundle from AWS Secrets Manager (region={region}, id={secretId})");

        try
        {
            // READ-ONLY CONTRACT: this tool performs exactly one Secrets Manager operation —
            // GetSecretValueAsync (read). It must never create, edit, delete, tag, or otherwise
            // mutate a secret in AWS. The AutomationTeam permission set is expected to be scoped
            // read-only to enforce this (GetSecretValue/DescribeSecret/ListSecrets only).
            // The fetched payload is written ONLY to the repo-external local cache below — never
            // back to AWS. Do NOT add Put/Update/Delete/CreateSecret calls here.
            using var client = new AmazonSecretsManagerClient(RegionEndpoint.GetBySystemName(region));
            var response = await client.GetSecretValueAsync(new GetSecretValueRequest { SecretId = secretId });
            var payload = response.SecretString;

            // A null/empty SecretString (binary-only or empty secret) would otherwise write an
            // empty bundle + fresh timestamp and exit 0, poisoning the cache until TTL expiry.
            if (string.IsNullOrWhiteSpace(payload))
                return await CommandBase.EmitErrorAsync("aws_fetch_failed",
                    "Secret has no string payload (empty, or stored as SecretBinary). " +
                    "Expected the JSON bundle as the secret's string value.", exitCode: 2);

            // Writes are INSIDE the try so a read-only/full cache dir surfaces as the documented
            // aws_fetch_failed/exit-2 contract instead of an uncaught crash.
            await SecretsCache.WriteAsync(payload, DateTimeOffset.UtcNow, ttlHours, secretId, region);
        }
        catch (Exception ex)
        {
            return await CommandBase.EmitErrorAsync("aws_fetch_failed", ex.Message, exitCode: 2);
        }

        Console.WriteLine($"[secrets] bundle written to {SecretsCache.BundlePath}");
        Console.WriteLine($"[secrets] set BOLT_SECRETS_PATH={SecretsCache.Dir}");
        return 0;
    }

    private static Task<(string? region, string? secretId)> ResolveConfigAsync(SyncOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.Region) && !string.IsNullOrWhiteSpace(options.SecretId))
            return Task.FromResult<(string?, string?)>((options.Region, options.SecretId));

        try
        {
            var configDir = FindAppsettingsDir();
            if (configDir != null)
            {
                var config = new ConfigurationBuilder()
                    .SetBasePath(configDir)
                    .AddJsonFile("appsettings.json", optional: true)
                    .Build();
                return Task.FromResult<(string?, string?)>((
                    options.Region ?? config["BoltSecrets:Region"],
                    options.SecretId ?? config["BoltSecrets:SecretId"]));
            }
        }
        catch { /* fall through to CLI options only */ }

        return Task.FromResult<(string?, string?)>((options.Region, options.SecretId));
    }

    private static string? FindAppsettingsDir()
    {
        var candidates = new[]
        {
            Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
            Directory.GetCurrentDirectory(),
        };
        foreach (var dir in candidates.Where(d => !string.IsNullOrEmpty(d)))
        {
            if (File.Exists(Path.Combine(dir!, "appsettings.json")))
                return dir;
        }

        var current = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (current != null)
        {
            foreach (var sub in new[] { "Bolt.Automation.Tests", "Bolt.Automation.InfraTests" })
            {
                var candidate = Path.Combine(current.FullName, sub, "appsettings.json");
                if (File.Exists(candidate))
                    return Path.GetDirectoryName(candidate);
            }
            current = current.Parent;
        }
        return null;
    }

    private static string FormatAge(TimeSpan age)
    {
        if (age.TotalHours >= 1) return $"{(int)age.TotalHours}h ago";
        if (age.TotalMinutes >= 1) return $"{(int)age.TotalMinutes}m ago";
        return "just now";
    }
}
