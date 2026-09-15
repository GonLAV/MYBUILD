using Bolt.Automation.AgentTools.SessionState;
using Bolt.Automation.Common.Context;
using Bolt.Automation.FrontEnds.Executor;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Microsoft.Extensions.DependencyInjection;

namespace Bolt.Automation.AgentTools.Browser.HostProcess;

/// <summary>
/// A live browser session held open inside the host process. Owns its DI scope
/// (kept alive for the session's lifetime — disposing it tears down the browser),
/// the bound <see cref="IBrowserManager"/>/<see cref="PlaywrightExecutor"/> pair,
/// the scope context, and the persisted snapshot. One per <c>browser navigate</c>.
/// </summary>
internal sealed class BrowserSession(
    string id,
    IServiceScope scope,
    IBrowserManager browserManager,
    PlaywrightExecutor executor,
    IScopeContext scopeContext,
    ScopeSnapshot snapshot,
    string? channel) : IDisposable
{
    public string Id { get; } = id;
    public IServiceScope Scope { get; } = scope;
    public IBrowserManager BrowserManager { get; } = browserManager;
    public PlaywrightExecutor Executor { get; } = executor;
    public IScopeContext ScopeContext { get; } = scopeContext;
    public ScopeSnapshot Snapshot { get; } = snapshot;

    /// <summary>
    /// Browser channel this session launches with (chrome / msedge / …); null when it
    /// uses Playwright's bundled Chromium. Recorders must launch the SAME browser —
    /// bundled Chromium may not be installed on QA machines at all.
    /// </summary>
    public string? Channel { get; } = channel;

    /// <summary>
    /// False when the browser/driver is gone (crashed, killed, or fully closed).
    /// A page parked on about:blank is still REACHABLE — the browser is alive.
    /// Consumed by <c>list</c>'s state field and the host's idle-exit gate.
    /// </summary>
    public bool IsReachable
    {
        get
        {
            try { return BrowserManager.GetCurrentTab()?.Url != null; }
            catch { return false; }
        }
    }

    public void Dispose()
    {
        // Browser first (closes Chromium), then the scope.
        try { (BrowserManager as IDisposable)?.Dispose(); } catch { /* already torn down */ }
        try { Scope.Dispose(); } catch { /* already torn down */ }
    }
}
