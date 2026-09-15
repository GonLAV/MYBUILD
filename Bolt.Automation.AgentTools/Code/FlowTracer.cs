using System.Collections;
using System.Reflection;

namespace Bolt.Automation.AgentTools.Code;

public sealed record FlowPage(string Name, string? FullName);

public sealed record FlowTraceResult(
    bool Found,
    string Requested,
    string? FlowType,
    string? FlowTypeNamespace,
    string? DeclaringType,
    IReadOnlyList<FlowPage> Pages,
    IReadOnlyList<string> AvailableFlows);

/// <summary>
/// Discovers <c>[FlowInitializer]</c> static methods across the FrontEnds
/// assembly and, for a requested flow, invokes the (pure) builder to read its
/// page sequence. Builders only assemble typeof() tokens + a defaults dict — no
/// browser or DI — so invoking them is safe.
/// </summary>
internal sealed class FlowTracer
{
    private const string FlowInitializerAttr = "FlowInitializerAttribute";
    private const string FlowsHelpersType = "FlowsHelpers";

    private readonly Assembly _asm;

    public FlowTracer(Assembly asm) => _asm = asm;

    public FlowTraceResult Trace(string flowName)
    {
        var initializers = DiscoverInitializers();
        var available = initializers
            .Select(i => i.FlowName)
            .Where(n => n != null)
            .Distinct()
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList()!;

        var match = initializers.FirstOrDefault(i =>
            string.Equals(i.FlowName, flowName, StringComparison.OrdinalIgnoreCase));
        if (match.Method == null)
            match = initializers.FirstOrDefault(i =>
                string.Equals(i.Method?.Name, flowName, StringComparison.OrdinalIgnoreCase));

        if (match.Method == null)
            return new FlowTraceResult(false, flowName, null, null, null, Array.Empty<FlowPage>(), available!);

        var pages = new List<FlowPage>();
        var flowObj = match.Method.Invoke(null, null);
        var pagesProp = flowObj?.GetType().GetProperty("Pages")?.GetValue(flowObj);
        if (pagesProp is IEnumerable seq)
        {
            foreach (var item in seq)
                if (item is Type t)
                    pages.Add(new FlowPage(t.Name, t.FullName));
        }

        return new FlowTraceResult(
            Found: true,
            Requested: flowName,
            FlowType: match.FlowName,
            FlowTypeNamespace: match.FlowNamespace,
            DeclaringType: match.Method.DeclaringType?.FullName,
            Pages: pages,
            AvailableFlows: available!);
    }

    private List<(MethodInfo Method, string? FlowName, string? FlowNamespace)> DiscoverInitializers()
    {
        var result = new List<(MethodInfo, string?, string?)>();
        foreach (var type in ReflectionLoader.SafeGetTypes(_asm))
        {
            MethodInfo[] methods;
            try { methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static); }
            catch { continue; }

            foreach (var m in methods)
            {
                if (m.ReturnType.Name != FlowsHelpersType) continue;
                if (m.GetParameters().Length != 0) continue;

                var attr = m.GetCustomAttributes(inherit: false)
                    .FirstOrDefault(a => a.GetType().Name == FlowInitializerAttr);
                if (attr == null) continue;

                var flowEnum = attr.GetType().GetProperty("FlowType")?.GetValue(attr) as Enum;
                result.Add((m, flowEnum?.ToString(), flowEnum?.GetType().Namespace));
            }
        }
        return result;
    }
}
