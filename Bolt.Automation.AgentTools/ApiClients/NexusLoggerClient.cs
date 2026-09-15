using System.Net.Http.Headers;
using System.Text.Json;
using Refit;

namespace Bolt.Automation.AgentTools.ApiClients;

public enum TcFetchStatus { Success, Unauthorized, NotFound, NetworkError, ServerError }

public sealed record TcFetchOutcome(
    TcFetchStatus Status,
    TestCase? TestCase,
    int? HttpStatus,
    string? Message);

/// <summary>
/// Thin wrapper over <see cref="INexusLoggerApi"/>: builds an HttpClient with the
/// bearer token + base address and maps responses to a <see cref="TcFetchOutcome"/>.
/// nexus-logger is a separate service with user-JWT auth, so it does NOT go
/// through the platform Bolt.Automation.ApiClients auth pipeline.
/// </summary>
public sealed class NexusLoggerClient : IDisposable
{
    private const string BaseUrl = "https://nexus-logger.auto.boltx.us";

    private readonly HttpClient _http;
    private readonly INexusLoggerApi _api;

    public NexusLoggerClient(string token, TimeSpan? timeout = null)
    {
        _http = new HttpClient
        {
            BaseAddress = new Uri(BaseUrl),
            Timeout = timeout ?? TimeSpan.FromSeconds(30),
        };
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var settings = new RefitSettings(
            new SystemTextJsonContentSerializer(new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        _api = RestService.For<INexusLoggerApi>(_http, settings);
    }

    public async Task<TcFetchOutcome> FetchAsync(int id)
    {
        try
        {
            using var resp = await _api.GetTestCaseAsync(id);
            if (resp.IsSuccessStatusCode)
            {
                var tc = resp.Content?.Data;
                return tc == null
                    ? new TcFetchOutcome(TcFetchStatus.ServerError, null, (int)resp.StatusCode, "Empty response body.")
                    : new TcFetchOutcome(TcFetchStatus.Success, tc, (int)resp.StatusCode, null);
            }

            return (int)resp.StatusCode switch
            {
                401 => new TcFetchOutcome(TcFetchStatus.Unauthorized, null, 401,
                    "Token missing or expired. Run `tc auth set-token --token <jwt>` with a fresh token."),
                404 => new TcFetchOutcome(TcFetchStatus.NotFound, null, 404,
                    $"Test case {id} not found (or you lack ADO permission)."),
                _ => new TcFetchOutcome(TcFetchStatus.ServerError, null, (int)resp.StatusCode,
                    (resp.Error as ApiException)?.Content ?? resp.ReasonPhrase ?? "Server error."),
            };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return new TcFetchOutcome(TcFetchStatus.NetworkError, null, null,
                $"Could not reach nexus-logger: {ex.Message}");
        }
    }

    public void Dispose() => _http.Dispose();
}
