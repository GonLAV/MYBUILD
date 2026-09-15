using Newtonsoft.Json;

namespace Bolt.Automation.Common.Utils
{
    /// <summary>
    /// Configuration settings for JSON logging operations
    /// </summary>
    public static class JsonLoggingSettings
    {
        /// <summary>
        /// JSON serialization settings for pretty-printed output
        /// </summary>
        public static readonly JsonSerializerSettings PrettySettings = new()
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Include,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc
        };
        
        /// <summary>
        /// JSON serialization settings for compact output
        /// </summary>
        public static readonly JsonSerializerSettings CompactSettings = new()
        {
            Formatting = Formatting.None,
            NullValueHandling = NullValueHandling.Ignore,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc
        };
        
        /// <summary>
        /// Maximum length for JSON preview in collapsed state
        /// </summary>
        public static int MaxPreviewLength { get; set; } = 100;
        
        /// <summary>
        /// Maximum length for JSON content before truncation
        /// </summary>
        public static int MaxJsonLength { get; set; } = 50000;
        
        /// <summary>
        /// Whether to enable HTML styling for JSON logs
        /// </summary>
        public static bool EnableStyling { get; set; } = true;
    }
}