

using System.Net;

namespace Bolt.Automation.Common.Utils
{
    /// <summary>
    /// HTML encoding utility for safe HTML content generation
    /// </summary>
    public static class HtmlEncoder
    {
        /// <summary>
        /// Encodes a string for safe HTML output
        /// </summary>
        /// <param name="input">The input string to encode</param>
        /// <returns>HTML-encoded string</returns>
        public static string Encode(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;
                
            return WebUtility.HtmlEncode(input);
        }
        
        /// <summary>
        /// Encodes a string for safe HTML attribute output
        /// </summary>
        /// <param name="input">The input string to encode</param>
        /// <returns>HTML attribute-encoded string</returns>
        public static string EncodeAttribute(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;
                
            return WebUtility.HtmlEncode(input).Replace("\"", "&quot;");
        }
    }
}