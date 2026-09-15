using System.Reflection;

namespace Bolt.Automation.AgentTools.Code;

/// <summary>
/// Loads <c>Bolt.Automation.FrontEnds.dll</c> for reflection (flow-trace,
/// field-lookup) with a dependency resolver rooted at the DLL's own bin dir.
/// Prefers the TFM matching the host runtime; includes a staleness pre-flight
/// so commands never reflect over an out-of-date build silently.
/// </summary>
internal sealed class ReflectionLoader
{
    private const string ProjectDir = "Bolt.Automation.FrontEnds";
    private const string DllName = "Bolt.Automation.FrontEnds.dll";

    public string? RepoRoot { get; }
    public string? DllPath { get; private set; }
    public DateTime DllMtimeUtc { get; private set; }

    private static bool _resolverHooked;

    public ReflectionLoader(string? repoRoot = null)
    {
        RepoRoot = repoRoot ?? RepoLocator.Resolve();
        DllPath = ResolveDllPath();
        if (DllPath != null) DllMtimeUtc = File.GetLastWriteTimeUtc(DllPath);
    }

    public bool DllExists => DllPath != null && File.Exists(DllPath);

    /// <summary>
    /// True when any .cs under the FrontEnds project is newer than the built
    /// DLL. <paramref name="newestSourceUtc"/> is the offending mtime.
    /// </summary>
    public bool IsStale(out DateTime newestSourceUtc)
    {
        newestSourceUtc = DateTime.MinValue;
        if (RepoRoot == null || DllPath == null) return false;

        var projDir = Path.Combine(RepoRoot, ProjectDir);
        if (!Directory.Exists(projDir)) return false;

        foreach (var cs in Directory.EnumerateFiles(projDir, "*.cs", SearchOption.AllDirectories))
        {
            var norm = cs.Replace('\\', '/');
            if (norm.Contains("/bin/", StringComparison.OrdinalIgnoreCase) ||
                norm.Contains("/obj/", StringComparison.OrdinalIgnoreCase)) continue;
            var m = File.GetLastWriteTimeUtc(cs);
            if (m > newestSourceUtc) newestSourceUtc = m;
        }
        return newestSourceUtc > DllMtimeUtc;
    }

    public Assembly Load()
    {
        if (DllPath == null) throw new InvalidOperationException("FrontEnds DLL not located.");
        HookResolver(Path.GetDirectoryName(DllPath)!);
        return Assembly.LoadFrom(DllPath);
    }

    /// <summary>Types from the assembly, tolerating partial load failures.</summary>
    public static IReadOnlyList<Type> SafeGetTypes(Assembly asm)
    {
        try { return asm.GetTypes(); }
        catch (ReflectionTypeLoadException ex) { return ex.Types.Where(t => t != null).Cast<Type>().ToList(); }
    }

    // FrontEnds is a class library, so its OWN bin only holds project refs — the
    // NuGet closure (Microsoft.Playwright, etc.) is copied to consuming *app*
    // outputs. We therefore prefer loading FrontEnds.dll from a test project's
    // output, where the full dependency closure sits beside it.
    private static readonly string[] HostProjects =
    {
        "Bolt.Automation.Tests",
        "Bolt.Automation.InfraTests",
        ProjectDir, // FrontEnds' own bin — last resort (deps likely missing)
    };

    private string? ResolveDllPath()
    {
        if (RepoRoot == null) return null;

        var candidates = new List<string>();
        foreach (var proj in HostProjects)
        {
            var binDir = Path.Combine(RepoRoot, proj, "bin");
            if (!Directory.Exists(binDir)) continue;
            try { candidates.AddRange(Directory.EnumerateFiles(binDir, DllName, SearchOption.AllDirectories)); }
            catch (UnauthorizedAccessException) { /* skip */ }
        }
        if (candidates.Count == 0) return null;

        // Rank: has the Playwright dep beside it (= complete closure) → host TFM
        // → Debug → newest.
        var hostTfm = $"net{Environment.Version.Major}.{Environment.Version.Minor}";
        return candidates
            .OrderByDescending(HasFullClosure)
            .ThenByDescending(p => p.Replace('\\', '/').Contains($"/{hostTfm}/", StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(p => p.Replace('\\', '/').Contains("/debug/", StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(File.GetLastWriteTimeUtc)
            .First();
    }

    private static bool HasFullClosure(string frontEndsDllPath)
    {
        var dir = Path.GetDirectoryName(frontEndsDllPath);
        return dir != null && File.Exists(Path.Combine(dir, "Microsoft.Playwright.dll"));
    }

    private static void HookResolver(string probeDir)
    {
        if (_resolverHooked) return;
        _resolverHooked = true;
        AppDomain.CurrentDomain.AssemblyResolve += (_, e) =>
        {
            var simpleName = new AssemblyName(e.Name).Name;
            if (simpleName == null) return null;
            var candidate = Path.Combine(probeDir, simpleName + ".dll");
            return File.Exists(candidate) ? Assembly.LoadFrom(candidate) : null;
        };
    }
}
