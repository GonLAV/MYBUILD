using System.Collections.Concurrent;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Bolt.Automation.Common.Enums;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.InternalServices.Database.Queries.Main.ResultData;

// Add this for CoverageData, CoverageSet, CoverageFieldResult, CoverageCustomType

namespace Bolt.Automation.InternalServices.Database.DBHelpers.ResultData;

/// <summary>
/// Implements ResultData related business logic on top of raw queries.
/// </summary>
public sealed class ResultDataHelper(ResultDataQueries resultDataQueries, IAutomationLogger logger)
{
    // Simple in-process cache (lifecycle scoped). Replace with MemoryCache if expiry needed later.
    private static readonly ConcurrentDictionary<(string ExternalId, CarrierEnums Carrier), CoverageSet> _cache = new();

    public async Task<decimal?> GetWindHailPercentAsync(string externalId, CarrierEnums carrier, CancellationToken ct = default)
        => await GetPercentFromFreeTextAsync(externalId, carrier, CoverageEnums.WindHail, ct);

    /// <summary>
    /// Whether the carrier returned Wind/Hail as Not-Applicable for this quote, read from the request
    /// attachment (type 2): N/A ⟺ <c>XWindFlag == false</c> AND no <c>WindHailDeductible</c> present.
    /// Returns <c>null</c> when it can't be determined (no attachment / unparseable).
    /// See kb partner:pgr-covmod-wind-hail-na for why the coverage-XML NA flag isn't sufficient.
    /// </summary>
    public async Task<bool?> IsWindHailNotApplicableAsync(string externalId, CarrierEnums carrier, CancellationToken ct = default)
    {
        var attachments = await resultDataQueries.GetRequestAttachmentsAsync(externalId, carrier, ct);
        if (attachments.Count == 0)
        {
            logger.Info($"ResultDataHelper: no request attachment (type 2) for {externalId} {carrier}; cannot determine Wind/Hail N/A.");
            return null;
        }

        // Prefer the selected offer, mirroring the coverage-attachment selection.
        var chosen = attachments.OrderByDescending(a => a.Selected).ThenBy(a => a.Premium ?? decimal.MaxValue).First();

        XDocument doc;
        try
        {
            doc = XDocument.Parse(chosen.XmlContent);
        }
        catch (Exception ex)
        {
            logger.LogException(ex, "ResultDataHelper: parsing request attachment (type 2) failed");
            return null;
        }

        var xWindFlag = doc.Descendants("XWindFlag").FirstOrDefault()?.Value?.Trim();
        var hasXWindFalse = string.Equals(xWindFlag, "false", StringComparison.OrdinalIgnoreCase);
        var hasWindHailDeductible = doc.Descendants("WindHailDeductible")
            .Any(e => !string.IsNullOrWhiteSpace(e.Value));

        var notApplicable = hasXWindFalse && !hasWindHailDeductible;

        logger.Info($"ResultDataHelper: Wind/Hail N/A determination for {externalId} {carrier}: " +
                    $"XWindFlag='{xWindFlag ?? "<absent>"}', WindHailDeductible present={hasWindHailDeductible} " +
                    $"=> NotApplicable={notApplicable}.");

        return notApplicable;
    }

    public async Task<decimal?> GetPercentFromFreeTextAsync(string externalId, CarrierEnums carrier, CoverageEnums coverage, CancellationToken ct = default)
    {
        var data = await GetCoverageAsync(externalId, carrier, coverage, ct);
        if (data is null) return null;
        return CoveragePercent.TryGet(data.FreeText, out var pct) ? pct : null;
    }

    public async Task<CoverageData?> GetCoverageAsync(string externalId, CarrierEnums carrier, CoverageEnums coverage, CancellationToken ct = default)
    {
        var set = await GetCoverageSetAsync(externalId, carrier, ct);
        if (set is null) return null;
        return set.TryGet(coverage.ToString(), out var data) ? data : null;
    }


    public async Task<CoverageSet?> GetCoverageSetAsync(string externalId, CarrierEnums carrier, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(externalId))
        {
            logger.Warning("ResultDataHelper.GetCoverageSetAsync: externalId empty.");
            return null;
        }

        var key = (externalId, carrier);
        if (_cache.TryGetValue(key, out var cached)) return cached;

        var attachment = await GetChosenAttachmentAsync(externalId, carrier, ct);
        if (attachment is null) return null;

        var set = CoverageXmlParser.Parse(attachment.XmlContent, logger);
        if (set is null) return null;

