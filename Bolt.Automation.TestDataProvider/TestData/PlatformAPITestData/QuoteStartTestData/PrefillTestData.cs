namespace Bolt.Automation.TestDataProvider.TestData.PlatformAPITestData.QuoteStartTestData
{
    /// <summary>
    /// Static test data for Platform API prefill configurations following ApplicationTestData pattern
    /// </summary>
    public static partial class QuoteStartRequestTestData
    {
        public static class PrefillTestData
        {
            /// <summary>
            /// Standard AB test configuration data
            /// </summary>
            public static readonly PolicyDataPrefill StandardPolicyData = new()
            {
                // No policy-specific prefills for standard configuration
            };

            public static readonly CustomFieldsPrefill StandardCustomFields = new()
            {
                ABTest_d2c_optional_phone_number = "1966B",
                ABTest_optional_owner_questions = "1972B",
                ABTest_hqx_chatbot_cd = "1970B"
            };

            /// <summary>
            /// Comprehensive property and policy prefill data
            /// </summary>
            public static readonly PolicyDataPrefill FullPolicyData = new()
            {
                YearsAtAddress = 6,
                RoofResponsible = false,
                PrimaryHome = true,
                EligibilityFinancialHardship = false,
                BusinessOnResidencePremises = false,
                PLYearBuilt = 2020, // Keep as null as specified
                PL_RoofUpdateYearRange = "ZeroToFour",
                ArchitectureStyle = "Ranch",
                PerimeterSecurityDD = "No",
                PL_PrimaryCounterMaterial = "GraniteorMarble",
                PL_HeatedByOil = false,
                NumberOfFirePlaces = 0,
                PLHeatingType = "Gas",
                AnyAdditionalInsured = false,
                PriorInsuranceProperty = true,
                CurrentPersonalHomeownerCarrier = "AAA",
                YearsWithPriorCarrierHome = 5,
                PurchaseDate = "2024-11-11",
                DateOccupied = "2024-11-11",
                NumberOfClaimsHistory = 0,
                Progressive_Preferences1 = 11,
                Progressive_Preferences2 = 11,
                HomeAutoInsurance = false,
                MitWindowOpening = "Intermediate",
                SmokeDetector = true,
                PL_CentralAC = true,
                PLHighRiseCondo = false,
                PLPersonalProperty = 120000,
                EffectiveDate = DateTime.Now.ToString("yyyy-MM-dd"),
                PL_AdditionalStructures_Pool = false

            };

            public static readonly CustomFieldsPrefill FullCustomFields = new()
            {
                // Include all AB test fields
                ABTest_d2c_optional_phone_number = "1966B",
                ABTest_optional_owner_questions = "1972B",
                ABTest_hqx_chatbot_cd = "1970B",
                ABTest_cvg_mod_exp = "1977B",
                // Additional custom fields
                //click_listings = "Yes"
            };

            /// <summary>
            /// New homeowner configuration (no insurance history)
            /// </summary>
            public static readonly PolicyDataPrefill NewHomeownerPolicyData = new()
            {
                YearsAtAddress = 1,
                PrimaryHome = true,
                PriorInsuranceProperty = false,
                NumberOfClaimsHistory = 0,
                PLYearBuilt = DateTime.Now.Year - 1, // New construction
                PL_RoofUpdateYearRange = "ZeroToFour",
                EffectiveDate = DateTime.Now.AddDays(30).ToString("yyyy-MM-dd")
            };

            /// <summary>
            /// Experienced homeowner configuration (with claims history)
            /// </summary>
            public static readonly PolicyDataPrefill ExperiencedHomeownerPolicyData = new()
            {
                YearsAtAddress = 15,
                PrimaryHome = true,
                PriorInsuranceProperty = true,
                CurrentPersonalHomeownerCarrier = "StateFarm",
                YearsWithPriorCarrierHome = 8,
                NumberOfClaimsHistory = 2,
                PLYearBuilt = 1995,
                PL_RoofUpdateYearRange = "FiveToTen"
            };

            /// <summary>
            /// Rental property owner configuration
            /// </summary>
            public static readonly PolicyDataPrefill RentalPropertyPolicyData = new()
            {
                YearsAtAddress = 5,
                PrimaryHome = false, // Rental property
                BusinessOnResidencePremises = true,
                PriorInsuranceProperty = true,
                CurrentPersonalHomeownerCarrier = "Liberty",
                NumberOfClaimsHistory = 1
            };
        }
    }
}