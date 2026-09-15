namespace Bolt.Automation.Common.Utils
{
    /// <summary>
    /// Compact JSON formatter that creates readable but single-line JSON for logging
    /// </summary>
    public static class CompactJsonFormatter
    {
        /// <summary>
        /// Formats JSON compactly with some spacing for readability
        /// </summary>
        /// <param name="data">Object to serialize</param>
        /// <returns>Formatted JSON string</returns>
        public static string Format(object data)
        {
            if (data == null) return "null";
            
            try
            {
                // Use compact formatting to avoid newlines
                var json = JsonHelper.Serialize(data, JsonLoggingSettings.CompactSettings);
                
                // Add some spacing after commas and colons for better readability
                // but keep it as a single line
                json = json.Replace(",", ", ").Replace(":", ": ");
                
                return json;
            }
            catch (Exception)
            {
                return $"[Serialization failed: {data.GetType().Name}]";
            }
        }
        
        /// <summary>
        /// Creates a formatted preview of the JSON data
        /// </summary>
        /// <param name="data">Object to create preview for</param>
        /// <param name="maxLength">Maximum length of preview</param>
        /// <returns>Preview string</returns>
        public static string CreatePreview(object data, int maxLength = 100)
        {
            if (data == null) return "null";
            
            var formatted = Format(data);
            
            if (formatted.Length <= maxLength)
                return formatted;
                
            return formatted.Substring(0, maxLength) + "...";
        }
    }
}