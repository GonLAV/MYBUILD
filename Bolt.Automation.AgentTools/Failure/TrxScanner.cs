using System.Globalization;
using System.Xml.Linq;
using Bolt.Automation.Common.Reporting;

namespace Bolt.Automation.AgentTools.Failure;

/// <summary>
/// Scans recent <c>.trx</c> files for the result of a specific test, returning
/// its outcome, duration, and (on failure) error message + stack trace. The
/// run-level counters come from the shared <see cref="TrxParser"/>; the per-test
/// detail is extracted here because TrxParser only summarizes the whole run.
/// </summary>
internal sealed class TrxScanner
{
    private static readonly XNamespace Ns = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";
    private const long MaxTrxFileSize = 50 * 1024 * 1024;

    private readonly ArtifactsLocator _artifacts;

    public TrxScanner(ArtifactsLocator artifacts) => _artifacts = artifacts;

    public TrxMatch FindTest(string fqnOrMethod)
    {
        var method = PageSourceLocator.DeriveMethodName(fqnOrMethod);

        var matches = new List<TrxMatch>();
        foreach (var trxPath in _artifacts.TrxFiles()) // newest file first
        {
            var match = TryMatchInFile(trxPath, fqnOrMethod, method);
            if (match != null) matches.Add(match);
        }

        if (matches.Count == 0)
            return new TrxMatch(false, null, null, null, null, null, null, null, null, null);

        // Prefer a Failed result ACROSS files, so a later passing re-run doesn't
        // hide the failure we're here to diagnose. OrderByDescending is stable, so
        // among equal outcomes the newest file still wins (TrxFiles is newest-first).
        return matches
            .OrderByDescending(m => string.Equals(m.Outcome, "Failed", StringComparison.OrdinalIgnoreCase))
            .First();
    }

    private TrxMatch? TryMatchInFile(string trxPath, string fqn, string method)
    {
        XDocument doc;
        try
        {
            if (new FileInfo(trxPath).Length > MaxTrxFileSize) return null;
            doc = XDocument.Load(trxPath);
        }
        catch
        {
            return null;
        }

        var root = doc.Root;
        if (root == null) return null;

        // testId -> (className, name) from TestDefinitions.
        var defs = new Dictionary<string, (string? ClassName, string Name)>(StringComparer.OrdinalIgnoreCase);
        foreach (var ut in root.Descendants(Ns + "UnitTest"))
        {
            var id = (string?)ut.Attribute("id");
            var tm = ut.Element(Ns + "TestMethod");
            if (id == null || tm == null) continue;
            defs[id] = ((string?)tm.Attribute("className"), (string?)tm.Attribute("name") ?? string.Empty);
        }

        var candidates = new List<XElement>();
        foreach (var result in root.Descendants(Ns + "UnitTestResult"))
        {
            var resName = (string?)result.Attribute("testName") ?? string.Empty;
            var testId = (string?)result.Attribute("testId") ?? string.Empty;
            defs.TryGetValue(testId, out var def);

            var fqnCandidate = def.ClassName != null ? $"{def.ClassName}.{def.Name}" : null;
            var matchesFqn = fqnCandidate != null && string.Equals(fqnCandidate, fqn, StringComparison.OrdinalIgnoreCase);
            var matchesMethod = BareName(resName).Equals(method, StringComparison.OrdinalIgnoreCase)
                || (!string.IsNullOrEmpty(def.Name) && BareName(def.Name).Equals(method, StringComparison.OrdinalIgnoreCase));

            if (matchesFqn || matchesMethod) candidates.Add(result);
        }

        if (candidates.Count == 0) return null;

        // Prefer a failed result; otherwise the latest by endTime.
        var chosen = candidates
            .OrderByDescending(r => string.Equals((string?)r.Attribute("outcome"), "Failed", StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(r => ParseDate((string?)r.Attribute("endTime")) ?? DateTime.MinValue)
            .First();

        var chosenId = (string?)chosen.Attribute("testId") ?? string.Empty;
        defs.TryGetValue(chosenId, out var chosenDef);

        var errorInfo = chosen.Descendants(Ns + "ErrorInfo").FirstOrDefault();
        var message = errorInfo?.Element(Ns + "Message")?.Value?.Trim();
        var stack = errorInfo?.Element(Ns + "StackTrace")?.Value?.Trim();

        double? durationMs = null;
        if (TimeSpan.TryParse((string?)chosen.Attribute("duration"), CultureInfo.InvariantCulture, out var ts))
            durationMs = ts.TotalMilliseconds;

        return new TrxMatch(
            Found: true,
            TrxPath: trxPath,
            Outcome: (string?)chosen.Attribute("outcome"),
            DurationMs: durationMs,
            StartTime: (string?)chosen.Attribute("startTime"),
            Message: Truncate(message, 4000),
            StackTrace: Truncate(stack, 8000),
            ClassName: chosenDef.ClassName,
            TestName: (string?)chosen.Attribute("testName"),
            RunSummary: SafeRunSummary(trxPath));
    }

    private static TrxResult? SafeRunSummary(string trxPath)
    {
        try { return TrxParser.Parse(trxPath); }
        catch { return null; }
    }

    private static string BareName(string testName)
    {
        var paren = testName.IndexOf('(');
        return paren >= 0 ? testName[..paren] : testName;
    }

    private static DateTime? ParseDate(string? s)
        => DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var d) ? d : null;

    private static string? Truncate(string? s, int max)
    {
        if (string.IsNullOrEmpty(s)) return s;
        return s.Length <= max ? s : s[..max] + "\n…[truncated]";
    }
}
