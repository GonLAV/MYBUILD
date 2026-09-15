namespace Bolt.Automation.TestDataProvider.TestData.ApplicationTestData
{
    public static partial class ApplicationTestData
    {
        /// <summary>
        /// UI form data dictionaries for Auto flows, keyed by FieldNames constants.
        /// Shared across D2C, Interview, and any other front-end that drives Auto questionnaires.
        /// Keys match the <c>nameof</c>-based constants in <c>CommonFieldNames</c> and <c>FieldNames</c>.
        /// </summary>
        public static class DriverFormData
        {
            /// <summary>
            /// Primary driver: GAIL SCHAFF — used in Safeco purchase flows that require an IBS score
            /// of 900 (driven by the ForceIBSScore900@safeco.com email address).
            /// </summary>
            public static readonly Dictionary<string, string> GailSchaffPrimary = new()
            {
                ["FirstName"] = "GAIL",
                ["LastName"] = "SCHAFF",
                ["Gender"] = "Male",
                ["DateOfBirth"] = "08/03/1958",
                ["Email"] = "ForceIBSScore900@safeco.com"
            };

            /// <summary>
            /// Secondary driver: TEST ABBADE — used as an additional driver in Auto flows
            /// to exercise the add-driver path and verify driver lifecycle.
            /// </summary>
            public static readonly Dictionary<string, string> AbbadeSecondaryDriver = new()
            {
                ["FirstName"] = "Test",
                ["LastName"] = "Abbade",
                ["DateOfBirth"] = "05/05/1990",
                ["DriverRelationshipToMainDriver"] = "Other"
            };

            /// <summary>
            /// Full-quote driver license details for the Safeco FQ driver-details page.
            /// <c>DriverDateLicensed</c> is computed at runtime (15 years before today).
            /// </summary>
            public static Dictionary<string, string> SafecoLicenseDetails => new()
            {
                ["DriverLicenseNumber"] = "25986748",
                ["DriverDateLicensed"] = DateTime.Today.AddYears(-15).ToString("MM/dd/yyyy")
            };
        }

        /// <summary>
        /// UI form data dictionaries for vehicle interactions in Auto flows.
        /// </summary>
        public static class VehicleFormData
        {
            /// <summary>
            /// 2007 LEXUS IS 250 Sedan — used in vehicle lifecycle and AgencyOne integration tests.
            /// </summary>
            public static readonly Dictionary<string, string> Lexus2007 = new()
            {
                ["PLMake"] = "LEXUS",
                ["PLModel"] = "IS 250",
                ["PLYear"] = "2007",
                ["BodyStyle"] = "SEDAN",
                ["VehicleOwnerShip"] = "Owned",
                ["AnnualMileage"] = "10000"
            };

        }

        /// <summary>
        /// UI form-data defaults for the CL Auto interview flow (Business -> Vehicle ->
        /// Operator -> CL Policy). Covers every field whose value differs from the field
        /// registry's DefaultValue. Tests start from this dictionary and override per-test
        /// fields (identity, address, VIN, NAIC industry, driver, effective date).
        /// Other CL flows (BOP, WC) should add their own sibling class.
        /// </summary>
        public static class CLAutoFormData
        {
            public static Dictionary<string, string> Defaults => new()
            {
                // Business — Corporation w/ FEIN, started current year, zero payroll
                ["LegalEntity"] = "Corporation",
                ["FederalIDNumber"] = "478500001",
                ["BusinessStartYear"] = DateTime.Now.Year.ToString(),
                ["AnnualPayroll"] = "0",

                // Vehicle — utility trailer used commercially. VIN supplied per-test.
                ["AnnualMileage"] = "201-300 miles",
                ["TruckSubCategory"] = "Utility Trailer",
                ["VehicleOwnerShip"] = "Less than 1 month",
                ["PrimaryUseOfVehicle"] = "Business Only",

                // CL Policy — coverage limits + deductibles. Product_CLPolicyPage's
                // arrow-wrapper override falls back to first option if the text doesn't
                // match this carrier's dropdown. Comp first-option ("No Coverage") was
                // blocking Collision — explicit values needed.
                ["CurrentBopCarrier"] = "My insurance company is not listed",
                ["CL_BIPD"] = "100,000 / 300,000",
                ["CombinedUninsuredUnderinsuredMotorist"] = "100,000 / 300,000",
                ["UninsuredMotoristPropertyDamage"] = "100,000",
                ["CL_ComprehensiveDeductible"] = "100",
                ["CL_CollisionDeductible"] = "5000",
                ["CL_CurrentVehicleValue"] = "45000"
            };
        }
    }
}
