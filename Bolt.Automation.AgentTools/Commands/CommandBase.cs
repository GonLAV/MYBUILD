using System.Text.Json;

namespace Bolt.Automation.AgentTools.Commands;

/// <summary>
/// Shared helpers for Phase-1 stub commands. As real implementations land
/// (Phases 3-9), command classes will keep using this for consistent output +
/// exit-code handling.
/// </summary>
internal static class CommandBase
{
    private static readonly JsonSerializerOptions PrettyJson = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    /// <summary>
    /// Emits a Phase-1 stub response. Returns exit code 2 (user-facing error)
    /// so callers detect that no real work happened.
    /// </summary>
    public static Task<int> StubAsync(string noun, string verb)
    {
        var payload = new
        {
            Status = "not-implemented",
            Command = $"{noun} {verb}",
            Hint = "Phase 1 scaffold — implementation lands in a later phase.",
        };
        Console.WriteLine(JsonSerializer.Serialize(payload, PrettyJson));
        return Task.FromResult(2);
    }

    /// <summary>
    /// Emits a structured payload as pretty-printed JSON to stdout and returns
    /// the requested exit code. Used by real command implementations (Phase 3+).
    /// </summary>
    public static Task<int> EmitJsonAsync(object payload, int exitCode = 0)
    {
        Console.WriteLine(JsonSerializer.Serialize(payload, PrettyJson));
        return Task.FromResult(exitCode);
    }

    /// <summary>
    /// Emits a structured error payload to stderr (so callers can `2>/dev/null`
    /// to suppress) and returns the requested exit code. The message is
    /// included in the payload so the user sees what went wrong.
    /// </summary>
    public static Task<int> EmitErrorAsync(string status, string message, int exitCode = 2, object? detail = null)
    {
        var payload = detail == null
            ? (object)new { status, message }
            : new { status, message, detail };
        Console.Error.WriteLine(JsonSerializer.Serialize(payload, PrettyJson));
        return Task.FromResult(exitCode);
    }
}
