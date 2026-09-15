using System.Reflection;
using System.Text;

namespace Bolt.Automation.AgentTools.Output;

/// <summary>
/// Compact column-aligned text rendering for human readers. Supports a flat
/// object (key/value list) or an <see cref="IEnumerable{T}"/> of objects
/// (column-aligned table). For deeply nested structures, fall back to JSON.
/// </summary>
public sealed class TableOutputFormatter : IOutputFormatter
{
    public string Format(object payload)
    {
        if (payload is System.Collections.IEnumerable enumerable and not string)
        {
            return FormatRows(enumerable);
        }
        return FormatKeyValue(payload);
    }

    private static string FormatKeyValue(object obj)
    {
        var props = obj.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead)
            .ToList();
        if (props.Count == 0) return obj.ToString() ?? string.Empty;

        var keyWidth = props.Max(p => p.Name.Length);
        var sb = new StringBuilder();
        foreach (var p in props)
        {
            var value = p.GetValue(obj)?.ToString() ?? "";
            sb.Append(p.Name.PadRight(keyWidth)).Append("  ").AppendLine(value);
        }
        return sb.ToString();
    }

    private static string FormatRows(System.Collections.IEnumerable rows)
    {
        var list = rows.Cast<object>().ToList();
        if (list.Count == 0) return "(empty)";

        var props = list[0].GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead)
            .ToList();
        if (props.Count == 0) return string.Join(Environment.NewLine, list.Select(x => x.ToString()));

        var widths = props.Select(p => Math.Max(p.Name.Length,
            list.Max(r => p.GetValue(r)?.ToString()?.Length ?? 0))).ToArray();

        var sb = new StringBuilder();
        for (var i = 0; i < props.Count; i++)
            sb.Append(props[i].Name.PadRight(widths[i])).Append("  ");
        sb.AppendLine();
        for (var i = 0; i < props.Count; i++)
            sb.Append(new string('-', widths[i])).Append("  ");
        sb.AppendLine();
        foreach (var row in list)
        {
            for (var i = 0; i < props.Count; i++)
                sb.Append((props[i].GetValue(row)?.ToString() ?? "").PadRight(widths[i])).Append("  ");
            sb.AppendLine();
        }
        return sb.ToString();
    }
}
