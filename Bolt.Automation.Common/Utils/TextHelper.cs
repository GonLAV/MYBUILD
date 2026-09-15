using System.Text.RegularExpressions;

namespace Bolt.Automation.Common.Utils;

/// <summary>
/// Helper methods for text manipulation and normalization.
/// </summary>
public static class TextHelper
{
    /// <summary>
    /// Normalizes text by removing newlines, carriage returns, and trimming whitespace.
    /// Useful for comparing UI text that may have formatting differences.
    /// </summary>
    /// <param name="text">The text to normalize</param>
    /// <returns>Normalized text with no newlines/carriage returns and trimmed whitespace</returns>
    public static string NormalizeText(string? text)
    {
        return text?.Replace("\n", "").Replace("\r", "").Trim() ?? string.Empty;
    }

    /// <summary>
    /// Collapses every run of whitespace (including newlines) to a single space and trims.
    /// </summary>
    /// <remarks>Stronger than <see cref="NormalizeText"/>, which only strips newlines — use this when a
    /// multi-line rendered block must compare cleanly against a single-line expected string.</remarks>
    public static string CollapseWhitespace(string? text) =>
        string.IsNullOrWhiteSpace(text) ? string.Empty : Regex.Replace(text, @"\s+", " ").Trim();

    // The mandatory marker renders either as a bare "*" or as "* Required" (the word is part of the
    // marker element, e.g. "What year was this home built?* Required"), so both shapes must be stripped.
    private static readonly Regex RequiredMarkerRegex = new(@"\s*\*\s*(?:Required)?\s*$", RegexOptions.IgnoreCase);

    /// <summary>Strips the trailing mandatory marker ("*" or "* Required") the UI appends to mandatory
    /// question labels.</summary>
    /// <remarks>The marker sits before any <c>.question-description</c> hint span, so exclude that
    /// span from the rendered text first or the marker will not be trailing.</remarks>
    public static string StripRequiredMarker(string? label) =>
        string.IsNullOrWhiteSpace(label) ? string.Empty : RequiredMarkerRegex.Replace(label.TrimEnd(), string.Empty).TrimEnd();

    /// <summary>
    /// Normalizes typographic apostrophes and quotation marks to their plain ASCII equivalents.
    /// Useful when comparing UI text (which may use smart/curly quotes) against hardcoded expected strings.
    /// </summary>
    /// <param name="text">The text to normalize</param>
    /// <returns>Text with all apostrophe/quote variants replaced by ' and "</returns>
    public static string NormalizeQuotes(string? text)
    {
        if (text is null) return string.Empty;
        return text
            .Replace('\u2018', '\'') // LEFT SINGLE QUOTATION MARK  '
            .Replace('\u2019', '\'') // RIGHT SINGLE QUOTATION MARK '
            .Replace('\u201C', '"')  // LEFT DOUBLE QUOTATION MARK  "
            .Replace('\u201D', '"'); // RIGHT DOUBLE QUOTATION MARK "
    }

    // AWS SES click-tracking wraps every link in an email as
    // https://<id>.r.<region>.awstrack.me/L0/<percent-encoded-real-url>/<segment>/.../<signature>.
    // Only the first path segment after "/L0/" is the (fully percent-encoded) real URL; the remaining
    // slash-separated segments are opaque tracking ids. This matches each wrapper and captures that
    // first segment so it can be decoded back to the original destination URL.
    private static readonly Regex AwsTrackingLinkRegex = new(
        @"https?://[^\s""'<>]*?awstrack\.me/L0/(?<encoded>[^/\s""'<>]+)[^\s""'<>]*",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Replaces AWS SES click-tracking link wrappers in an HTML (or plain-text) body with the real
    /// destination URLs they encode, leaving all other content untouched. Email platforms rewrite
    /// outbound links as <c>awstrack.me/L0/&lt;percent-encoded-url&gt;/&lt;tracking-ids&gt;</c>, so a raw
    /// <c>Does.Contain(originalUrl)</c> assertion no longer matches; run the body through this first to
    /// recover the original URLs. Non-wrapped links pass through unchanged, and any segment that fails to
    /// decode to an http(s) URL is left exactly as-is (never throws).
    /// </summary>
    /// <param name="body">The email body (HTML or text) to unwrap.</param>
    /// <returns>The body with every AWS-tracking link replaced by its decoded destination URL.</returns>
    public static string UnwrapTrackingLinks(string? body)
    {
        if (string.IsNullOrEmpty(body)) return string.Empty;

        return AwsTrackingLinkRegex.Replace(body, match =>
        {
            try
            {
                var decoded = Uri.UnescapeDataString(match.Groups["encoded"].Value);
                return decoded.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? decoded : match.Value;
            }
            catch
            {
                return match.Value;
            }
        });
    }

    /// <summary>
    /// Normalizes an address string for comparison by collapsing extra whitespace
    /// and removing the comma before a ZIP code.
    /// </summary>
    /// <param name="address">The address to normalize</param>
    /// <returns>Normalized address string</returns>
    public static string NormalizeAddress(string? address)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return string.Empty;
        }

        var normalized = Regex.Replace(address.Trim(), @",\s+(?=\d{5}\b)", " ");
        normalized = Regex.Replace(normalized, @"\s+", " ");
        return normalized;
    }
}
