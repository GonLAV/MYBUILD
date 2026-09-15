namespace Bolt.Automation.AgentTools.Browser.HostProcess;

/// <summary>Thread-safe map of <c>session_id → <see cref="BrowserSession"/></c> inside the host.</summary>
internal sealed class SessionRegistry
{
    private readonly Dictionary<string, BrowserSession> _sessions = new(StringComparer.Ordinal);
    private readonly object _lock = new();

    public void Add(BrowserSession s) { lock (_lock) _sessions[s.Id] = s; }

    public BrowserSession? Get(string id)
    {
        lock (_lock) return _sessions.TryGetValue(id, out var s) ? s : null;
    }

    public IReadOnlyList<BrowserSession> All()
    {
        lock (_lock) return _sessions.Values.ToList();
    }

    public bool Remove(string id)
    {
        lock (_lock)
        {
            if (_sessions.Remove(id, out var s)) { s.Dispose(); return true; }
            return false;
        }
    }

    public void DisposeAll()
    {
        lock (_lock)
        {
            foreach (var s in _sessions.Values) s.Dispose();
            _sessions.Clear();
        }
    }

    public int Count { get { lock (_lock) return _sessions.Count; } }
}
