using Bolt.Automation.AgentTools.Browser.Client;
using CommandLine;

namespace Bolt.Automation.AgentTools.Commands.Browser;

[Verb("open-quote", HelpText = "Create a GetQuote application (OnlineQuote source) and open it directly — mirrors a D2C OnlineQuote test (e.g. 237026). Lands on the resulting page without walking the QQ flow.")]
internal sealed class OpenQuoteOptions
{
    [Option("tenant", Required = true, HelpText = "Tenant enum value, e.g. USAA.")]
    public string Tenant { get; set; } = "";

    [Option("env", Required = true, HelpText = "Environment, e.g. QA.")]
    public string Env { get; set; } = "";

    [Option("quote-file", HelpText = "Path to a JSON file: the full GetQuote request (ApplicationRequestModel: { \"Products\": [...], \"Data\": { ... } }), or — with --provider — a flat overrides object merged over the framework-built data.")]
    public string? QuoteFile { get; set; }

    [Option("provider", HelpText = "Build Data via the framework's data provider instead of a full payload: personal-home (PersonalLineDataProvider.GetPersonalHomeData). --quote-file then supplies overrides (an Address property is passed to the provider).")]
    public string? Provider { get; set; }

    [Option("append-url", Default = "&forcePageSkipping=true", HelpText = "Query string appended to the questionnaire URL. Default: &forcePageSkipping=true.")]
    public string AppendUrl { get; set; } = "&forcePageSkipping=true";

    [Option("front-end", Default = "D2C", HelpText = "FrontEndType for the session (D2C|HQXConsumer|Interview|...). Default: D2C.")]
    public string FrontEnd { get; set; } = "D2C";

    [Option("user", Default = "OnlineQuote", HelpText = "User from the tenant/env UserTestDataCollection that carries auth (e.g. OnlineQuote, LakeviewConsumer, Consumer). Default: OnlineQuote.")]
    public string User { get; set; } = "OnlineQuote";

    [Option("line", Default = "personal", HelpText = "Quote data shape: personal (PersonalLineData) or commercial (CommercialLineData). Default: personal.")]
    public string Line { get; set; } = "personal";

    [Option("headed", HelpText = "Launch browser visibly (default for interactive debug).")]
    public bool Headed { get; set; }

    [Option("applicant", HelpText = "Create a random applicant first (GetQuote /applicants) and set its id on the application request.")]
    public bool CreateApplicant { get; set; }

    [Option("submit", HelpText = "Submit the application after create and poll the submission until completed; quote statuses are reported.")]
    public bool Submit { get; set; }

    [Option("no-open", HelpText = "API-only setup: skip the questionnaire and don't open a browser — use when an agent-side session continues the quote (e.g. MarketsLib dashboard).")]
    public bool NoOpen { get; set; }

    [Option("timeout", Default = 180, HelpText = "Max seconds for the quote API chain (+ navigate) before the host returns. Default 180; raise with --submit (polling can be slow).")]
    public int TimeoutSeconds { get; set; } = 180;
}

internal static class OpenQuoteCommand
{
    public static async Task<int> ExecuteAsync(OpenQuoteOptions o)
    {
        if (string.IsNullOrWhiteSpace(o.Tenant) || string.IsNullOrWhiteSpace(o.Env))
            return await CommandBase.EmitErrorAsync("input_error", "open-quote requires --tenant and --env.", exitCode: 3);
        if (string.IsNullOrWhiteSpace(o.QuoteFile) && string.IsNullOrWhiteSpace(o.Provider))
            return await CommandBase.EmitErrorAsync("input_error",
                "open-quote requires --quote-file (full payload) or --provider (framework-built payload; --quote-file then holds overrides).", exitCode: 3);

        var quoteJson = string.Empty;
        if (!string.IsNullOrWhiteSpace(o.QuoteFile))
        {
            if (!File.Exists(o.QuoteFile))
                return await CommandBase.EmitErrorAsync("input_error", $"Quote file not found: {o.QuoteFile}", exitCode: 3);
            quoteJson = await File.ReadAllTextAsync(o.QuoteFile);
        }

        var payload = new
        {
            tenant = o.Tenant,
            env = o.Env,
            quote_json = quoteJson,
            provider = o.Provider,
            append_url = o.AppendUrl,
            front_end = o.FrontEnd,
            user = o.User,
            line = o.Line,
            headed = o.Headed,
            create_applicant = o.CreateApplicant,
            submit = o.Submit,
            no_open = o.NoOpen,
            timeout_seconds = o.TimeoutSeconds,
        };

        var result = await HostClient.PostAsync("open-quote", payload, TimeSpan.FromSeconds(o.TimeoutSeconds + 60));
        return BrowserClientOutput.Emit(result);
    }
}
