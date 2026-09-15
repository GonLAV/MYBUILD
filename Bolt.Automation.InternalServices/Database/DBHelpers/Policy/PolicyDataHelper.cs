using System.Collections.Concurrent;
using System.Xml.Linq;
using System.Xml.XPath;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.Common.PollyRetry;
using Bolt.Automation.InternalServices.Database.Extensions;
using Bolt.Automation.InternalServices.Database.Queries.Main.Policy;

namespace Bolt.Automation.InternalServices.Database.DBHelpers.Policy
{
    public sealed class PolicyDataHelper(PolicyQueries policyQueries, IAutomationLogger logger, IPollyRetryService pollyRetryService)
    {
        private static readonly ConcurrentDictionary<string, (XDocument Document, DateTime CachedAt)> _xmlCache = new();
        private static readonly TimeSpan CacheExpiry = TimeSpan.FromMinutes(5); 

        public async Task<string?> GetNodeValueAsync(string friendlyId, string nodePath, CancellationToken ct = default)
        {
            var xmlDoc = await GetXmlAsync(friendlyId, ct);
            return xmlDoc == null ? null : GetSingleValue(xmlDoc, nodePath);
        }

        public Task<string?> GetNodeValueWithRetryAsync(string friendlyId, string nodePath, int timeOutSeconds = 30, CancellationToken ct = default)
        {
            return pollyRetryService.ExecuteWithRetryAsync(async () =>
            {
                ClearXmlCache(friendlyId);
                return await GetNodeValueAsync(friendlyId, nodePath, ct);
            }, timeOutSeconds);
        }

        public async Task<List<string>> GetNodeValuesListAsync(string friendlyId, string nodePath, CancellationToken ct = default)
        {
            var xmlDoc = await GetXmlAsync(friendlyId, ct);
            return xmlDoc == null ? [] : GetMultipleValues(xmlDoc, nodePath);
        }

        public async Task<string?> ValidateAddressAsync(string friendlyId, string expectedAddress, CancellationToken ct = default)
        {
            var xmlDoc = await GetXmlAsync(friendlyId, ct);
            if (xmlDoc == null) return null;

            var propertyAddress = xmlDoc.Descendants("PropertyAddress").FirstOrDefault();
            if (propertyAddress == null) return null;

            // Check if it has child elements (structured address)
            if (propertyAddress.HasElements)
            {
                var addressLine1 = propertyAddress.Element("AddressLine1")?.Value?.Trim();
                var city = propertyAddress.Element("City")?.Value?.Trim();
                var state = propertyAddress.Element("State")?.Value?.Trim();
                var zipCode = propertyAddress.Element("ZipCode")?.Value?.Trim();

                var parts = new[] { addressLine1, city, state, zipCode }
                    .Where(p => !string.IsNullOrWhiteSpace(p));

                return string.Join(", ", parts);
            }

            var rawValue = propertyAddress.Value?.Trim();
            return string.IsNullOrWhiteSpace(rawValue) ? null : rawValue;
        }

        public Task<string?> WaitForNodeValueAsync(
            string friendlyId,
            string nodePath,
            Func<string?, bool> predicate,
            TimeSpan? timeout = null,
            TimeSpan? interval = null,
            CancellationToken ct = default)
        {
            return QueryExtensions.WaitForValueAsync(
                query: async () =>
                {
                    ClearXmlCache(friendlyId);
                    return await GetNodeValueAsync(friendlyId, nodePath, ct);
                },
                predicate: predicate,
                timeout: timeout,
                interval: interval,
                ct: ct);
        }

        public void ClearXmlCache(string? friendlyId = null)
        {
            if (friendlyId == null)
            {
                _xmlCache.Clear();
                logger.Info("Cleared all XML cache");
            }
            else if (_xmlCache.TryRemove(friendlyId, out _))
            {
                logger.Debug($"Cleared XML cache for friendlyId: {friendlyId}");
            }
        }

        private async Task<XDocument?> GetXmlAsync(string friendlyId, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(friendlyId)) return null;
            
            // Check cache with expiration
            if (_xmlCache.TryGetValue(friendlyId, out var cached))
            {
                if (DateTime.UtcNow - cached.CachedAt < CacheExpiry)
                {
                    logger.Debug($"Returning cached XML for friendlyId: {friendlyId}");
                    return cached.Document;
                }
                else
                {
                    // Cache expired, remove it
                    _xmlCache.TryRemove(friendlyId, out _);
                    logger.Debug($"XML cache expired for friendlyId: {friendlyId}");
                }
            }

            // Get fresh XML from database
            var rawXml = await policyQueries.GetPolicyDataRawAsync(friendlyId);
            if (string.IsNullOrWhiteSpace(rawXml)) return null;

            try
            {
                var xmlDoc = XDocument.Parse(rawXml, LoadOptions.PreserveWhitespace);
                _xmlCache.TryAdd(friendlyId, (xmlDoc, DateTime.UtcNow));
                logger.Debug($"Cached fresh XML for friendlyId: {friendlyId}");
                return xmlDoc;
            }
            catch (Exception ex)
            {
                logger.LogException(ex, "XML parsing failed");
                return null;
            }
        }

        private static string? GetSingleValue(XDocument xmlDoc, string nodePath)
        {
            try
            {
                var elements = nodePath.StartsWith("//") 
                    ? xmlDoc.XPathSelectElements(nodePath)
                    : xmlDoc.Descendants(nodePath);
                return elements.FirstOrDefault()?.Value?.Trim();
            }
            catch
            {
                return null;
            }
        }

        private static List<string> GetMultipleValues(XDocument xmlDoc, string nodePath)
        {
            var result = new List<string>();
            try
            {
                if (nodePath.StartsWith("//"))
                {
                    var elements = xmlDoc.XPathSelectElements(nodePath);
                    result.AddRange(elements.Select(e => e.Value?.Trim()).Where(v => !string.IsNullOrWhiteSpace(v))!);
                }
                else
                {
                    var parent = xmlDoc.Descendants(nodePath).FirstOrDefault();
                    if (parent?.HasElements == true)
                    {
                        result.AddRange(parent.Elements().Select(e => e.Value?.Trim()).Where(v => !string.IsNullOrWhiteSpace(v))!);
                    }
                    else if (!string.IsNullOrWhiteSpace(parent?.Value))
                    {
                        result.Add(parent.Value.Trim());
                    }
                }
            }
            catch { }
            return result;
        }
    }
}
