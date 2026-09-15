using System.Text.RegularExpressions;

namespace Bolt.Automation.FrontEnds.Projects.D2C.Base
{
    /// <summary>
    /// The comparison footer and the comparison table identify a carrier only by its logo image —
    /// there is no <c>carrier-name-automation</c> attribute on either — so the display name has to be
    /// read back out of the logo request.
    /// </summary>
    internal static class CarrierLogo
    {
        private static readonly Regex DisplayNamePattern = new("carrierDisplayName=([^&]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static string? DisplayNameFrom(string? logoSrc)
        {
            if (string.IsNullOrEmpty(logoSrc))
                return null;

            var match = DisplayNamePattern.Match(logoSrc);
            return match.Success ? Uri.UnescapeDataString(match.Groups[1].Value).Trim() : null;
        }
    }
}
