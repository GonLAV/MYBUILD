using Bolt.Automation.AgentTools.Browser.Client;
using CommandLine;

namespace Bolt.Automation.AgentTools.Commands.Browser;

[Verb("navigate", HelpText = "Walk a flow up to a target page; leave the browser open in the host process.")]
internal sealed class NavigateOptions
{
    [Option("flow", Required = true, HelpText = "FlowType enum value, e.g. D2CCondoFlow.")]
    public string Flow { get; set; } = "";

    [Option("tenant", Required = true, HelpText = "Tenant enum value, e.g. BOLTAG.")]
    public string Tenant { get; set; } = "";

    [Option("env", Required = true, HelpText = "Environment, e.g. QA.")]
    public string Env { get; set; } = "";

    [Option("until", Required = true, HelpText = "Target page type name, e.g. D2C_HouseDetailsPage.")]
    public string Until { get; set; } = "";

    [Option("headed", HelpText = "Launch browser visibly (default for interactive debug).")]
    public bool Headed { get; set; }

    [Option("url", HelpText = "Explicit start URL; skips per-product URL resolution.")]
    public string? Url { get; set; }

    [Option("front-end", HelpText = "FrontEndType override (D2C|ADBX|PartnerPortal|HQXConsumer|HQXAgent|Interview); auto-derived from the flow when omitted.")]
    public string? FrontEnd { get; set; }

    [Option("data", HelpText = "Path to a JSON file of field-name → value overrides, merged over the flow's default data during the walk.")]
    public string? DataFile { get; set; }

    [Option("set", HelpText = "Inline Field=Value overrides (repeatable: --set Vin=123 ZipCode=33101). Override the data file on collision.")]
    public IEnumerable<string>? Set { get; set; }

    [Option("timeout", Default = 240, HelpText = "Max seconds for the flow walk before the host returns navigate_timeout (browser left open). Default 240.")]
    public int TimeoutSeconds { get; set; } = 240;
}

internal static class NavigateCommand
{
    public static async Task<int> ExecuteAsync(NavigateOptions o)
    {
        if (string.IsNullOrWhiteSpace(o.Flow) || string.IsNullOrWhiteSpace(o.Tenant) ||
            string.IsNullOrWhiteSpace(o.Env) || string.IsNullOrWhiteSpace(o.Until))
            return await CommandBase.EmitErrorAsync("input_error",
                "navigate requires --flow, --tenant, --env, and --until.", exitCode: 3);

        var (formData, dataError) = QaFormData.Load(o.DataFile, o.Set);
        if (dataError != null)
            return await CommandBase.EmitErrorAsync("input_error", dataError, exitCode: 3);

        var payload = new
        {
            tenant = o.Tenant,
            env = o.Env,
            flow = o.Flow,
            until = o.Until,
            headed = o.Headed,
            url = o.Url,
            front_end = o.FrontEnd,
            form_data = formData?.Count > 0 ? formData : null,
            timeout_seconds = o.TimeoutSeconds,
        };

        // Client waits a bit longer than the host's walk timeout so the host's clean
        // navigate_timeout wins over a blunt client-side cancellation.
        var result = await HostClient.PostAsync("navigate", payload, TimeSpan.FromSeconds(o.TimeoutSeconds + 60));
        return BrowserClientOutput.Emit(result);
    }
}
