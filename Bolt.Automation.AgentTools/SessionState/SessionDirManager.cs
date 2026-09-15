using System.Text.Json;

namespace Bolt.Automation.AgentTools.SessionState;

/// <summary>
/// Owns the on-disk layout for browser sessions under
/// <c>%TMP%\nexus-agent\sessions\&lt;id&gt;\</c>:
/// <list type="bullet">
///   <item><c>scope.json</c> — <see cref="ScopeSnapshot"/> metadata.</item>
///   <item><c>storage_state.json</c> — Playwright storage state (for resume).</item>
///   <item><c>url.txt</c> — last known URL (quick read).</item>
///   <item><c>pause-reason.md</c> — human-readable pause note.</item>
/// </list>
/// Machine-local and transient — never tracked by git.
/// </summary>
internal sealed class SessionDirManager
{
    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    /// <summary>Shared CLI temp root (also holds host.lock, kb-index.json).</summary>
    public static string Root => Path.Combine(Path.GetTempPath(), "nexus-agent");

    public static string SessionsRoot => Path.Combine(Root, "sessions");

    /// <summary>8 hex chars — collision-safe for the handful of concurrent sessions a human drives.</summary>
    public static string NewSessionId() => Guid.NewGuid().ToString("N")[..8];

    public string SessionDir(string id) => Path.Combine(SessionsRoot, id);

    public string EnsureSessionDir(string id)
    {
        var dir = SessionDir(id);
        Directory.CreateDirectory(dir);
        return dir;
    }

    public string ScopePath(string id) => Path.Combine(SessionDir(id), "scope.json");
    public string StorageStatePath(string id) => Path.Combine(SessionDir(id), "storage_state.json");
    public string UrlPath(string id) => Path.Combine(SessionDir(id), "url.txt");
    public string PauseReasonPath(string id) => Path.Combine(SessionDir(id), "pause-reason.md");

    public bool Exists(string id) => Directory.Exists(SessionDir(id));

    public IReadOnlyList<string> ListSessionIds()
    {
        if (!Directory.Exists(SessionsRoot)) return Array.Empty<string>();
        return Directory.EnumerateDirectories(SessionsRoot)
            .Select(d => Path.GetFileName(d)!)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();
    }

    public void WriteScope(string id, ScopeSnapshot snap)
    {
        EnsureSessionDir(id);
        File.WriteAllText(ScopePath(id), JsonSerializer.Serialize(snap, Json));
        if (!string.IsNullOrEmpty(snap.CurrentUrl))
            File.WriteAllText(UrlPath(id), snap.CurrentUrl);
    }

    public ScopeSnapshot? ReadScope(string id)
    {
        var path = ScopePath(id);
        if (!File.Exists(path)) return null;
        try { return JsonSerializer.Deserialize<ScopeSnapshot>(File.ReadAllText(path), Json); }
        catch { return null; }
    }

    public void WritePauseReason(string id, string reason)
    {
        EnsureSessionDir(id);
        File.WriteAllText(PauseReasonPath(id),
            $"# Session {id} — paused\n\n{reason}\n");
    }

    public void Delete(string id)
    {
        var dir = SessionDir(id);
        if (Directory.Exists(dir))
        {
            try { Directory.Delete(dir, recursive: true); }
            catch { /* best-effort cleanup */ }
        }
    }
}
