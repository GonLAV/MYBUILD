namespace Bolt.Automation.AgentTools.SessionState;

/// <summary>
/// Serializable snapshot of a live browser session, persisted to
/// <c>%TMP%\nexus-agent\sessions\&lt;id&gt;\scope.json</c>. It lets the client
/// surface session metadata (and `browser list`) without round-tripping to the
/// host for every field, and gives a paper trail for cross-process resume.
/// </summary>
public sealed class ScopeSnapshot
{
    public string SessionId { get; set; } = string.Empty;
    public string Tenant { get; set; } = string.Empty;
    public string Env { get; set; } = string.Empty;
    public string Flow { get; set; } = string.Empty;
    public string Until { get; set; } = string.Empty;
    public bool Headed { get; set; }

    public string? StartUrl { get; set; }
    public string? CurrentUrl { get; set; }
    public string? CurrentPage { get; set; }

    public bool Paused { get; set; }
    public string? PauseReason { get; set; }

    /// <summary>ISO-8601 UTC creation timestamp (string so it survives round-trips verbatim).</summary>
    public string CreatedAtUtc { get; set; } = string.Empty;

    /// <summary>ISO-8601 UTC of the last successful host action against this session.</summary>
    public string? LastActionUtc { get; set; }
}
