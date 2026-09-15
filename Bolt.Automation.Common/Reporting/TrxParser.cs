using System.Xml.Linq;

namespace Bolt.Automation.Common.Reporting;

public static class TrxParser
{
    private static readonly XNamespace TrxNs = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";
    private const long MaxTrxFileSize = 50 * 1024 * 1024; // 50 MB safety limit

    public static TrxResult Parse(string trxPath)
    {
        if (!File.Exists(trxPath))
        {
            return new TrxResult
            {
                Total = 1,
                Failed = 1,
                ErrorSummary = $"TRX file not found: {trxPath}",
            };
        }

        var fileSize = new FileInfo(trxPath).Length;
        if (fileSize > MaxTrxFileSize)
        {
            return new TrxResult
            {
                Total = 1,
                Failed = 1,
                ErrorSummary = $"TRX file too large to parse safely: {fileSize:N0} bytes (limit {MaxTrxFileSize:N0})",
            };
        }

        try
        {
            var doc = XDocument.Load(trxPath);
            return ParseDocument(doc);
        }
        catch (Exception ex)
        {
            return new TrxResult
            {
                Total = 1,
                Failed = 1,
                ErrorSummary = $"Failed to parse TRX: {ex.Message}",
            };
        }
    }

    private static TrxResult ParseDocument(XDocument doc)
    {
        var root = doc.Root!;

        // Parse counters from ResultSummary
        var counters = root
            .Descendants(TrxNs + "Counters")
            .FirstOrDefault();

        int total = 0, passed = 0, failed = 0, skipped = 0;

        if (counters != null)
        {
            total = IntAttr(counters, "total");
            passed = IntAttr(counters, "passed");
            failed = IntAttr(counters, "failed");
            // "notExecuted" maps to skipped in NUnit TRX output
            skipped = IntAttr(counters, "notExecuted");
        }

        // Parse duration from Times element
        double durationMs = 0;
        var times = root.Descendants(TrxNs + "Times").FirstOrDefault();
        if (times != null)
        {
            var start = DateTimeAttr(times, "start");
            var finish = DateTimeAttr(times, "finish");
            if (start.HasValue && finish.HasValue)
            {
                durationMs = (finish.Value - start.Value).TotalMilliseconds;
            }
        }

        // Collect error messages from failed results
        string? errorSummary = null;
        if (failed > 0)
        {
            var errors = root
                .Descendants(TrxNs + "UnitTestResult")
                .Where(r => (string?)r.Attribute("outcome") == "Failed")
                .SelectMany(r => r.Descendants(TrxNs + "Message"))
                .Select(m => m.Value.Trim())
                .ToList();

            if (errors.Count > 0)
            {
                var joined = string.Join("\n---\n", errors);
                errorSummary = joined.Length > 2000 ? joined[..2000] : joined;
            }
        }

        return new TrxResult
        {
            Total = total,
            Passed = passed,
            Failed = failed,
            Skipped = skipped,
            DurationMs = durationMs,
            ErrorSummary = errorSummary,
        };
    }

    private static int IntAttr(XElement el, string name)
        => int.TryParse((string?)el.Attribute(name), out var v) ? v : 0;

    private static DateTime? DateTimeAttr(XElement el, string name)
        => DateTime.TryParse((string?)el.Attribute(name), out var v) ? v : null;
}
