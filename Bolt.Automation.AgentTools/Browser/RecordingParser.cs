using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Bolt.Automation.AgentTools.Browser;

/// <summary>One parsed action from a Playwright codegen C# recording.</summary>
internal sealed class RecordedAction
{
    public int Index { get; set; }

    /// <summary>goto | click | dblclick | fill | check | uncheck | select | press | type | hover | close | marker | unparsed</summary>
    public string Action { get; set; } = "";

    /// <summary>Fill/select/press value (unescaped). For markers: the marker text.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Value { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public RecordedTarget? Target { get; set; }

    /// <summary>Public Playwright selector-engine string when one exists (role=/text=/css) — feeds `browser raw` directly.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SuggestedSelector { get; set; }

    /// <summary>Destination URL (goto only).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Url { get; set; }

    /// <summary>The original recording line, verbatim — nothing is ever silently dropped.</summary>
    public string Raw { get; set; } = "";
}

/// <summary>The locator a recorded action addressed.</summary>
internal sealed class RecordedTarget
{
    /// <summary>role | label | text | placeholder | testid | title | alttext | css</summary>
    public string Kind { get; set; } = "";

    /// <summary>Role name / label text / css selector — whatever the kind implies.</summary>
    public string Value { get; set; } = "";

    /// <summary>Accessible name for role targets (GetByRole(..., Name = X)).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; set; }

    /// <summary>.Filter(HasText/HasTextRegex) text, when present.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FilterText { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Nth { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? First { get; set; }
}

/// <summary>
/// Deterministic parser for Playwright codegen C# recordings (`browser record` output):
/// each `await page.*` statement becomes a structured action the agent can distill
/// without re-reading raw C#. Verification markers — a fill whose value is
/// <c>##text##</c> — are surfaced as <c>marker</c> actions (the QA's "pause here and
/// check" convention). Tolerant by design: unrecognized statements come back as
/// <c>unparsed</c> with the raw line, never dropped.
/// </summary>
internal static partial class RecordingParser
{
    public static List<RecordedAction> Parse(IEnumerable<string> lines)
    {
        var actions = new List<RecordedAction>();

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (!line.StartsWith("await page.", StringComparison.Ordinal))
                continue;

            RecordedAction? action;
            // Tolerant by design: a line the parser chokes on degrades to `unparsed`,
            // it never aborts the whole recording.
            try { action = ParseLine(line); }
            catch { action = new RecordedAction { Action = "unparsed", Raw = line }; }
            if (action == null) continue;
            action.Index = actions.Count + 1;
            actions.Add(action);
        }

