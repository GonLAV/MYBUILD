namespace Bolt.Automation.AgentTools.Failure;

/// <summary>
/// Locates the on-disk test artifacts the <c>failure</c> commands read:
///   - per-test folders (page_source_*.html, *_final_*.png) under
///     <c>Bolt.Automation.Tests/bin/&lt;cfg&gt;/net&lt;tfm&gt;/TestResults/&lt;TestName&gt;/</c>
///   - <c>.trx</c> run files under any <c>TestResults/</c> (bin, project root,
///     or repo root).
///
/// TFM- and configuration-agnostic: it walks <c>bin/</c> for any directory
/// literally named <c>TestResults</c>, so net9.0/net10.0 + Debug/Release all
/// resolve. Override with <c>NEXUS_TEST_RESULTS</c> (semicolon-separated dirs).
/// </summary>
internal sealed class ArtifactsLocator
{
    private const string TestProjectDir = "Bolt.Automation.Tests";
    private const string TestResultsDirName = "TestResults";
    private const string EnvOverride = "NEXUS_TEST_RESULTS";
    private const int MaxTrxFilesConsidered = 50;

    private readonly string? _repoRoot;
    private readonly List<string> _overrideRoots;

    public ArtifactsLocator()
    {
        // Reuse the shared, git-aware resolver (handles worktrees) instead of a
        // second .sln walk-up.
        _repoRoot = Code.RepoLocator.Resolve();

        var env = Environment.GetEnvironmentVariable(EnvOverride);
        _overrideRoots = string.IsNullOrWhiteSpace(env)
            ? new List<string>()
            : env.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                 .Select(Path.GetFullPath)
                 .Where(Directory.Exists)
                 .ToList();
    }

    public string? RepoRoot => _repoRoot;

    /// <summary>
    /// Per-test artifact folders live directly under these directories. Most
    /// recently modified root first.
    /// </summary>
    public IReadOnlyList<string> ArtifactRoots()
    {
        var roots = new List<string>(_overrideRoots);

        if (_repoRoot != null)
        {
            var binDir = Path.Combine(_repoRoot, TestProjectDir, "bin");
            if (Directory.Exists(binDir))
            {
                try
                {
                    roots.AddRange(Directory.EnumerateDirectories(binDir, TestResultsDirName, SearchOption.AllDirectories));
                }
                catch (UnauthorizedAccessException) { /* skip unreadable trees */ }
            }
        }

        return roots
            .Where(Directory.Exists)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(SafeLastWrite)
            .ToList();
    }

    /// <summary>
    /// All <c>.trx</c> files across bin, project-root, and repo-root
    /// TestResults dirs. Most recent first, capped for safety.
    /// </summary>
    public IReadOnlyList<string> TrxFiles()
    {
        var searchDirs = new List<string>(_overrideRoots);

        if (_repoRoot != null)
        {
            AddIfExists(searchDirs, Path.Combine(_repoRoot, TestProjectDir, TestResultsDirName));
            AddIfExists(searchDirs, Path.Combine(_repoRoot, TestResultsDirName));
            searchDirs.AddRange(ArtifactRoots());
        }

        var trx = new List<string>();
        foreach (var dir in searchDirs.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                trx.AddRange(Directory.EnumerateFiles(dir, "*.trx", SearchOption.AllDirectories));
            }
            catch (UnauthorizedAccessException) { /* skip */ }
            catch (DirectoryNotFoundException) { /* raced deletion */ }
        }

        return trx
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(SafeLastWrite)
            .Take(MaxTrxFilesConsidered)
            .ToList();
    }

    private static void AddIfExists(List<string> list, string dir)
    {
        if (Directory.Exists(dir)) list.Add(dir);
    }

    private static DateTime SafeLastWrite(string path)
    {
        try { return File.GetLastWriteTimeUtc(path); }
        catch { return DateTime.MinValue; }
    }
}
