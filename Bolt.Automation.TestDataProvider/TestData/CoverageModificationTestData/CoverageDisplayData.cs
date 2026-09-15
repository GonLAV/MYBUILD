using Bolt.Automation.Common.Enums;

namespace Bolt.Automation.TestDataProvider.TestData.CoverageModificationTestData
{
    /// <summary>
    /// Expected coverage titles shown under "View rates" on the PGR Selected Carrier page, keyed by
    /// line of business (titles vary by LOB). These are the <c>coverage-title</c> spans rendered in
    /// the rate table, reconciled against the live QA DOM. The manual TC concatenates each title
    /// with its value cell (e.g. "Settlement option – Dwelling Replacement Cost"); the DOM renders
    /// the title alone ("Settlement option - Dwelling") with the settlement value in a separate
    /// cell — so only the bare, trimmed titles are listed here.
    /// </summary>
    public static class CoverageDisplayData
    {
        public static readonly Dictionary<LOBEnums, List<string>> SelectedCarrierCoverages = new()
        {
            // Homeowners (HO3) — E&S (Bamboo Surplus), TX (ADO TC 246475).
            [LOBEnums.HO3] = new List<string>
            {
                "Repair/ rebuild dwelling",
                "Settlement option - Dwelling",
                "Settlement option - Roof",
                "Repair/ rebuild other structures",
                "Personal property",
                "Settlement option - Personal property",
                "Temporary living expenses",
                "Personal liability",
                "Medical payments to others",
                "Standard deductible",
                "Wind/Hail deductible",
                "Extended dwelling",
                "Water backup coverage",
            },
        };
    }
}
