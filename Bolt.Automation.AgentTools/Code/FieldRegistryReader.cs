using System.Collections;
using System.Reflection;

namespace Bolt.Automation.AgentTools.Code;

public sealed record FieldMatch(
    string FrontEnd,
    string Key,
    string? FieldType,
    string? Strategy,
    string? DefaultValue,
    bool Required,
    string? DependsOn,
    IReadOnlyList<string> Pages);

public sealed record FieldLookupResult(
    string Field,
    int MatchCount,
    IReadOnlyList<FieldMatch> Matches,
    IReadOnlyList<string> SearchedFrontEnds);

/// <summary>
/// Reflection view over the FrontEnds field registries. Triggers every
/// <c>[FieldRegistry]</c> static field (which self-registers into
/// FieldRegistryProvider), then reads <c>FieldRegistryProvider.Registries</c>
/// to enumerate all FrontEndType → field maps. There is no global field
/// namespace, so a name can resolve in more than one FrontEnd — all matches
/// are returned, grouped by FrontEnd.
/// </summary>
internal sealed class FieldRegistryReader
{
    private const string FieldRegistryAttr = "FieldRegistryAttribute";
    private const string ProviderType = "Bolt.Automation.FrontEnds.FormData.Base.FieldRegistryProvider";

    private readonly Assembly _asm;

    public FieldRegistryReader(Assembly asm) => _asm = asm;

    public FieldLookupResult Lookup(string fieldName)
    {
        TriggerRegistryFields();

        var registries = ReadRegistries(); // FrontEnd name -> IDictionary
        var matches = new List<FieldMatch>();

        foreach (var (frontEnd, dict) in registries)
        {
            foreach (DictionaryEntry entry in dict)
            {
                if (entry.Key is not string key) continue;
                if (!string.Equals(key, fieldName, StringComparison.OrdinalIgnoreCase)) continue;
                matches.Add(ToMatch(frontEnd, key, entry.Value));
            }
        }

        return new FieldLookupResult(
            Field: fieldName,
            MatchCount: matches.Count,
            Matches: matches.OrderBy(m => m.FrontEnd, StringComparer.Ordinal).ToList(),
            SearchedFrontEnds: registries.Keys.OrderBy(k => k, StringComparer.Ordinal).ToList());
    }

    private static FieldMatch ToMatch(string frontEnd, string key, object? uiElement)
    {
        string? Str(string prop) => uiElement?.GetType().GetProperty(prop)?.GetValue(uiElement)?.ToString();
        bool Bool(string prop) => uiElement?.GetType().GetProperty(prop)?.GetValue(uiElement) is true;

        var pages = new List<string>();
        if (uiElement?.GetType().GetProperty("Pages")?.GetValue(uiElement) is IEnumerable seq)
            foreach (var item in seq)
                if (item is Type t) pages.Add(t.Name);

        return new FieldMatch(
            FrontEnd: frontEnd,
            Key: key,
            FieldType: Str("FieldType"),
            Strategy: Str("Strategy"),
            DefaultValue: Str("DefaultValue"),
            Required: Bool("Required"),
            DependsOn: Str("DependsOn"),
            Pages: pages);
    }

    /// <summary>Force every [FieldRegistry] static field to initialize (self-registers).</summary>
    private void TriggerRegistryFields()
    {
        foreach (var type in ReflectionLoader.SafeGetTypes(_asm))
        {
            FieldInfo[] fields;
            try { fields = type.GetFields(BindingFlags.Public | BindingFlags.Static); }
            catch { continue; }

            foreach (var f in fields)
            {
                var hasAttr = f.GetCustomAttributes(inherit: false)
                    .Any(a => a.GetType().Name == FieldRegistryAttr);
                if (!hasAttr) continue;
                try { _ = f.GetValue(null); } catch { /* skip a registry that fails to init */ }
            }
        }
    }

    private Dictionary<string, IDictionary> ReadRegistries()
    {
        var result = new Dictionary<string, IDictionary>(StringComparer.Ordinal);

        var providerType = _asm.GetType(ProviderType);
        var registriesObj = providerType?.GetProperty("Registries", BindingFlags.Public | BindingFlags.Static)?
            .GetValue(null);
        if (registriesObj is not IEnumerable pairs) return result;

        foreach (var pair in pairs)
        {
            var pt = pair.GetType();
            var key = pt.GetProperty("Key")?.GetValue(pair);
            var value = pt.GetProperty("Value")?.GetValue(pair);
            if (key == null || value is not IDictionary dict) continue;
            result[key.ToString() ?? "?"] = dict;
        }
        return result;
    }
}
