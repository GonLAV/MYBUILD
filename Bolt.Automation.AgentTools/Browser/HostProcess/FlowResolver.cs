using System.Collections;
using System.Reflection;

namespace Bolt.Automation.AgentTools.Browser.HostProcess;

/// <summary>
/// A flow resolved from the FrontEnds assembly: the actual <see cref="Enum"/>
/// value to hand <c>PlaywrightExecutor.Execute</c>, plus the ordered page-type
/// sequence (Pages[0] is the start page = TStart).
/// </summary>
internal sealed record ResolvedFlow(
    Enum FlowValue,
    string FlowName,
    string? FlowNamespace,
    IReadOnlyList<Type> Pages);

/// <summary>
/// Discovers <c>[FlowInitializer]</c> static builders in the FrontEnds assembly
/// and resolves a flow name (e.g. <c>D2CCondoFlow</c>) to its enum value + page
/// list. Mirrors <see cref="Code.FlowTracer"/> but keeps the live <see cref="Enum"/>
/// value (not just its name) because the executor needs it. Builders are pure —
/// they assemble <c>typeof()</c> tokens and a defaults dict, no browser/DI — so
/// invoking them here is safe.
/// </summary>
internal static class FlowResolver
{
    private const string FlowInitializerAttr = "FlowInitializerAttribute";
    private const string FlowsHelpersType = "FlowsHelpers";

    public static ResolvedFlow? Resolve(Assembly frontEnds, string flowName, out IReadOnlyList<string> available)
    {
        var initializers = Discover(frontEnds);
        available = initializers
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
        if (match.Method == null || match.FlowValue == null) return null;

        var flowObj = match.Method.Invoke(null, null);
        var pagesVal = flowObj?.GetType().GetProperty("Pages")?.GetValue(flowObj);

        var pages = new List<Type>();
        if (pagesVal is IEnumerable seq)
            foreach (var item in seq)
                if (item is Type t)
                    pages.Add(t);

        return new ResolvedFlow(match.FlowValue, match.FlowName!, match.FlowNamespace, pages);
    }

    private static List<(MethodInfo Method, Enum? FlowValue, string? FlowName, string? FlowNamespace)> Discover(Assembly asm)
    {
        var result = new List<(MethodInfo, Enum?, string?, string?)>();
        foreach (var type in SafeGetTypes(asm))
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
                result.Add((m, flowEnum, flowEnum?.ToString(), flowEnum?.GetType().Namespace));
            }
        }
        return result;
    }

    private static IReadOnlyList<Type> SafeGetTypes(Assembly asm)
    {
        try { return asm.GetTypes(); }
        catch (ReflectionTypeLoadException ex) { return ex.Types.Where(t => t != null).Cast<Type>().ToList(); }
    }
}
