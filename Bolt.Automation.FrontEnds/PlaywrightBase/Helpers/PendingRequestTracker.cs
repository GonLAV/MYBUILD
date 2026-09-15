using Bolt.Automation.Common.Logging.Core;
using Microsoft.Playwright;

namespace Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;

public sealed class PendingRequestTracker : IDisposable
{
    private readonly IPage _page;
    private readonly IAutomationLogger? _logger;
    private string? _sameOriginHost;
    private int _inFlight;
    private bool _disposed;

    public PendingRequestTracker(IPage page, IAutomationLogger? logger = null)
    {
        _page = page;
        _logger = logger;
        _sameOriginHost = TryGetHost(page.Url);

        _page.Request += OnRequest;
        _page.RequestFinished += OnRequestFinished;
        _page.RequestFailed += OnRequestFailed;
    }

    private void OnRequest(object? sender, IRequest request)
    {
        if (IsSameOrigin(request.Url))
            Interlocked.Increment(ref _inFlight);
    }

    private void OnRequestFinished(object? sender, IRequest request)
    {
        if (IsSameOrigin(request.Url))
            Interlocked.Decrement(ref _inFlight);
    }

    private void OnRequestFailed(object? sender, IRequest request)
    {
        if (IsSameOrigin(request.Url))
            Interlocked.Decrement(ref _inFlight);
    }

    public async Task WaitForBackendIdleAsync(int budgetMs = 2000, int settleMs = 100)
    {
        // Lazy-init: if constructed while page was at about:blank, re-snapshot on first real wait.
        _sameOriginHost ??= TryGetHost(_page.Url);
        if (_sameOriginHost == null) return;

        var deadline = DateTime.UtcNow.AddMilliseconds(budgetMs);
        while (DateTime.UtcNow < deadline)
        {
            if (Volatile.Read(ref _inFlight) == 0)
            {
                await Task.Delay(settleMs);
                if (Volatile.Read(ref _inFlight) == 0)
                    return;
            }
            await Task.Delay(50);
        }

        _logger?.Debug($"Backend-idle wait exceeded {budgetMs}ms (in-flight={Volatile.Read(ref _inFlight)})");
    }

    private bool IsSameOrigin(string url)
    {
        if (_sameOriginHost == null) return false;
        var host = TryGetHost(url);
        return host != null && string.Equals(host, _sameOriginHost, StringComparison.OrdinalIgnoreCase);
    }

    private static string? TryGetHost(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var u) ? u.Host : null;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try
        {
            _page.Request -= OnRequest;
            _page.RequestFinished -= OnRequestFinished;
            _page.RequestFailed -= OnRequestFailed;
        }
        catch { }
    }
}
