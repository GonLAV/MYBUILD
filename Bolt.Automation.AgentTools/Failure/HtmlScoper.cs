using System.Text.RegularExpressions;

namespace Bolt.Automation.AgentTools.Failure;

/// <summary>
/// Extracts a useful subtree from a captured page_source HTML file for agent
/// inspection. Regex-based (no HTML-parser dependency) — pragmatic, matching
/// the project's grep-first stance for v1 diagnostics.
///
///   all  → verbatim file content.
///   page → noise-stripped (script/style/svg/comments removed), &lt;body&gt; only.
///   form → noise-stripped, concatenated &lt;form&gt; blocks; falls back to page
///          scope (with a note) when the page renders no &lt;form&gt; elements.
/// </summary>
internal static class HtmlScoper
{
    private static readonly RegexOptions Opts = RegexOptions.IgnoreCase | RegexOptions.Singleline;
    private static readonly Regex Comments = new("<!--.*?-->", RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex Scripts = new("<script.*?</script>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex Styles = new("<style.*?</style>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex Svgs = new("<svg.*?</svg>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    public sealed record ScopeResult(string Html, string EffectiveScope, string? Note);

    public static ScopeResult Scope(string html, string scope)
    {
        switch (scope?.ToLowerInvariant())
        {
            case "all":
                return new ScopeResult(html, "all", null);

            case "page":
                return new ScopeResult(ExtractBody(StripNoise(html)), "page", null);

            case "form":
            default:
                var stripped = StripNoise(html);
                var forms = Regex.Matches(stripped, "<form.*?</form>", Opts)
                    .Select(m => m.Value)
                    .ToList();
                if (forms.Count > 0)
                    return new ScopeResult(string.Join("\n\n", forms), "form", null);

                return new ScopeResult(
                    ExtractBody(stripped),
                    "page",
                    "No <form> elements found; returning page scope instead.");
        }
    }

    private static string StripNoise(string html)
    {
        html = Comments.Replace(html, string.Empty);
        html = Scripts.Replace(html, string.Empty);
        html = Styles.Replace(html, string.Empty);
        html = Svgs.Replace(html, string.Empty);
        return html;
    }

    private static string ExtractBody(string html)
    {
        var m = Regex.Match(html, "<body.*?</body>", Opts);
        return m.Success ? m.Value : html.Trim();
    }
}
