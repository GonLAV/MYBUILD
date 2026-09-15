using Bolt.Automation.WorkerAgent.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Bolt.Automation.WorkerAgent.Health;

/// <summary>
/// Liveness check — returns Unhealthy only when the worker is draining and fully drained,
/// signaling Kubernetes to stop sending traffic and eventually restart.
/// </summary>
public sealed class LivenessCheck(WorkerService worker) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        var data = GetCommonData();

        if (worker.IsDrained)
            return Task.FromResult(HealthCheckResult.Unhealthy("Worker has drained — shutting down", data: data));

        return Task.FromResult(HealthCheckResult.Healthy("Worker is alive", data: data));
    }

    private Dictionary<string, object> GetCommonData() => new()
    {
        ["status"] = worker.CurrentStatus,
        ["activeWorkItems"] = worker.ActiveCount,
    };
}

/// <summary>
/// Readiness check — returns Unhealthy when draining (stop assigning new work)
/// or when all slots are occupied (temporarily not ready for more).
/// </summary>
public sealed class ReadinessCheck(WorkerService worker, IOptions<WorkerOptions> options) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        var opts = options.Value;
        var data = new Dictionary<string, object>
        {
            ["status"] = worker.CurrentStatus,
            ["activeWorkItems"] = worker.ActiveCount,
            ["maxConcurrency"] = opts.Concurrency,
        };

        if (worker.IsDrained)
            return Task.FromResult(HealthCheckResult.Unhealthy("Worker has drained", data: data));

        if (worker.CurrentStatus == "draining")
            return Task.FromResult(HealthCheckResult.Degraded("Worker is draining — no new work accepted", data: data));

        return Task.FromResult(HealthCheckResult.Healthy("Worker is ready", data: data));
    }
}
