namespace Bolt.Automation.AgentTools.Code;

/// <summary>
/// Maps a changed source-file path to the KB philosophy/framework areas a
/// reviewer should weigh when judging the change. Static table — grep-grade,
/// not semantic; the nexus-framework-review skill uses it to surface "this
/// change touches the fluent-page-object contract" style context.
/// </summary>
internal static class PhilosophyAreaMap
{
    // Order matters: first matching rule wins its areas; all matching rules
    // contribute (deduped). Path comparison is case-insensitive, '/'-normalized.
    private static readonly (string Fragment, string[] Areas)[] Rules =
    {
        ("/formdata/fieldregistry", new[] { "framework:field-registry", "philosophy:sparse-dictionaries" }),
        ("fieldregistry",          new[] { "framework:field-registry", "philosophy:sparse-dictionaries" }),
        ("/formdata/",             new[] { "framework:field-registry", "framework:ui-field-types" }),
        ("/flows/",                new[] { "framework:flows-executor" }),
        ("/executor/",             new[] { "framework:flows-executor" }),
        ("popup",                  new[] { "framework:popups" }),
        ("/pages/",                new[] { "framework:page-objects", "philosophy:fluent-page-objects" }),
        ("page.cs",                new[] { "framework:page-objects", "philosophy:fluent-page-objects" }),
        ("/base/interviewbase",    new[] { "framework:page-objects", "philosophy:fluent-page-objects" }),
        ("/logging/",              new[] { "framework:logging", "philosophy:logging-where-work-happens" }),
        ("logger",                 new[] { "framework:logging", "philosophy:logging-where-work-happens" }),
        ("/bolt.automation.tests/", new[] { "framework:test-class", "philosophy:tests-stay-clean" }),
        ("/apiclients/",           new[] { "domain:tc-api" }),
        ("scopecontext",           new[] { "framework:overview" }),
        ("testinfrastructure",     new[] { "framework:overview" }),
    };

    public static IReadOnlyList<string> AreasFor(string repoRelativePath)
    {
        var p = repoRelativePath.Replace('\\', '/').ToLowerInvariant();
        if (!p.StartsWith('/')) p = "/" + p;

        var areas = new List<string>();
        foreach (var (fragment, ruleAreas) in Rules)
        {
            if (p.Contains(fragment, StringComparison.Ordinal))
                foreach (var a in ruleAreas)
                    if (!areas.Contains(a)) areas.Add(a);
        }
        if (areas.Count == 0) areas.Add("framework:overview");
        return areas;
    }
}
