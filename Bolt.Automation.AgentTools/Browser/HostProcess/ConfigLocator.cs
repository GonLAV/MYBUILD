using Bolt.Automation.AgentTools.Code;

namespace Bolt.Automation.AgentTools.Browser.HostProcess;

/// <summary>
/// The host runs outside the Tests project, so <c>ConfigurationLoader</c> can't
/// find <c>appsettings*.json</c> on its own (it would throw on the non-optional
/// base file). We point <c>BOLT_CONFIG_PATH</c> — ConfigurationLoader's official
/// override — at the Tests build output, which carries the full appsettings set
/// for every environment, TFM-matched to the host runtime.
/// </summary>
internal static class ConfigLocator
{
    public const string EnvVar = "BOLT_CONFIG_PATH";

    public static void EnsureConfigPath()
    {
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(EnvVar))) return;
        var dir = FindTestsConfigDir();
        if (dir != null) Environment.SetEnvironmentVariable(EnvVar, dir);
    }

    public static string? FindTestsConfigDir()
    {
        var repoRoot = RepoLocator.Resolve();
        if (repoRoot == null) return null;

        var binDir = Path.Combine(repoRoot, "Bolt.Automation.Tests", "bin");
        if (!Directory.Exists(binDir)) return null;

        var hostTfm = $"net{Environment.Version.Major}.{Environment.Version.Minor}";
        return Directory.EnumerateFiles(binDir, "appsettings.json", SearchOption.AllDirectories)
            .Select(Path.GetDirectoryName)
            .Where(d => d != null)
            .Cast<string>()
            .OrderByDescending(d => d.Replace('\\', '/').Contains($"/{hostTfm}/", StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(d => d.Replace('\\', '/').Contains("/debug/", StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(d => File.GetLastWriteTimeUtc(Path.Combine(d, "appsettings.json")))
            .FirstOrDefault();
    }
}
