using Bolt.Automation.Common.Logging.Core;
using Microsoft.Playwright;

namespace Bolt.Automation.FrontEnds.Projects.HQXConsumer.Services.CarrierSelection;

/// <summary>
/// Resolves carrier information from image elements
/// </summary>
public class CarrierImageResolver
{
    private readonly IAutomationLogger? _logger;

    public CarrierImageResolver(IAutomationLogger? logger = null)
    {
        _logger = logger;
    }

    public async Task<string?> ResolveFromImageAsync(ILocator img)
    {
        try
        {
            var src = await img.GetAttributeAsync("src");
            var parsed = ParseCarrierFromSrc(src);
            if (!string.IsNullOrWhiteSpace(parsed)) return NormalizeCarrier(parsed);

            var alt = await img.GetAttributeAsync("alt") ?? string.Empty;
            return string.IsNullOrWhiteSpace(alt) ? null : NormalizeCarrier(NormalizeCarrierAlt(alt));
        }
        catch (Exception ex)
        {
            _logger?.Debug($"Failed to resolve carrier from image: {ex.Message}");
            return null;
        }
    }

    private static string? ParseCarrierFromSrc(string? src)
    {
        if (string.IsNullOrEmpty(src)) return null;
        const string key = "carrier=";
        var start = src.IndexOf(key, StringComparison.OrdinalIgnoreCase);
        if (start < 0) return null;
        start += key.Length;
        var end = src.IndexOf('&', start);
        var value = end > start ? src.Substring(start, end - start) : src[start..];
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string NormalizeCarrierAlt(string alt) =>
        alt.Replace("Logo", "", StringComparison.OrdinalIgnoreCase)
           .Replace("Insurance", "", StringComparison.OrdinalIgnoreCase)
           .Trim();

    // Maps the different DOM spellings the same carrier gets across the Rates page to one canonical
    // token. The comparison-table header (SVG title "Progressive by StillWaterPropertyCasualty Logo")
    // and the main rate card (component tag "logo-stillwater") resolve the SAME carrier to different
    // strings, so a raw string compare in the switch-confirmation poll never matches for cobranded
    // carriers like Stillwater. Keyed/valued on the space-and-dash-stripped, lowercased form.
    private static readonly Dictionary<string, string> CarrierAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["stillwaterpropertycasualty"] = "stillwater",
        ["progressivebystillwater"] = "stillwater",
    };

    /// <summary>
    /// Canonicalizes a carrier name so the value read from the comparison-table header and the value
    /// read from the main rate card compare equal for the same carrier. Strips a leading
    /// "Progressive by " cobrand prefix and all spaces/dashes, lowercases, then applies the alias map.
    /// </summary>
    public static string NormalizeCarrier(string? raw)
    {
        var value = (raw ?? "").Trim();
        if (value.Length == 0) return value;

        const string cobrandPrefix = "progressive by ";
        if (value.StartsWith(cobrandPrefix, StringComparison.OrdinalIgnoreCase))
        {
            value = value[cobrandPrefix.Length..].Trim();
        }

        var collapsed = value.Replace(" ", "").Replace("-", "").ToLowerInvariant();
        return CarrierAliases.TryGetValue(collapsed, out var canonical) ? canonical : collapsed;
    }
}
