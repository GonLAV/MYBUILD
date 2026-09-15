namespace Bolt.Automation.TestDataProvider.TestData.PlatformAPITestData.QuoteStartTestData
{
    /// <summary>
    /// Strongly-typed model for PolicyData prefill fields
    /// </summary>
    public class PolicyDataPrefill
    {
        public int? YearsAtAddress { get; set; }
        public string? ArchitectureStyle { get; set; }
        public int? NumberOfFirePlaces { get; set; }
        public bool? RoofResponsible { get; set; }
        public bool? PrimaryHome { get; set; }
        public bool? EligibilityFinancialHardship { get; set; }
        public bool? BusinessOnResidencePremises { get; set; }
        public int? PLYearBuilt { get; set; }
        public string? PLHeatingType { get; set; }
        public string? PL_RoofUpdateYearRange { get; set; }
        public string? PerimeterSecurityDD { get; set; }
        public string? PL_PrimaryCounterMaterial { get; set; }
        public bool? PL_HeatedByOil { get; set; }
        public bool? AnyAdditionalInsured { get; set; }
        public bool? PriorInsuranceProperty { get; set; }
        public string? CurrentPersonalHomeownerCarrier { get; set; }
        public int? YearsWithPriorCarrierHome { get; set; }
        public string? PurchaseDate { get; set; }
        public string? DateOccupied { get; set; }
        public int? NumberOfClaimsHistory { get; set; }
        public int? Progressive_Preferences1 { get; set; }
        public int? Progressive_Preferences2 { get; set; }
        public bool? HomeAutoInsurance { get; set; }
        public string? EffectiveDate { get; set; }
        public string? MitWindowOpening { get; set; }
        public bool? PL_CentralAC { get; set; }
        public bool PLHighRiseCondo { get; set; }
        public int? PLPersonalProperty { get; set; }
        public bool? SmokeDetector { get; set; }
        public bool? PL_AdditionalStructures_Pool { get; set; }

        // --- Manufactured-home (MFH) prefill fields ---------------------------------------------
        // Set only for an MFH quote so the manufactured-home interview questions on the Details/
        // Discounts pages arrive pre-answered and the short flow can auto-advance to Rates.
        // Field names match the platform prefill keys (verified against the live MFH Rates DOM);
        // values mirror the canonical MFH HomeDetails (PGRMHDetails).
        public string? PLTypeOfDwelling { get; set; }
        public string? MHArchitectureStyle { get; set; }
        public int? HomeLength { get; set; }
        public int? HomeWidth { get; set; }
        public bool? ModularHome { get; set; }
        public bool? HomeTiedDown { get; set; }
        public bool? IsLocatedInPark { get; set; }
        public bool? UtilityHookup365 { get; set; }
        public int? PLSquareFootage { get; set; }
        public string? PLConstructionType { get; set; }
        public string? TypeOfFoundation { get; set; }
        public string? PlumbingType { get; set; }
        public string? RoofType { get; set; }
        public string? MitRoofShape { get; set; }
        public string? PLNumberOfStories { get; set; }
        public int? FullBathNum { get; set; }
        public int? HalfBathNum { get; set; }
        public bool? BuiltOnSlope { get; set; }
        public int? NumberofAcres { get; set; }
        public bool? PL_AdditionalStructures_Garage { get; set; }
    }

    /// <summary>
    /// Strongly-typed model for CustomFields prefill fields
    /// </summary>
    public class CustomFieldsPrefill
    {
        public string? ABTest_new_d2c { get; set; }
        public string? ABTest_name_and_dob_pilot { get; set; }
        public string? ABTest_d2c_optional_phone_number { get; set; }
        public string? ABTest_optional_owner_questions { get; set; }
        public string? ABTest_d2c_long_form { get; set; }
        public string? ABTest_hqx_chatbot_cd { get; set; }
        public string? ABTest_cvg_mod_exp { get; set; }
        public string? click_listings { get; set; }
    }

    /// <summary>
    /// Strongly-typed model for all prefill data - pure data model without logic
    /// </summary>
    public class PrefillDataModel
    {
        public PolicyDataPrefill PolicyData { get; set; } = new();
        public CustomFieldsPrefill CustomFields { get; set; } = new();
    }
}