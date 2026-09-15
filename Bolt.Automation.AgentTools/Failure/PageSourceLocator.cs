using Bolt.Automation.Common.Utils;

namespace Bolt.Automation.AgentTools.Failure;

/// <summary>
/// Maps a test FQN (or bare method name) to its captured page_source/screenshot
/// artifacts. The framework writes them to a folder named
/// <c>FileNameUtils.SanitizeFileName(TestContext.Test.Name)</c> — i.e. the
/// (possibly parameterized) method name — so we sanitize identically here.
/// When no exact folder exists, a nearest-match suggestion is offered.
/// </summary>
internal sealed class PageSourceLocator
{
    private readonly ArtifactsLocator _artifacts;

    public PageSourceLocator(ArtifactsLocator artifacts) => _artifacts = artifacts;

    public TestArtifacts Locate(string fqnOrMethod)
    {
        var method = DeriveMethodName(fqnOrMethod);
        var folderName = FileNameUtils.SanitizeFileName(method);

        var roots = _artifacts.ArtifactRoots();
        var folderPath = FindFolder(roots, folderName);

        if (folderPath == null)
        {
            var suggestion = NearestFolder(roots, folderName);
            return new TestArtifacts(
                Method: method,
                FolderName: folderName,
                FolderFound: false,
                FolderPath: null,
                PageSource: new ArtifactRef(false, null, null, null),
                Screenshot: new ArtifactRef(false, null, null, null),
                Suggestion: suggestion);
        }

        return new TestArtifacts(
            Method: method,
            FolderName: Path.GetFileName(folderPath),
            FolderFound: true,
            FolderPath: folderPath,
            PageSource: LatestArtifact(folderPath, "page_source_*.html"),
            Screenshot: LatestScreenshot(folderPath),
            Suggestion: null);
    }

    /// <summary>Last segment of the FQN, with any parameterization stripped.</summary>
    public static string DeriveMethodName(string fqnOrMethod)
    {
        var s = fqnOrMethod.Trim();
        var paren = s.IndexOf('(');
        var beforeParen = paren >= 0 ? s[..paren] : s;
        var lastDot = beforeParen.LastIndexOf('.');
        return lastDot >= 0 ? beforeParen[(lastDot + 1)..] : beforeParen;
    }

    private static string? FindFolder(IReadOnlyList<string> roots, string folderName)
    {
        // 1. Exact (case-insensitive) folder name match.
        foreach (var root in roots)
        {
            var exact = Path.Combine(root, folderName);
            if (Directory.Exists(exact)) return exact;
        }

        // 2. Parameterized variants: "<method>(args)" → folder startsWith "<method>(".
        var prefix = folderName + "(";
        string? best = null;
        var bestTime = DateTime.MinValue;
        foreach (var root in roots)
        {
            foreach (var dir in SafeSubdirs(root))
            {
                var name = Path.GetFileName(dir);
                if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    var t = SafeLastWrite(dir);
                    if (t > bestTime) { bestTime = t; best = dir; }
                }
            }
        }
        return best;
    }

    private static ArtifactRef LatestArtifact(string folder, string pattern)
    {
        var file = SafeFiles(folder, pattern)
            .OrderByDescending(SafeLastWrite)
            .FirstOrDefault();
        return ToRef(file);
    }

    private static ArtifactRef LatestScreenshot(string folder)
    {
        // Framework writes "<Method>_final_<timestamp>.png"; fall back to any png.
        var file = SafeFiles(folder, "*_final_*.png")
            .Concat(SafeFiles(folder, "*.png"))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(SafeLastWrite)
            .FirstOrDefault();
        return ToRef(file);
    }

    private static ArtifactRef ToRef(string? file)
    {
        if (file == null) return new ArtifactRef(false, null, null, null);
        var fi = new FileInfo(file);
        return new ArtifactRef(
            Found: true,
            Path: file,
            CapturedAt: fi.LastWriteTimeUtc.ToString("o"),
            Bytes: fi.Exists ? fi.Length : null);
    }

    private static string? NearestFolder(IReadOnlyList<string> roots, string desired)
    {
        string? best = null;
        var bestScore = int.MaxValue;
        foreach (var root in roots)
        {
            foreach (var dir in SafeSubdirs(root))
            {
                var name = Path.GetFileName(dir);
                var d = Levenshtein(desired.ToLowerInvariant(), name.ToLowerInvariant());
                if (d < bestScore) { bestScore = d; best = name; }
            }
        }
        // Only suggest when reasonably close (≤ half the desired length).
        return best != null && bestScore <= Math.Max(3, desired.Length / 2) ? best : null;
    }

    private static IEnumerable<string> SafeSubdirs(string root)
    {
        try { return Directory.EnumerateDirectories(root); }
        catch { return Enumerable.Empty<string>(); }
    }

    private static IEnumerable<string> SafeFiles(string folder, string pattern)
    {
        try { return Directory.EnumerateFiles(folder, pattern); }
        catch { return Enumerable.Empty<string>(); }
    }

    private static DateTime SafeLastWrite(string path)
    {
        try { return File.GetLastWriteTimeUtc(path); }
        catch { return DateTime.MinValue; }
    }

    private static int Levenshtein(string a, string b)
    {
        if (a.Length == 0) return b.Length;
        if (b.Length == 0) return a.Length;

        var prev = new int[b.Length + 1];
        var curr = new int[b.Length + 1];
        for (var j = 0; j <= b.Length; j++) prev[j] = j;

        for (var i = 1; i <= a.Length; i++)
        {
            curr[0] = i;
            for (var j = 1; j <= b.Length; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                curr[j] = Math.Min(Math.Min(curr[j - 1] + 1, prev[j] + 1), prev[j - 1] + cost);
            }
            (prev, curr) = (curr, prev);
        }
        return prev[b.Length];
    }
}
