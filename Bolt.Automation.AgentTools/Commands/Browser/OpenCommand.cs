using Bolt.Automation.AgentTools.Browser.Client;
using CommandLine;

namespace Bolt.Automation.AgentTools.Commands.Browser;

[Verb("open", HelpText = "Open a session at a URL with tenant/env context but no flow walk — for product areas without a registered flow (ADBX, admin surfaces). Optionally resolve a test user and perform the framework's STS login.")]
internal sealed class OpenOptions
{
    [Option("tenant", Required = true, HelpText = "Tenant enum value, e.g. BOLTACCESS.")]
    public string Tenant { get; set; } = "";

    [Option("env", Required = true, HelpText = "Environment, e.g. QA.")]
    public string Env { get; set; } = "";

    [Option("url", HelpText = "Destination URL. Defaults to the resolved user's LoginUrl when --user is given.")]
    public string? Url { get; set; }

    [Option("user", HelpText = "User from the tenant/env UserTestDataCollection (e.g. RootAdmin, MfaPrincipal). Sets the session's current user; its LoginUrl is the default destination.")]
    public string? User { get; set; }

    [Option("login", HelpText = "After navigating, perform the framework's STS login with the resolved user's stored credentials (requires --user).")]
    public bool Login { get; set; }

    [Option("front-end", HelpText = "FrontEndType for the session (ADBX|D2C|Interview|...).")]
    public string? FrontEnd { get; set; }

    [Option("headed", HelpText = "Launch browser visibly (default for interactive debug).")]
    public bool Headed { get; set; }

    [Option("timeout", Default = 120, HelpText = "Max seconds for navigate (+ login) before the host returns open_timeout (browser left open). Default 120.")]
    public int TimeoutSeconds { get; set; } = 120;
}

internal static class OpenCommand
{
    public static async Task<int> ExecuteAsync(OpenOptions o)
    {
        if (string.IsNullOrWhiteSpace(o.Url) && string.IsNullOrWhiteSpace(o.User))
            return await CommandBase.EmitErrorAsync("input_error",
                "open requires --url, --user (whose LoginUrl is used), or both.", exitCode: 3);
        if (o.Login && string.IsNullOrWhiteSpace(o.User))
            return await CommandBase.EmitErrorAsync("input_error",
                "--login requires --user — the framework logs in with that user's stored credentials.", exitCode: 3);

        var payload = new
        {
            tenant = o.Tenant,
            env = o.Env,
            url = o.Url,
            user = o.User,
            login = o.Login,
            front_end = o.FrontEnd,
            headed = o.Headed,
            timeout_seconds = o.TimeoutSeconds,
        };

        var result = await HostClient.PostAsync("open", payload, TimeSpan.FromSeconds(o.TimeoutSeconds + 60));
        return BrowserClientOutput.Emit(result);
    }
}
