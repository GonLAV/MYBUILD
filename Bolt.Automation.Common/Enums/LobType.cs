namespace Bolt.Automation.Common.Enums
{
    public enum LobType
    {
        Auto,
        Home,
        Renters,
        Condo,
        DF,
        Umbrella,
        WorkersCompensation,
        GeneralLiability,
        CommercialAuto,
        BusinessOwners,
        Flood,
        Earthquake,
        Motorcycle,
        DeviceProtection,
        Pets,
        ErrorsOmissions,
        EventLiability,
        CyberLiability,
    }

    public static class LobTypeExtensions
    {
        // Only the LOBs whose emitted token differs from the enum name.
        private static readonly Dictionary<LobType, string> TokenOverrides = new()
        {
            [LobType.BusinessOwners]      = "Business Owners Policy",
            [LobType.WorkersCompensation] = "Workers Compensation",
            [LobType.CommercialAuto]      = "Commercial Auto",
            [LobType.GeneralLiability]    = "General Liability",
            [LobType.ErrorsOmissions]     = "Errors & Omissions",
            [LobType.EventLiability]      = "Event Liability",
            [LobType.CyberLiability]      = "Cyber Liability",
        };

        public static string ToToken(this LobType lob) =>
            TokenOverrides.GetValueOrDefault(lob, lob.ToString());
    }
}
