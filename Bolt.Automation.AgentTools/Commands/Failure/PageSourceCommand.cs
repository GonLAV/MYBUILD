using Bolt.Automation.AgentTools.Failure;
using CommandLine;

namespace Bolt.Automation.AgentTools.Commands.Failure;

[Verb("page-source", HelpText = "Return the path + scoped DOM subtree for a failed test's captured page source.")]
internal sealed class PageSourceOptions
{
    [Option("test", Required = true, HelpText = "Fully-qualified test name (or bare method name).")]
    public string Test { get; set; } = string.Empty;

    [Option("scope", Default = "form", HelpText = "DOM scope to return: form | page | all. Default: form.")]
    public string Scope { get; set; } = "form";
}

internal static class PageSourceCommand
{
    // Inline HTML cap so a multi-MB page_source doesn't flood stdout. The path
    // is always returned so the agent can read the full file when needed.
    private const int InlineHtmlCap = 400_000;

    public static Task<int> ExecuteAsync(PageSourceOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Test))
            return CommandBase.EmitErrorAsync("input_error", "Pass --test <fqn>.", exitCode: 3);

        var scope = options.Scope?.ToLowerInvariant() ?? "form";
        if (scope is not ("form" or "page" or "all"))
            return CommandBase.EmitErrorAsync("input_error",
                "scope must be one of: form | page | all.", exitCode: 3, detail: new { scope = options.Scope });

        var artifacts = new ArtifactsLocator();
        var located = new PageSourceLocator(artifacts).Locate(options.Test);

        if (!located.PageSource.Found || located.PageSource.Path == null)
        {
            return CommandBase.EmitJsonAsync(new
            {
                status = "not_found",
                test = options.Test,
                method = located.Method,
                folder = located.FolderName,
                folder_found = located.FolderFound,
                suggestion = located.Suggestion,
                hint = "No page_source_*.html for this test. Run it locally or set NEXUS_TEST_RESULTS.",
            }, exitCode: 2);
        }

        string raw;
        try { raw = System.IO.File.ReadAllText(located.PageSource.Path); }
        catch (Exception ex)
        {
            return CommandBase.EmitErrorAsync("read_error", ex.Message, exitCode: 2,
                detail: new { path = located.PageSource.Path });
        }

        var result = HtmlScoper.Scope(raw, scope);
        var html = result.Html;
        var truncated = html.Length > InlineHtmlCap;
        if (truncated) html = html[..InlineHtmlCap] + "\n<!-- …[truncated] -->";

        return CommandBase.EmitJsonAsync(new
        {
            test = options.Test,
            method = located.Method,
            path = located.PageSource.Path,
            captured_at = located.PageSource.CapturedAt,
            bytes = located.PageSource.Bytes,
            requested_scope = scope,
            effective_scope = result.EffectiveScope,
            note = result.Note,
            truncated,
            html,
        }, exitCode: 0);
    }
}
