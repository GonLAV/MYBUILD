using Bolt.Automation.AgentTools.ApiClients;
using CommandLine;

namespace Bolt.Automation.AgentTools.Commands.Tc;

[Verb("fetch", HelpText = "Fetch a test case by ID from the nexus-logger API; caches locally.")]
internal sealed class FetchOptions
{
    [Value(0, MetaName = "id", Required = true, HelpText = "Numeric test-case ID, e.g. 228954.")]
    public int Id { get; set; }

    [Option("force-refresh", HelpText = "Bypass local cache and re-fetch from the API.")]
    public bool ForceRefresh { get; set; }
}

internal static class FetchCommand
{
    public static async Task<int> ExecuteAsync(FetchOptions options)
    {
        if (options.Id <= 0)
            return await CommandBase.EmitErrorAsync("input_error", "Pass a positive numeric test-case ID.", exitCode: 3);

        var cache = new TcCache();

        // Cache-first unless --force-refresh.
        if (!options.ForceRefresh)
        {
            var cached = cache.TryGet(options.Id);
            if (cached != null)
            {
                return await CommandBase.EmitJsonAsync(new
                {
                    source = "cache",
                    cached_at = cached.CachedAtUtc.ToString("o"),
                    test_case = cached.TestCase,
                }, exitCode: 0);
            }
        }

        var token = TokenStore.Read();
        if (string.IsNullOrWhiteSpace(token))
        {
            return await CommandBase.EmitErrorAsync(
                "no_token",
                "No nexus-logger token configured. Run `tc auth set-token --token <jwt>` (or set NEXUS_TC_TOKEN).",
                exitCode: 2);
        }

        using var client = new NexusLoggerClient(token);
        var outcome = await client.FetchAsync(options.Id);

        switch (outcome.Status)
        {
            case TcFetchStatus.Success:
                cache.Save(options.Id, outcome.TestCase!);
                return await CommandBase.EmitJsonAsync(new
                {
                    source = "api",
                    http_status = outcome.HttpStatus,
                    test_case = outcome.TestCase,
                }, exitCode: 0);

            case TcFetchStatus.Unauthorized:
                return await CommandBase.EmitErrorAsync("unauthorized", outcome.Message!, exitCode: 2,
                    detail: new { http_status = outcome.HttpStatus });

            case TcFetchStatus.NotFound:
                return await CommandBase.EmitJsonAsync(new
                {
                    status = "not_found",
                    id = options.Id,
                    message = outcome.Message,
                }, exitCode: 2);

            default:
                return await CommandBase.EmitErrorAsync(
                    outcome.Status == TcFetchStatus.NetworkError ? "network_error" : "server_error",
                    outcome.Message ?? "Fetch failed.",
                    exitCode: 2,
                    detail: new { http_status = outcome.HttpStatus });
        }
    }
}
