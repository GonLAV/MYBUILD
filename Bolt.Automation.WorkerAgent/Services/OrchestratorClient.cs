using System.Text.Json;
using System.Text.Json.Serialization;
using Bolt.Automation.WorkerAgent.Models;

namespace Bolt.Automation.WorkerAgent.Services;

public sealed class OrchestratorClient
{
    private readonly HttpClient _http;
    private readonly HttpClient _pollHttp;
    private readonly string _workerId;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>
    /// Two HttpClients share the same base address and default headers but have different
    /// resilience profiles. <paramref name="http"/> uses the tight default (15s attempt,
    /// 45s total, 2 retries) appropriate for short RPCs. <paramref name="pollHttp"/> uses
    /// a relaxed profile for the long-polling <c>/api/work</c> endpoint, which the
    /// orchestrator may hold open for up to ~25s while waiting for work.
    /// </summary>
    public OrchestratorClient(HttpClient http, HttpClient pollHttp, string workerId)
    {
        _http = http;
        _pollHttp = pollHttp;
        _workerId = workerId;
    }

    public async Task<DeregisterResponse> DeregisterAsync(string reason, CancellationToken ct = default)
    {
        var body = new DeregisterRequest(_workerId, reason);
        using var resp = await _http.PostAsJsonAsync("/api/workers/deregister", body, JsonOptions, ct);
        await EnsureSuccessAsync(resp, ct);
        return (await resp.Content.ReadFromJsonAsync<DeregisterResponse>(JsonOptions, ct))!;
    }

    public async Task<HeartbeatResponse> HeartbeatAsync(
        string[] activeWorkItemIds,
        string status,
        CodeVersion codeVersion,
        int readySlots,
        WorkerOptions options,
        CancellationToken ct = default)
    {
        var body = new HeartbeatRequest(
            _workerId, status, activeWorkItemIds, codeVersion,
            options.PodName, options.NodeName, Environment.ProcessorCount,
            options.Concurrency, options.Version, readySlots);
        using var resp = await _http.PostAsJsonAsync("/api/workers/heartbeat", body, JsonOptions, ct);
        await EnsureSuccessAsync(resp, ct);
        return (await resp.Content.ReadFromJsonAsync<HeartbeatResponse>(JsonOptions, ct))!;
    }

    public async Task<WorkResponse> GetWorkAsync(
        string branch,
        string commitHash,
        CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/work");
        request.Headers.Add("X-Worker-Id", _workerId);
        request.Headers.Add("X-Worker-Branch", branch);
        request.Headers.Add("X-Worker-Commit", commitHash);
        using var resp = await _pollHttp.SendAsync(request, ct);
        await EnsureSuccessAsync(resp, ct);
        return (await resp.Content.ReadFromJsonAsync<WorkResponse>(JsonOptions, ct))!;
    }

    public async Task<ResultResponse> ReportResultAsync(SubmitResultRequest result, CancellationToken ct = default)
    {
        using var resp = await _http.PostAsJsonAsync("/api/results", result, JsonOptions, ct);
        await EnsureSuccessAsync(resp, ct);
        return (await resp.Content.ReadFromJsonAsync<ResultResponse>(JsonOptions, ct))!;
    }

    /// <summary>
    /// Returns a claimed work item to the pending queue. Used when this worker
    /// has picked up an item via <see cref="GetWorkAsync"/> but cannot execute it
    /// (slot contention during a sibling slot's rebuild, code version shifted
    /// while awaiting the build, drain signalled post-claim). Idempotent on the
    /// server side: a stale release returns <c>released: false</c>.
    /// </summary>
    public async Task<ReleaseWorkItemResponse> ReleaseWorkItemAsync(
        string workItemId,
        string? reason,
        CancellationToken ct = default)
    {
        var body = new ReleaseWorkItemRequest(_workerId, reason);
        using var resp = await _http.PostAsJsonAsync(
            $"/api/work/{workItemId}/release", body, JsonOptions, ct);
        await EnsureSuccessAsync(resp, ct);
        return (await resp.Content.ReadFromJsonAsync<ReleaseWorkItemResponse>(JsonOptions, ct))!;
    }

    public async Task<BuildStatusResponse> PostBuildStatusAsync(
        BuildStatusRequest request,
        CancellationToken ct = default)
    {
        using var resp = await _http.PostAsJsonAsync($"/api/workers/{_workerId}/build-status", request, JsonOptions, ct);
        await EnsureSuccessAsync(resp, ct);
        return (await resp.Content.ReadFromJsonAsync<BuildStatusResponse>(JsonOptions, ct))!;
    }

    public async Task SendLogsAsync(LogBatchRequest batch, CancellationToken ct = default)
    {
        using var resp = await _http.PostAsJsonAsync($"/api/workers/{_workerId}/logs", batch, JsonOptions, ct);
        await EnsureSuccessAsync(resp, ct);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage resp, CancellationToken ct)
    {
        if (resp.IsSuccessStatusCode) return;
        var body = await resp.Content.ReadAsStringAsync(ct);
        throw new HttpRequestException(
            $"HTTP {(int)resp.StatusCode}: {body}", null, resp.StatusCode);
    }
}
