using Bolt.Automation.Common.Enums;

namespace Bolt.Automation.TestDataProvider.Extensions
{
    /// <summary>
    /// Extension helpers for mapping internal LOB enums to Platform API expected LOBCd values.
    /// Centralized so tests and data providers use the same translation.
    /// </summary>
    public static class PlatformApiLobExtensions
    {
        /// <summary>
        /// Maps internal <see cref="LOBEnums"/> to the string the Platform API expects in QuoteStartRequestModel.LOBCd.
        /// Falls back to enum.ToString() when no explicit mapping is defined.
        /// </summary>
        public static string ToPlatformApiCode(this LOBEnums lob) => lob switch
        {
            LOBEnums.HO3 => "Home",   // Platform expects "Home" for HO3
            LOBEnums.HO6 => "Condo",  // Platform expects "Condo" for HO6
            LOBEnums.RENTERS => "Renters",  // Platform expects "Renters" for RENTERS
            // Add additional mappings as business rules require:
            // LOBEnums.DF => "DwellingFire",
            // LOBEnums.MFH => "MobileHome",
            _ => lob.ToString()
        };
    }
}
