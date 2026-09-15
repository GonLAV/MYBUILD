namespace Bolt.Automation.AgentTools.KnowledgeBase;

/// <summary>
/// Resolves the absolute path of the KB root (<c>Documentation/agent-knowledge/</c>).
/// Strategy:
///   1. <c>NEXUS_KB_ROOT</c> env var, if set and the directory exists.
///   2. Walk up from <see cref="AppContext.BaseDirectory"/> looking for
///      <c>Documentation/agent-knowledge/index.yml</c>.
///   3. Walk up from <see cref="Directory.GetCurrentDirectory"/> as a fallback
///      (covers <c>dotnet run --project</c>).
/// Throws if none of the strategies find <c>index.yml</c>.
/// </summary>
internal static class KnowledgeBaseLocator
{
    private const string KbRelativePath = "Documentation/agent-knowledge";
    private const string IndexFileName = "index.yml";
    private const string KbEnvVar = "NEXUS_KB_ROOT";

    public static string Resolve()
    {
        var fromEnv = Environment.GetEnvironmentVariable(KbEnvVar);
        if (!string.IsNullOrWhiteSpace(fromEnv))
        {
            var envRoot = Path.GetFullPath(fromEnv);
            if (File.Exists(Path.Combine(envRoot, IndexFileName)))
                return envRoot;
            throw new InvalidOperationException(
                $"{KbEnvVar} is set to '{envRoot}' but '{IndexFileName}' is not present there.");
        }

        foreach (var start in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
        {
            var found = WalkUpFor(start);
            if (found != null) return found;
        }

        throw new InvalidOperationException(
            $"Could not locate '{KbRelativePath}/{IndexFileName}'. Set {KbEnvVar} to the KB root directory, " +
            $"or invoke nexus-agent from inside the nexus repo.");
    }

    private static string? WalkUpFor(string startDirectory)
    {
        var dir = new DirectoryInfo(startDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, KbRelativePath);
            if (File.Exists(Path.Combine(candidate, IndexFileName)))
                return Path.GetFullPath(candidate);
            dir = dir.Parent;
        }
        return null;
    }
}
