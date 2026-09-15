using Bolt.Automation.Common.Enums;

namespace Bolt.Automation.AgentTools.Browser.HostProcess;

/// <summary>
/// Maps a resolved flow to its <see cref="FrontEndType"/> so the host can set
/// <c>ScopeContext.FrontEnd</c> — which the framework otherwise can't auto-detect
/// (the test base classes set it explicitly, e.g. <c>D2CTestBase</c> sets
/// <c>FrontEndType.D2C</c>). Derived from the flow's namespace/name.
/// </summary>
internal static class FrontEndResolver
{
    public static FrontEndType? Resolve(ResolvedFlow flow)
    {
        var ns = flow.FlowNamespace ?? string.Empty;
        var name = flow.FlowName;

        if (Has(ns, ".D2C.") || name.StartsWith("D2C", StringComparison.OrdinalIgnoreCase))
            return FrontEndType.D2C;

        if (Has(ns, ".Adbx.") || Has(ns, ".ADBX.") || name.Contains("Adbx", StringComparison.OrdinalIgnoreCase))
            return FrontEndType.ADBX;

        if (Has(ns, ".PartnerPortal.") || name.Contains("PartnerPortal", StringComparison.OrdinalIgnoreCase))
            return FrontEndType.PartnerPortal;

        if (Has(ns, ".HQX") || name.Contains("HQX", StringComparison.OrdinalIgnoreCase))
            return name.Contains("Agent", StringComparison.OrdinalIgnoreCase)
                ? FrontEndType.HQXAgent
                : FrontEndType.HQXConsumer;

        if (Has(ns, ".Interview.") || name.Contains("Interview", StringComparison.OrdinalIgnoreCase))
            return FrontEndType.Interview;

        return null;
    }

    private static bool Has(string haystack, string needle) =>
        haystack.Contains(needle, StringComparison.OrdinalIgnoreCase);
}
