using System.Text.RegularExpressions;

namespace Bolt.Automation.AgentTools.Code;

public sealed record GrepHit(string File, int Line, string Text);

/// <summary>
/// Runs ripgrep (<c>rg</c>) for fast code search and parses its
/// <c>file:line:text</c> output. Falls back to a managed
/// EnumerateFiles + Regex scan over <c>*.cs</c> only when <c>rg</c> isn't
/// installed. Both paths exclude build output and nested worktrees
/// (.git/.claude/.skill-explore/node_modules) so results are the canonical
/// source tree, not stale copies.
/// </summary>
internal sealed class GrepRunner
{
    private static readonly string[] ExcludedSegments =
        { "/bin/", "/obj/", "/.git/", "/.claude/", "/.skill-explore/", "/node_modules/" };

    private readonly string _root;
    public GrepRunner(string root) => _root = root;

    public IReadOnlyList<GrepHit> Search(string pattern, int maxCount = 200)
    {
        var rg = TryRipgrep(pattern, maxCount);
        return rg ?? ManagedFallback(pattern, maxCount);
    }

    private List<GrepHit>? TryRipgrep(string pattern, int maxCount)
    {
        var result = ProcessRunner.Run("rg",
        [
            "--line-number", "--no-heading", "--color", "never",
            "-g", "*.cs",
            "-g", "!**/bin/**", "-g", "!**/obj/**",
            "-g", "!**/.git/**", "-g", "!**/.claude/**",
            "-g", "!**/.skill-explore/**", "-g", "!**/node_modules/**",
            "-m", maxCount.ToString(),
            "-e", pattern, _root,
        ], _root, 60_000);

        // rg not installed → signal fallback. Any other case (0 matches, 1 no
        // matches, 2 partial read errors) is authoritative: rg still skips
        // hidden dirs + .gitignore, so its stdout is the clean result.
        if (!result.Started) return null;

        var hits = new List<GrepHit>();
        foreach (var line in result.StdOut.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var parsed = ParseRgLine(line.TrimEnd('\r'));
            if (parsed != null) hits.Add(parsed);
            if (hits.Count >= maxCount) break;
        }
        return hits;
    }

    private GrepHit? ParseRgLine(string line)
    {
        // Format: <path>:<line>:<text>. Path may contain a drive colon on Windows.
        var firstColon = line.IndexOf(':', line.Length > 1 && line[1] == ':' ? 2 : 0);
        if (firstColon < 0) return null;
        var secondColon = line.IndexOf(':', firstColon + 1);
        if (secondColon < 0) return null;

        var path = line[..firstColon];
        if (!int.TryParse(line[(firstColon + 1)..secondColon], out var lineNo)) return null;
        var text = line[(secondColon + 1)..].Trim();
        return new GrepHit(Rel(path), lineNo, text);
    }

    private List<GrepHit> ManagedFallback(string pattern, int maxCount)
    {
        var hits = new List<GrepHit>();
        // Bound match time: a user-supplied --pattern can backtrack catastrophically
        // and Compiled regexes have no interpretive bail-out. On the first timeout the
        // pattern is unusable, so stop and return what we have rather than hang.
        var matchTimeout = TimeSpan.FromSeconds(5);
        Regex rx;
        try { rx = new Regex(pattern, RegexOptions.Compiled, matchTimeout); }
        catch (ArgumentException) { rx = new Regex(Regex.Escape(pattern), RegexOptions.Compiled, matchTimeout); }

        foreach (var file in Directory.EnumerateFiles(_root, "*.cs", SearchOption.AllDirectories))
        {
            if (IsExcluded(file)) continue;

            int n = 0;
            foreach (var line in File.ReadLines(file))
            {
                n++;
                bool isMatch;
                try { isMatch = rx.IsMatch(line); }
                catch (RegexMatchTimeoutException) { return hits; }
                if (isMatch)
                {
                    hits.Add(new GrepHit(Rel(file), n, line.Trim()));
                    if (hits.Count >= maxCount) return hits;
                }
            }
        }
        return hits;
    }

    private static bool IsExcluded(string path)
    {
        var norm = path.Replace('\\', '/');
        foreach (var seg in ExcludedSegments)
            if (norm.Contains(seg, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    private string Rel(string path)
    {
        try { return Path.GetRelativePath(_root, path).Replace('\\', '/'); }
        catch { return path.Replace('\\', '/'); }
    }
}
