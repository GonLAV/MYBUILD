namespace Bolt.Automation.AgentTools.Failure;

/// <summary>
/// The novel value-add: cross-references a test's TRX outcome with its captured
/// page_source and screenshot into a single diagnosis. Today the codebase has
/// no such correlation — an engineer manually hunts the TRX, then the
/// page_source folder, then the screenshot. This collapses that to one call.
/// </summary>
internal sealed class FailureCorrelator
{
    private readonly TrxScanner _trx;
    private readonly PageSourceLocator _pageSource;

    public FailureCorrelator(ArtifactsLocator artifacts)
    {
        _trx = new TrxScanner(artifacts);
        _pageSource = new PageSourceLocator(artifacts);
    }

    public FailureDiagnosis Correlate(string fqnOrMethod)
    {
        var trx = _trx.FindTest(fqnOrMethod);
        var artifacts = _pageSource.Locate(fqnOrMethod);

        return new FailureDiagnosis(
            Test: fqnOrMethod,
            Method: artifacts.Method,
            Class: trx.ClassName,
            Trx: trx,
            PageSource: artifacts.PageSource,
            Screenshot: artifacts.Screenshot,
            Suggestion: artifacts.Suggestion);
    }
}
