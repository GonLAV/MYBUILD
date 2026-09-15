using System.Diagnostics;
using System.Text.Json;
using Bolt.Automation.AgentTools.SessionState;

namespace Bolt.Automation.AgentTools.Browser.HostProcess;

/// <summary>Host discovery record written to <c>%TMP%\nexus-agent\host.lock</c>.</summary>
internal sealed record HostLock(int Pid, int Port);

/// <summary>
/// Reads/writes the single-host lock file. The client uses it to find a running
/// host (and verify the PID is still alive); the host owns its lifecycle.
/// </summary>
internal static class LockFile
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    public static string Path => System.IO.Path.Combine(SessionDirManager.Root, "host.lock");

    public static void Write(int port)
    {
        Directory.CreateDirectory(SessionDirManager.Root);
        File.WriteAllText(Path, JsonSerializer.Serialize(new HostLock(Environment.ProcessId, port), Json));
    }

    public static HostLock? Read()
    {
        if (!File.Exists(Path)) return null;
        try { return JsonSerializer.Deserialize<HostLock>(File.ReadAllText(Path), Json); }
        catch { return null; }
    }

    public static void Delete()
    {
        try { if (File.Exists(Path)) File.Delete(Path); } catch { /* best-effort */ }
    }

    /// <summary>True if the recorded PID is a live process.</summary>
    public static bool IsAlive(HostLock l)
    {
        try
        {
            using var p = Process.GetProcessById(l.Pid);
            return !p.HasExited;
        }
        catch { return false; }
    }
}
