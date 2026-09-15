using Bolt.Automation.Common.Models.Urls;

namespace Bolt.Automation.AgentTools.Browser.HostProcess;

/// <summary>
/// Derives the flow's entry URL from the tenant/env <see cref="UrlTestData"/>,
/// mirroring how the test base classes set <c>ScopeContext.CurrentUrl</c>. Start
/// URLs are product-specific, so this handles the common products and otherwise
/// returns an actionable error telling the caller to pass <c>--url</c>.
/// </summary>
internal static class StartUrlResolver
{
    public static (string? Url, string? Error) Resolve(ResolvedFlow flow, UrlTestData? frontEnd)
    {
        if (frontEnd == null)
            return (null, "No FrontEnd URL data for this tenant/env. Pass --url <startUrl> explicitly.");

        var ns = flow.FlowNamespace ?? string.Empty;
        var name = flow.FlowName;

        // D2C — the documented gate. Mirrors D2CTestBase: D2CUrl + "D2CAutomation".
        if (ns.Contains(".D2C.", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("D2C", StringComparison.OrdinalIgnoreCase))
        {
            return string.IsNullOrEmpty(frontEnd.D2CUrl)
                ? (null, "FrontEnd.D2CUrl is empty for this tenant/env. Pass --url <startUrl>.")
                : (frontEnd.D2CUrl + "D2CAutomation", null);
        }

        // Consumer CL interview — mirrors CLTests: AdditionalUrls["ConsumerInterviewCL"].
        if ((ns.Contains(".Interview.", StringComparison.OrdinalIgnoreCase) ||
             name.Contains("Interview", StringComparison.OrdinalIgnoreCase)) &&
            name.Contains("CL", StringComparison.OrdinalIgnoreCase) &&
            frontEnd.AdditionalUrls.TryGetValue("ConsumerInterviewCL", out var clUrl) &&
            !string.IsNullOrEmpty(clUrl))
        {
            return (clUrl, null);
        }

        // Everything else: degrade gracefully with what IS available.
        return (null, BuildHint(frontEnd));
    }

    private static string BuildHint(UrlTestData fe)
    {
        var fields = new List<string>();
        if (!string.IsNullOrEmpty(fe.D2CUrl)) fields.Add("D2CUrl");
        if (!string.IsNullOrEmpty(fe.LoginUrl)) fields.Add("LoginUrl");
        if (!string.IsNullOrEmpty(fe.BaseUrl)) fields.Add("BaseUrl");

        var addl = fe.AdditionalUrls.Keys.OrderBy(k => k, StringComparer.Ordinal).ToList();
        var addlPart = addl.Count > 0 ? $" AdditionalUrls keys: {string.Join(", ", addl)}." : string.Empty;

        return "Could not auto-derive a start URL for this flow. Pass --url <startUrl>. "
             + $"Available FrontEnd fields: {(fields.Count > 0 ? string.Join(", ", fields) : "(none)")}.{addlPart}";
    }
}
