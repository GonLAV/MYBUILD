namespace Bolt.Automation.TestDataProvider.TestData.ApplicationTestData
{
    public static partial class ApplicationTestData
    {
        /// <summary>
        /// UI form data for the CL Workers' Compensation interview flow, keyed by FieldNames constants.
        /// Covers the fields whose value differs from the field registry's DefaultValue; tests start from
        /// this dictionary and override per-test.
        /// </summary>
        public static class CLWCFormData
        {
            /// <summary>
            /// A sole-proprietor linen-supply business: one employee (the owner), no formal safety
            /// program. From TC 253222's policy data.
            /// </summary>
            /// <remarks>
            /// The industry currently matches the CL Auto profile because TC 253181/253222 carry the wrong
            /// policy-data attachment (bead nx-639) - when the real WC data arrives, it changes here.
            /// <para>
            /// A property returning a new dictionary, deliberately, where the siblings in this namespace
            /// are <c>static readonly</c> fields. A test that takes one of those and overrides a key
            /// mutates it for every later test in the process; handing out a fresh copy each call is what
            /// makes "start from this and override per-test" safe. Do not "normalise" it to a field.
            /// </para>
            /// </remarks>
            public static Dictionary<string, string> Defaults => new()
            {
                ["EposNaicDescription"] = "Linen Supply",

                // Employee and officer classifications
                ["EmployeeClassificationSearch"] = "8215",
                ["EmployeeClassificationPayroll"] = "80000",
                ["OfficerEmployeeClassificationSearch"] = "8116",
                ["WCWhoIsCovered"] = "Myself as the owner",
                ["EmployeeWorkplaceSafetyProgram"] = "No",

                // This scenario really is a new business, which is not the registry default - see the
                // IsNewBusiness comment in FieldRegistryInterview for what ticking it reveals.
                ["IsNewBusiness"] = "true"
            };
        }
    }
}
