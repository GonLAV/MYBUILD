namespace Bolt.Automation.AgentTools.KnowledgeBase;

/// <summary>
/// Resolves KB-relative paths (forward-slash, e.g. <c>framework/overview.md</c>)
/// to absolute filesystem paths under the KB root, and enumerates all leaves.
/// </summary>
internal sealed class LeafResolver
{
    private readonly string _kbRoot;

    public LeafResolver(string kbRoot)
    {
        _kbRoot = Path.GetFullPath(kbRoot);
    }

    public string KbRoot => _kbRoot;

    public string ToAbsolute(string kbRelative)
    {
        var normalized = kbRelative.Replace('\\', '/').TrimStart('/');
        return Path.GetFullPath(Path.Combine(_kbRoot, normalized.Replace('/', Path.DirectorySeparatorChar)));
    }

    public bool Exists(string kbRelative)
    {
        var abs = ToAbsolute(kbRelative);
        return IsUnderKbRoot(abs) && File.Exists(abs);
    }

    /// <summary>
    /// Guards against path-traversal attempts via <c>..</c> segments. Returns
    /// true if <paramref name="absolutePath"/> resolves to a location at or
    /// below the KB root.
    /// </summary>
    public bool IsUnderKbRoot(string absolutePath)
    {
        // Match the filesystem's case rules: Windows is case-insensitive, *nix is
        // case-sensitive. OrdinalIgnoreCase everywhere would let a sibling directory
        // whose name only case-matches the KB root slip past the guard on Linux.
        var cmp = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        var root = _kbRoot.TrimEnd(Path.DirectorySeparatorChar);
        var path = Path.GetFullPath(absolutePath);
        return path.StartsWith(root + Path.DirectorySeparatorChar, cmp)
            || string.Equals(path, root, cmp);
    }

    /// <summary>
    /// All <c>.md</c> files under the KB root, returned as KB-relative paths
    /// with forward-slash separators. Order is implementation-defined.
    /// </summary>
    public IEnumerable<string> EnumerateLeaves()
    {
        foreach (var path in Directory.EnumerateFiles(_kbRoot, "*.md", SearchOption.AllDirectories))
        {
            yield return Path.GetRelativePath(_kbRoot, path).Replace(Path.DirectorySeparatorChar, '/');
        }
    }
}