        // Cache only successful parses.
        _cache.TryAdd(key, set);
        return set;
    }

    private async Task<ResultDataQueries.CoverageAttachmentRow?> GetChosenAttachmentAsync(string externalId, CarrierEnums carrier, CancellationToken ct)
    {
        var attachments = await resultDataQueries.GetCoverageAttachmentsAsync(externalId, carrier, ct);
        if (attachments.Count == 0)
        {
            logger.Info($"ResultDataHelper: no coverage attachments for {externalId} {carrier}");
            return null;
        }
        return CoverageAttachmentSelector.Choose(attachments);
    }

    // ----------------- Helper static components (no interfaces to keep it simple) -----------------

    private static class CoveragePercent
    {
        private static readonly Regex PercentRegex = new(@"(?<num>\d+(\.\d+)?)%", RegexOptions.Compiled);
        public static bool TryGet(string? freeText, out decimal value)
        {
            value = 0m;
            if (string.IsNullOrWhiteSpace(freeText)) return false;
            var m = PercentRegex.Match(freeText);
            if (!m.Success) return false;
            return decimal.TryParse(m.Groups["num"].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
        }
    }

    private static class CoverageFormatter
    {
        public static Dictionary<string, string?> ToDictionary(CoverageData c) => new(StringComparer.OrdinalIgnoreCase)
        {
            [nameof(CoverageData.Name)] = c.Name,
            [nameof(CoverageData.Limit)] = c.Limit?.ToString(CultureInfo.InvariantCulture),
            [nameof(CoverageData.CustomType)] = c.CustomType.ToString(),
            [nameof(CoverageData.UseInSubmission)] = c.UseInSubmission.ToString(),
            [nameof(CoverageData.ShowInResultPage)] = c.ShowInResultPage.ToString(),
            [nameof(CoverageData.Order)] = c.Order?.ToString(CultureInfo.InvariantCulture),
            [nameof(CoverageData.CoverageLevel)] = c.CoverageLevel,
            [nameof(CoverageData.FreeText)] = c.FreeText,
            [nameof(CoverageData.Comparable)] = c.Comparable.ToString(),
            [nameof(CoverageData.Compared)] = c.Compared.ToString(),
            [nameof(CoverageData.ProposalCoverageLevel)] = c.ProposalCoverageLevel,
            [nameof(CoverageData.SortOrder)] = c.SortOrder,
            [nameof(CoverageData.HasImage)] = c.HasImage.ToString(),
            [nameof(CoverageData.BestOffer)] = c.BestOffer.ToString()
        };
    }

    private static class CoverageAttachmentSelector
    {
        public static ResultDataQueries.CoverageAttachmentRow? Choose(IReadOnlyList<ResultDataQueries.CoverageAttachmentRow> rows)
            => rows
                .OrderByDescending(a => a.Selected)
                .ThenBy(a => a.Premium ?? decimal.MaxValue)
                .FirstOrDefault();
    }

    private static class CoverageXmlParser
    {
        public static CoverageSet? Parse(string xml, IAutomationLogger logger)
        {
            if (string.IsNullOrWhiteSpace(xml)) return null;
            try
            {
                var doc = XDocument.Parse(xml, LoadOptions.PreserveWhitespace);
                var coverages = new List<CoverageData>();
                foreach (var node in doc.Descendants("Coverage"))
                {
                    try
                    {
                        var c = ParseCoverageNode(node);
                        if (c is not null) coverages.Add(c);
                    }
                    catch (Exception ex)
                    {
                        // Skip bad node, log at debug level to avoid noise.
                        logger.Debug($"ResultDataHelper: skipping malformed Coverage node: {ex.Message}");
                    }
                }
                return new CoverageSet(coverages);
            }
            catch (Exception ex)
            {
                logger.LogException(ex, "ResultDataHelper: parsing coverage set failed");
                return null;
            }
        }

        private static CoverageData? ParseCoverageNode(XElement node)
        {
            string name = node.Element("Name")?.Value?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(name)) return null;

            decimal? limit = TryDecimal(node.Element("Limit")?.Value);
            var customType = ParseCustomType(node.Element("CoverageCustomType")?.Value);
            bool useInSubmission = TryBool(node.Element("UseInSubmission")?.Value);
            bool showInResult = TryBool(node.Element("ShowInResultPage")?.Value);
            int? order = TryInt(node.Element("Order")?.Value);
            string coverageLevel = node.Element("CoverageLevel")?.Value?.Trim() ?? string.Empty;
            string? freeText = node.Element("FreeText")?.Value?.Trim();
            bool comparable = TryBool(node.Element("Comparable")?.Value);
            bool compared = TryBool(node.Element("Compared")?.Value);
            string proposalLevel = node.Element("ProposalCoverageLevel")?.Value?.Trim() ?? string.Empty;
            string sortOrder = node.Element("SortOrder")?.Value?.Trim() ?? string.Empty;
            bool hasImage = TryBool(node.Element("HasImage")?.Value);
            bool bestOffer = TryBool(node.Element("BestOffer")?.Value);

            return new CoverageData(
                name,
                limit,
                customType,
                useInSubmission,
                showInResult,
                order,
                coverageLevel,
                freeText,
                comparable,
                compared,
                proposalLevel,
                sortOrder,
                hasImage,
                bestOffer);
        }

        private static decimal? TryDecimal(string? raw) => decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : null;
        private static int? TryInt(string? raw) => int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i) ? i : null;
        private static bool TryBool(string? raw) => string.Equals(raw, "true", StringComparison.OrdinalIgnoreCase);
        private static CoverageCustomType ParseCustomType(string? raw) => raw?.Trim() switch
        {
            null or "" => CoverageCustomType.Unknown,
            "NA" => CoverageCustomType.NA,
            "FreeText" => CoverageCustomType.FreeText,
            _ => CoverageCustomType.Unknown
        };
    }
}
