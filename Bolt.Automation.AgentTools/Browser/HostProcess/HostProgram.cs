using System.Net;

namespace Bolt.Automation.AgentTools.Browser.HostProcess;

/// <summary>
/// The long-lived host: owns Playwright sessions and serves <c>browser</c>
/// commands over <see cref="HttpListener"/> on <c>127.0.0.1:&lt;port&gt;</c>.
/// Entered via <c>nexus-agent --host-mode &lt;port&gt;</c>. Requests are handled
/// serially (a human-driven debug browser needs no request concurrency, and
/// serial handling avoids races on Playwright/<c>FieldRegistryProvider</c> state).
/// Validated by the Phase 0 spike (<c>.skill-explore/spike-pause-resume.md</c>).
/// </summary>
internal static class HostProgram
{
    public static async Task<int> RunAsync(int port)
    {
        Action<string> log = HostLog.Line;

        // The framework's BrowserManager uses sync-over-async (.GetAwaiter().GetResult()
        // inside a lock) during browser/page init. With the default slow-growing thread
        // pool that deadlocks: the blocking thread waits on a continuation that needs a
        // NEW pool thread, and the pool adds threads only ~1 every 0.5s. Pre-seeding worker
        // threads gives those continuations a thread immediately. (UITestBase mitigates the
        // same hazard with a launch semaphore; the host has no NUnit harness, so we seed
        // generously — Playwright's driver + the full nexus DI graph are thread-hungry.)
        ThreadPool.SetMinThreads(256, 256);
        ThreadPool.GetMinThreads(out var minW, out var minIo);
        log($"[host] minthreads worker={minW} iocp={minIo}");

        var prefix = $"http://127.0.0.1:{port}/";

        var listener = new HttpListener();
        listener.Prefixes.Add(prefix);
        try
        {
            listener.Start();
        }
        catch (HttpListenerException ex)
        {
            log($"[host] failed to bind {prefix}: {ex.Message}");
            return 2;
        }

        var registry = new SessionRegistry();
        var controller = new HostController(registry, port, log);
        LockFile.Write(port);
        log($"[host] listening on {prefix} pid={Environment.ProcessId}");

        try
        {
            // Idle-exit: a forgotten host shouldn't sit on the port for days. Exit after
            // 4h without a request — but only when no sessions are open (an open headed
            // browser is a QA mid-verification; never yank it).
            var idleLimit = TimeSpan.FromHours(4);
            var idleTick = TimeSpan.FromMinutes(10);
            var lastRequestUtc = DateTime.UtcNow;

            var keepRunning = true;
            Task<HttpListenerContext>? pending = null;
            while (keepRunning)
            {
                // GetContextAsync can throw synchronously (listener stopped/faulted) —
                // that must stay a graceful "listener ended" exit, not a process crash.
                try { pending ??= listener.GetContextAsync(); }
                catch (Exception ex) { log($"[host] listener ended: {ex.Message}"); break; }

                using var tickCts = new CancellationTokenSource();
                var tick = Task.Delay(idleTick, tickCts.Token);
                var winner = await Task.WhenAny(pending, tick).ConfigureAwait(false);
                if (winner != pending)
                {
                    // Idle tick. Exit only when idle AND no session still has a live
                    // browser — a reachable session (even parked on about:blank) is a
                    // QA mid-something; crashed/orphaned ones must not pin the host.
                    if (DateTime.UtcNow - lastRequestUtc > idleLimit
                        && !registry.All().Any(s => s.IsReachable))
                    {
                        log($"[host] idle for {idleLimit.TotalHours}h with no reachable sessions — exiting");
                        break;
                    }
                    continue;
                }
                tickCts.Cancel(); // free the loser timer immediately — no 10-min timer churn per request

                HttpListenerContext ctx;
                try { ctx = await pending.ConfigureAwait(false); }
                catch (Exception ex) { log($"[host] listener ended: {ex.Message}"); break; }
                finally { pending = null; }

                lastRequestUtc = DateTime.UtcNow;
                keepRunning = await controller.HandleAsync(ctx).ConfigureAwait(false);
            }
        }
        finally
        {
            try { listener.Stop(); } catch { /* ignore */ }
            registry.DisposeAll();
            LockFile.Delete();
            log("[host] bye");
        }

        return 0;
    }
}

/// <summary>
/// Append-only host log at <c>%TMP%\nexus-agent\host.log</c>. The client never
/// reads host stdout (it polls <c>/status</c>), so a file is the durable record;
/// the console write is best-effort and ignored when the host has no console.
/// </summary>
internal static class HostLog
{
    private static readonly object Lock = new();

    private static string Path => System.IO.Path.Combine(SessionState.SessionDirManager.Root, "host.log");

    public static void Line(string message)
    {
        var stamped = $"{DateTime.Now:HH:mm:ss} {message}";
        try { Console.WriteLine(stamped); } catch { /* no console when detached */ }
        try
        {
            lock (Lock)
            {
                Directory.CreateDirectory(SessionState.SessionDirManager.Root);
                File.AppendAllText(Path, stamped + Environment.NewLine);
            }
        }
        catch { /* logging must never throw */ }
    }
}
