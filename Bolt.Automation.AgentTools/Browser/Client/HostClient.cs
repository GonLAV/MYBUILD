using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Bolt.Automation.AgentTools.Browser.HostProcess;

namespace Bolt.Automation.AgentTools.Browser.Client;

/// <summary>Outcome of a host call: HTTP result, or a transport error when the host was unreachable.</summary>
internal sealed record HostResult(bool Ok, int HttpStatus, string Json, string? TransportError);

/// <summary>
/// Client side of the browser host. Discovers a running host via the lock file,
/// spawns one (<c>--host-mode</c>) if absent, posts the command as JSON, and
/// auto-respawns once on connection-refused (host died between discovery and POST).
/// </summary>
internal static class HostClient
{
    private const int Port = 5151;

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    public static async Task<HostResult> PostAsync(string path, object payload, TimeSpan? timeout = null)
    {
        var ensure = await EnsureHostAsync().ConfigureAwait(false);
        if (ensure != null) return new HostResult(false, 0, string.Empty, ensure);

        try
        {
            return await DoPostAsync(path, payload, timeout).ConfigureAwait(false);
        }
        catch (HttpRequestException)
        {
            // Connection refused/reset — host died between discovery and POST. Respawn ONCE.
            LockFile.Delete();
            var retry = await EnsureHostAsync().ConfigureAwait(false);
            if (retry != null) return new HostResult(false, 0, string.Empty, retry);

            try { return await DoPostAsync(path, payload, timeout).ConfigureAwait(false); }
            catch (Exception ex2) { return new HostResult(false, 0, string.Empty, $"Host unreachable: {ex2.Message}"); }
        }
        catch (TaskCanceledException)
        {
            // Client-side timeout. Do NOT respawn or retry: a long op (e.g. a flow
            // walk) is likely still running in the host, and re-POSTing would queue
            // a duplicate behind it and risk spawning zombie hosts. Report and let
            // the user inspect via `browser list`.
            return new HostResult(false, 0, string.Empty,
                $"Timed out waiting for the host to finish '{path}'. It may still be working — "
                + "check `browser list` or %TMP%\\nexus-agent\\host.log, then retry.");
        }
    }

    private static async Task<HostResult> DoPostAsync(string path, object payload, TimeSpan? timeout)
    {
        using var http = new HttpClient { Timeout = timeout ?? TimeSpan.FromMinutes(5) };
        var content = new StringContent(JsonSerializer.Serialize(payload, Json), Encoding.UTF8, "application/json");
        using var resp = await http.PostAsync($"http://127.0.0.1:{Port}/{path}", content).ConfigureAwait(false);
        var bodyText = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
        return new HostResult((int)resp.StatusCode == 200, (int)resp.StatusCode, bodyText, null);
    }

    /// <returns>null on success; an error message if the host could not be made ready.</returns>
    private static async Task<string?> EnsureHostAsync()
    {
        var existing = LockFile.Read();
        if (existing != null && LockFile.IsAlive(existing) && await PingAsync(existing.Port).ConfigureAwait(false))
            return null;
        if (existing != null) LockFile.Delete(); // stale

        var spawnErr = SpawnHost();
        if (spawnErr != null) return spawnErr;

        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (DateTime.UtcNow < deadline)
        {
            if (await PingAsync(Port).ConfigureAwait(false)) return null;
            await Task.Delay(300).ConfigureAwait(false);
        }
        return "Browser host did not become ready within 30s. Check %TMP%\\nexus-agent\\host.log.";
    }

    // Reused across the readiness poll loop (up to ~100 pings during a cold start)
    // so we don't allocate/dispose a client + handler per 300ms tick.
    private static readonly HttpClient Pinger = new() { Timeout = TimeSpan.FromSeconds(2) };

    private static async Task<bool> PingAsync(int port)
    {
        try
        {
            using var resp = await Pinger.PostAsync($"http://127.0.0.1:{port}/status",
                new StringContent("{}", Encoding.UTF8, "application/json")).ConfigureAwait(false);
            return (int)resp.StatusCode == 200;
        }
        catch { return false; }
    }

    private static string? SpawnHost()
    {
        var exe = Environment.ProcessPath;
        if (string.IsNullOrEmpty(exe))
            return "Could not determine the nexus-agent executable to spawn the host.";

        try
        {
            var psi = new ProcessStartInfo
            {
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            // When launched via `dotnet nexus-agent.dll`, ProcessPath is dotnet — relaunch the DLL.
            if (string.Equals(Path.GetFileNameWithoutExtension(exe), "dotnet", StringComparison.OrdinalIgnoreCase))
            {
                psi.FileName = exe;
                psi.ArgumentList.Add(Path.Combine(AppContext.BaseDirectory, "nexus-agent.dll"));
            }
            else
            {
                psi.FileName = exe;
            }

            psi.ArgumentList.Add("--host-mode");
            psi.ArgumentList.Add(Port.ToString());

            // Dispose only frees the local handle; the detached host keeps running.
            using (Process.Start(psi)) { }
            return null;
        }
        catch (Exception ex)
        {
            return $"Failed to spawn browser host: {ex.Message}";
        }
    }
}
