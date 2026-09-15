using System.Text.Json;
using Bolt.Automation.AgentTools.Browser;
using Bolt.Automation.AgentTools.Browser.Client;
using CommandLine;

namespace Bolt.Automation.AgentTools.Commands.Browser;

[Verb("record", HelpText = "Launch the native Playwright codegen recorder for exploratory capture. With --session, the recorder inherits the live session's auth (storage state) and starts at its current URL; recorded actions stream to a C# file for the agent to clean into a scenario. Mid-recording, typing ##text## into any input marks a verification checkpoint.")]
internal sealed class RecordOptions
{
    [Option("session", HelpText = "Live session id to record from — its cookies/localStorage are loaded into the recorder browser and its current URL is the start point.")]
    public string? Session { get; set; }

    [Option("url", HelpText = "Start URL for the recorder. Required without --session; overrides the session's current URL when both are given.")]
    public string? Url { get; set; }

    [Option("output", HelpText = "File the recorded C# actions are written to. Default: .qa-scenarios/recordings/recording-<timestamp>.cs")]
    public string? Output { get; set; }

    [Option("channel", HelpText = "Browser channel for the recorder (chrome, msedge, …). Default: the donor session's channel with --session; without one, pass this explicitly on machines that don't have Playwright's bundled Chromium installed.")]
    public string? Channel { get; set; }
}

internal static class RecordCommand
{
    public static async Task<int> ExecuteAsync(RecordOptions o)
    {
        if (string.IsNullOrWhiteSpace(o.Session) && string.IsNullOrWhiteSpace(o.Url))
            return await CommandBase.EmitErrorAsync("input_error",
                "record requires --session (start where the live session is) or --url.", exitCode: 3);

        var url = o.Url;
        string? storagePath = null;
        string? tenant = null, env = null;
        var channel = o.Channel;
        var donorHeaded = false;

        if (!string.IsNullOrWhiteSpace(o.Session))
        {
            var prep = await HostClient.PostAsync("record-prep", new { session = o.Session });
            if (!prep.Ok)
                return BrowserClientOutput.Emit(prep);

            using var doc = JsonDocument.Parse(prep.Json);
            var root = doc.RootElement;
            storagePath = root.GetProperty("storage_state_path").GetString();
            url ??= root.GetProperty("current_url").GetString();
            tenant = root.TryGetProperty("tenant", out var t) ? t.GetString() : null;
            env = root.TryGetProperty("env", out var e) ? e.GetString() : null;
            channel ??= root.TryGetProperty("channel", out var c) ? c.GetString() : null;
            donorHeaded = root.TryGetProperty("headed", out var h) && h.ValueKind == JsonValueKind.True;
        }

        if (string.IsNullOrWhiteSpace(url) || url.StartsWith("about:", StringComparison.OrdinalIgnoreCase))
            return await CommandBase.EmitErrorAsync("input_error",
                "The session's current page is blank (about:blank) — its browser window was probably closed. "
                + "Pass --url with the intended start URL (e.g. the quote deeplink), or open a fresh session.", exitCode: 3);

        var output = o.Output;
        if (string.IsNullOrWhiteSpace(output))
            output = Path.Combine(".qa-scenarios", "recordings", $"recording-{DateTime.Now:yyyyMMdd-HHmmss}.cs");
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);

        // Provenance sidecar — distillation reads it to know where the recording came from.
        var metaPath = output + ".meta.json";
        var startedAt = DateTime.UtcNow;
        await File.WriteAllTextAsync(metaPath, JsonSerializer.Serialize(new
        {
            session = o.Session,
            tenant,
            env,
            channel,
            start_url = url,
            authenticated = storagePath != null,
            donor_headed = donorHeaded,
            started_at_utc = startedAt.ToString("o"),
        }, new JsonSerializerOptions { WriteIndented = true }));

        var args = new List<string> { "codegen", "--target", "csharp", "-o", output };
        // No channel = Playwright's bundled Chromium, which QA machines may not have
        // (framework sessions run on a channel browser) — mirror the session's channel.
        if (!string.IsNullOrEmpty(channel))
        {
            args.Add("--channel");
            args.Add(channel);
        }
        if (!string.IsNullOrEmpty(storagePath))
        {
            args.Add("--load-storage");
            args.Add(storagePath);
        }
        args.Add(url);

        // Blocks until the QA closes the recorder browser/inspector — that IS the
        // recording session. Codegen streams the generated actions into --output.
        var exit = Microsoft.Playwright.Program.Main(args.ToArray());

        if (!File.Exists(output) || new FileInfo(output).Length == 0)
        {
            // Don't orphan the provenance sidecar of a recording that never existed.
            try { File.Delete(metaPath); } catch { /* best-effort */ }
            return await CommandBase.EmitErrorAsync("recording_empty",
                "The recorder exited but produced no recorded actions — was the browser closed without interacting?",
                exitCode: 2, detail: new { output, codegen_exit = exit });
        }

        var actions = RecordingParser.Parse(await File.ReadAllLinesAsync(output));
        var meaningful = actions.Count(a => a.Action is not ("goto" or "close" or "unparsed"));

        return await CommandBase.EmitJsonAsync(new
        {
            output = Path.GetFullPath(output),
            meta = Path.GetFullPath(metaPath),
            action_count = actions.Count,
            meaningful_actions = meaningful,
            marker_count = actions.Count(a => a.Action == "marker"),
            start_url = url,
            channel = channel ?? "chromium (bundled)",
            authenticated = storagePath != null,
            donor_headed_warning = donorHeaded
                ? "The donor session was HEADED — two look-alike windows were on screen during recording. Prefer a headless session next time (skill Phase 2b)."
                : null,
            codegen_exit = exit,
            hint = "Run `browser parse-recording --file <output>` for the structured action list.",
        });
    }
}
