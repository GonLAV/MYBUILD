using Bolt.Automation.Common.Enums;

namespace Bolt.Automation.TestDataProvider.TestData.CoverageModificationTestData
{
    /// <summary>
    /// Expected "key points" informational text and carrier display names shown on the PGR Selected
    /// Carrier page, keyed by carrier (the copy differs per carrier — e.g. the E&amp;S carriers add
    /// an "Important Notes on E&amp;S Homeowners" section that admitted carriers do not). Reconciled
    /// against the live QA DOM (ADO TC 246475). Apostrophes are the rendered curly form.
    /// </summary>
    public static class KeyPointsData
    {
        /// <summary>Carrier name shown on the Selected Carrier page, keyed by carrier.</summary>
        public static readonly Dictionary<CarrierEnums, string> CarrierDisplayNames = new()
        {
            [CarrierEnums.BambooSurplus] = "Progressive by Bamboo",
        };

        /// <summary>
        /// Key-points lines (section headings + every note bullet, in document order), keyed by
        /// carrier. The declination line is state-specific ("Texas: 1") — the current entry is TX.
        /// </summary>
        public static readonly Dictionary<CarrierEnums, List<string>> SelectedCarrierKeyPoints = new()
        {
            [CarrierEnums.BambooSurplus] = new List<string>
            {
                "Important Notes on E&S Homeowners",
                "Progressive holds E&S carriers to the same high principles as our standard Homeowner carriers.",
                "An E&S homeowners policy covers homes with unique needs that don’t always fit into a one size fits all policy. We work closely with trusted carrier partners to offer the best available options tailored to each homeowner’s unique situation.",
                "Do not present E&S rates to the customer until all required carrier declinations have been met.",
                "Number of declinations for diligent search in Texas: 1",
                "A few key points...",
                "Recap your discovery of customers needs and wants.",
                "Share the solution the selected carrier offers.",
                "Educate additional information is needed to finalize rate & eligibility.",
                "Do not present preliminary rate to customer.",
            },
        };

        /// <summary>
        /// Expected "Not quoted" (<c>rate-unquoted-carriers</c>) decline reason shown when a carrier
        /// declines the risk, keyed by carrier. For the E&amp;S carrier (Bamboo Surplus) an
        /// excessive Coverage A pushes the risk outside its allowable limits (UUDS scenario) and a
        /// "Get DF rates" fallback link accompanies the block. Reconciled against the live QA decline
        /// DOM (ADO TC 246473, TX / HO). Apostrophe is the straight form as rendered.
        /// </summary>
        public static readonly Dictionary<CarrierEnums, string> SelectedCarrierNotQuotedReason = new()
        {
            [CarrierEnums.BambooSurplus] =
                "E&S HO: According to this carrier's underwriting guidelines, Coverage A for this risk is outside the allowable limits.",
        };
    }
}
