using Bolt.Automation.AgentTools.Failure;
using CommandLine;

namespace Bolt.Automation.AgentTools.Commands.Failure;

[Verb("summarize", HelpText = "Correlate TRX + page_source + screenshot for a failed test; return structured diagnosis.")]
internal sealed class SummarizeOptions
{
    [Option("test", Required = true, HelpText = "Fully-qualified test name (or bare method name), e.g. Bolt.Automation.Tests.Tests.KLX.KLX_CLInterviewTests.KLX_CLAuto_OldInterview_E2E_SubmitQuote.")]
    public string Test { get; set; } = string.Empty;
}

internal static class SummarizeCommand
{
    public static Task<int> ExecuteAsync(SummarizeOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Test))
            return CommandBase.EmitErrorAsync("input_error", "Pass --test <fqn>.", exitCode: 3);

        var artifacts = new ArtifactsLocator();
        var correlator = new FailureCorrelator(artifacts);
        var diagnosis = correlator.Correlate(options.Test);

        var anythingFound = diagnosis.Trx.Found || diagnosis.PageSource.Found || diagnosis.Screenshot.Found;
        if (!anythingFound)
        {
            return CommandBase.EmitJsonAsync(new
            {
                status = "not_found",
                test = diagnosis.Test,
                method = diagnosis.Method,
                suggestion = diagnosis.Suggestion,
                searched_repo_root = artifacts.RepoRoot,
                hint = "No TRX result, page source, or screenshot found. Run the test locally first, "
                     + "or set NEXUS_TEST_RESULTS to the artifacts directory.",
            }, exitCode: 2);
        }

        return CommandBase.EmitJsonAsync(diagnosis, exitCode: 0);
    }
}