        return actions;
    }

    private static RecordedAction? ParseLine(string line)
    {
        var gotoMatch = GotoRegex().Match(line);
        if (gotoMatch.Success)
            return new RecordedAction { Action = "goto", Url = Unescape(gotoMatch.Groups[1].Value), Raw = line };

        if (line.StartsWith("await page.CloseAsync(", StringComparison.Ordinal))
            return new RecordedAction { Action = "close", Raw = line };

        var actionMatch = ActionRegex().Match(line);
        if (!actionMatch.Success)
            return new RecordedAction { Action = "unparsed", Raw = line };

        var verb = actionMatch.Groups["verb"].Value.ToLowerInvariant() switch
        {
            "selectoption" => "select",
            "presssequentially" => "type", // modern codegen's keystroke-sequence API
            var v => v,
        };
        var value = actionMatch.Groups["value"].Success ? Unescape(actionMatch.Groups["value"].Value) : null;

        // Verification marker: the QA typed ##text## into any input mid-recording.
        if (verb == "fill" && value != null && MarkerRegex().Match(value) is { Success: true } marker)
            return new RecordedAction { Action = "marker", Value = marker.Groups[1].Value.Trim(), Raw = line };

        var target = ParseTarget(line[..actionMatch.Index]);
        return new RecordedAction
        {
            Action = verb,
            Value = value,
            Target = target,
            SuggestedSelector = SuggestSelector(target),
            Raw = line,
        };
    }

    private static RecordedTarget? ParseTarget(string chain)
    {
        RecordedTarget? target = null;

        var role = RoleRegex().Match(chain);
        if (role.Success)
            target = new RecordedTarget
            {
                Kind = "role",
                Value = role.Groups[1].Value.ToLowerInvariant(),
                Name = role.Groups[2].Success ? Unescape(role.Groups[2].Value) : null,
            };

        if (target == null)
        {
            var getBy = GetByRegex().Match(chain);
            if (getBy.Success)
                target = new RecordedTarget
                {
                    Kind = getBy.Groups[1].Value.ToLowerInvariant(),
                    Value = Unescape(getBy.Groups[2].Value),
                };
        }

        if (target == null)
        {
            var css = LocatorRegex().Match(chain);
            if (css.Success)
                target = new RecordedTarget { Kind = "css", Value = Unescape(css.Groups[1].Value) };
        }

        if (target == null) return null;

        var filter = FilterRegex().Match(chain);
        if (filter.Success)
            target.FilterText = Unescape(filter.Groups[1].Value);

        var nth = NthRegex().Match(chain);
        if (nth.Success)
            target.Nth = int.Parse(nth.Groups[1].Value);

        if (chain.Contains(".First", StringComparison.Ordinal))
            target.First = true;

        return target;
    }

    /// <summary>Public selector-engine equivalent, when one exists — plain css, role=, text=.
    /// Modified targets (Filter/Nth/First) and label/placeholder/testid kinds return null:
    /// they need a human (or registry) decision before replay.</summary>
    private static string? SuggestSelector(RecordedTarget? t)
    {
        if (t == null || t.FilterText != null || t.Nth != null || t.First == true) return null;
        return t.Kind switch
        {
            "css" => t.Value,
            "text" => $"text={t.Value}",
            // Escape embedded quotes so a name like: Don't show "Save" again
            // still yields a well-formed selector for `browser raw`.
            "role" when t.Name != null => $"role={t.Value}[name=\"{t.Name.Replace("\"", "\\\"")}\"]",
            "role" => $"role={t.Value}",
            _ => null,
        };
    }

    /// <summary>
    /// Decodes C# string-literal escapes (the grammar codegen actually emits:
    /// \" \\ \n \r \t \0 \uXXXX). NOT Regex.Unescape — that implements the regex
    /// escape grammar and throws on sequences like \U0001F600. Unknown escapes
    /// pass through verbatim; this method never throws.
    /// </summary>
    private static string Unescape(string s)
    {
        if (!s.Contains('\\')) return s;
        var sb = new System.Text.StringBuilder(s.Length);
        for (var i = 0; i < s.Length; i++)
        {
            if (s[i] != '\\' || i + 1 >= s.Length) { sb.Append(s[i]); continue; }
            var next = s[++i];
            switch (next)
            {
                case '"': sb.Append('"'); break;
                case '\\': sb.Append('\\'); break;
                case 'n': sb.Append('\n'); break;
                case 'r': sb.Append('\r'); break;
                case 't': sb.Append('\t'); break;
                case '0': sb.Append('\0'); break;
                case 'u' when i + 4 < s.Length
                    && ushort.TryParse(s.AsSpan(i + 1, 4), System.Globalization.NumberStyles.HexNumber, null, out var code):
                    sb.Append((char)code);
                    i += 4;
                    break;
                default:
                    sb.Append('\\').Append(next); // unknown escape — keep verbatim
                    break;
            }
        }
        return sb.ToString();
    }

    [GeneratedRegex("""^await page\.GotoAsync\("((?:[^"\\]|\\.)*)"\)""")]
    private static partial Regex GotoRegex();

    [GeneratedRegex("""\.(?<verb>Click|DblClick|Fill|Check|Uncheck|SelectOption|PressSequentially|Press|Type|Hover)Async\((?:"(?<value>(?:[^"\\]|\\.)*)")?""")]
    private static partial Regex ActionRegex();

    [GeneratedRegex(@"^\s*##(.+?)##\s*$")]
    private static partial Regex MarkerRegex();

    [GeneratedRegex("""GetByRole\(AriaRole\.(\w+)(?:,\s*new\(\)\s*\{[^}]*Name\s*=\s*"((?:[^"\\]|\\.)*)"[^}]*\})?\)""")]
    private static partial Regex RoleRegex();

    [GeneratedRegex("""GetBy(Label|Text|Placeholder|TestId|Title|AltText)\("((?:[^"\\]|\\.)*)"[^)]*\)""")]
    private static partial Regex GetByRegex();

    [GeneratedRegex("""Locator\("((?:[^"\\]|\\.)*)"\)""")]
    private static partial Regex LocatorRegex();

    [GeneratedRegex("""\.Filter\(new\(\)\s*\{\s*HasText(?:Regex)?\s*=\s*(?:new Regex\()?"((?:[^"\\]|\\.)*)"\)?\s*\}\)""")]
    private static partial Regex FilterRegex();

    [GeneratedRegex(@"\.Nth\((\d+)\)")]
    private static partial Regex NthRegex();
}
