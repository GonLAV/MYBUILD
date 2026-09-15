using Bolt.Automation.AgentTools.Browser.Client;
using CommandLine;

namespace Bolt.Automation.AgentTools.Commands.Browser;

[Verb("quote-start", HelpText = "Create a Progressive consumer quote via the Platform QuoteStart API (full prefill for a named AddressKey) and open its deeplink — the HQX consumer entry path. Step onward with `browser continue`.")]
internal sealed class QuoteStartOptions
{
    [Option("tenant", Required = true, HelpText = "Tenant enum value, e.g. PROGRESSIVEPL.")]
    public string Tenant { get; set; } = "";

    [Option("env", Required = true, HelpText = "Environment, e.g. QA.")]
    public string Env { get; set; } = "";

    [Option("address", Required = true, HelpText = "AddressKey enum value for the quote's property address, e.g. ID, OH, MI, PA_Meadville.")]
    public string Address { get; set; } = "";

    [Option("user", Default = "Consumer", HelpText = "User from the tenant/env UserTestDataCollection carrying API auth. Default: Consumer.")]
    public string User { get; set; } = "Consumer";

    [Option("front-end", Default = "HQXConsumer", HelpText = "FrontEndType for the session. Default: HQXConsumer.")]
    public string FrontEnd { get; set; } = "HQXConsumer";

    [Option("headed", HelpText = "Launch browser visibly (default for interactive debug).")]
    public bool Headed { get; set; }

    [Option("timeout", Default = 180, HelpText = "Max seconds for QuoteStart + navigate before the host returns quote_start_timeout (browser left open). Default 180.")]
    public int TimeoutSeconds { get; set; } = 180;
}

internal static class QuoteStartCommand
{
    public static async Task<int> ExecuteAsync(QuoteStartOptions o)
    {
        var payload = new
        {
            tenant = o.Tenant,
            env = o.Env,
            address = o.Address,
            user = o.User,
            front_end = o.FrontEnd,
            headed = o.Headed,
            timeout_seconds = o.TimeoutSeconds,
        };

        var result = await HostClient.PostAsync("quote-start", payload, TimeSpan.FromSeconds(o.TimeoutSeconds + 60));
        return BrowserClientOutput.Emit(result);
    }
}
