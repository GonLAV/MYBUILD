using System.Text.Json;
using Bolt.Automation.Common.Logging.Core;
using Microsoft.Playwright;

namespace Bolt.Automation.FrontEnds.PlaywrightBase.Helpers
{
    /// <summary>
    /// Helper for reading values from the browser's sessionStorage and localStorage.
    /// </summary>
    public static class BrowserStorageHelper
    {
        /// <summary>
        /// Gets the raw string value for a key from sessionStorage.
        /// Returns null if the key is not present.
        /// </summary>
        public static async Task<string?> GetSessionStorageItemAsync(IPage page, string key, IAutomationLogger? logger = null)
        {
            var value = await page.EvaluateAsync<string?>($"() => sessionStorage.getItem('{key}')");
            return value;
        }

        /// <summary>
        /// Gets the raw string value for a key from localStorage.
        /// Returns null if the key is not present.
        /// </summary>
        public static async Task<string?> GetLocalStorageItemAsync(IPage page, string key, IAutomationLogger? logger = null)
        {
            var value = await page.EvaluateAsync<string?>($"() => localStorage.getItem('{key}')");
            return value;
        }

        /// <summary>
        /// Parses a JSON string from sessionStorage and returns a nested string property
        /// by navigating a dot-separated path (e.g. "interviewMetadata.friendlyId").
        /// Returns null if any segment along the path is missing.
        /// </summary>
        public static async Task<string?> GetSessionStorageJsonValueAsync(
            IPage page,
            string key,
            string jsonPath,
            IAutomationLogger? logger = null)
        {
            var json = await GetSessionStorageItemAsync(page, key, logger);
            if (string.IsNullOrEmpty(json))
                return null;

            return ExtractJsonValue(json, jsonPath, logger);
        }

        /// <summary>
        /// Parses a JSON string from localStorage and returns a nested string property
        /// by navigating a dot-separated path (e.g. "interviewMetadata.friendlyId").
        /// Returns null if any segment along the path is missing.
        /// </summary>
        public static async Task<string?> GetLocalStorageJsonValueAsync(
            IPage page,
            string key,
            string jsonPath,
            IAutomationLogger? logger = null)
        {
            var json = await GetLocalStorageItemAsync(page, key, logger);
            if (string.IsNullOrEmpty(json))
                return null;

            return ExtractJsonValue(json, jsonPath, logger);
        }

        /// <summary>
        /// Parses a JSON string from sessionStorage and returns a <see cref="JsonElement"/>
        /// at the given dot-separated path. Returns null if not found or on any error.
        /// </summary>
        public static async Task<JsonElement?> GetSessionStorageJsonElementAsync(
            IPage page,
            string key,
            string? jsonPath = null,
            IAutomationLogger? logger = null)
        {
            var json = await GetSessionStorageItemAsync(page, key, logger);
            if (string.IsNullOrEmpty(json))
                return null;

            return NavigateJsonPath(json, jsonPath, logger);
        }

        // ── private helpers ──────────────────────────────────────────────────────

        private static string? ExtractJsonValue(string json, string jsonPath, IAutomationLogger? logger)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var element = NavigateToElement(doc.RootElement, jsonPath.Split('.'));
                if (element == null)
                {
                    logger?.Debug($"JSON path '{jsonPath}' not found in storage value");
                    return null;
                }
                return element.Value.GetString();
            }
            catch (Exception ex)
            {
                logger?.Debug($"Failed to parse JSON path '{jsonPath}': {ex.Message}");
                return null;
            }
        }

        private static JsonElement? NavigateJsonPath(string json, string? jsonPath, IAutomationLogger? logger)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (string.IsNullOrEmpty(jsonPath))
                    return doc.RootElement.Clone();

                var segments = jsonPath.Split('.');
                var element = NavigateToElement(doc.RootElement, segments);
                return element?.Clone();
            }
            catch (Exception ex)
            {
                logger?.Debug($"Failed to navigate JSON path '{jsonPath}': {ex.Message}");
                return null;
            }
        }

        private static JsonElement? NavigateToElement(JsonElement current, string[] segments)
        {
            foreach (var segment in segments)
            {
                if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(segment, out var next))
                    return null;
                current = next;
            }
            return current;
        }
    }
}
