using Bolt.Automation.Common.Enums;

namespace Bolt.Automation.TestDataProvider.TestData.CoverageModificationTestData
{
    /// <summary>
    /// Expected carrier "information statement" copy shown on the Rates page, keyed by carrier.
    /// The stored text is whitespace-normalized (runs of whitespace collapsed to single spaces) to
    /// match the reader (<c>CarrierSelectionService.GetCarrierInformationStatementAsync</c>), so tests
    /// can compare the rendered statement against the expected value without re-formatting.
    /// </summary>
    public static class CarrierInformationStatementData
    {
        public static string GetExpectedStatement(CarrierEnums carrier) =>
            ExpectedStatements.TryGetValue(carrier, out var statement)
                ? statement
                : throw new KeyNotFoundException($"No expected information statement is defined for carrier '{carrier}'.");

        public static readonly Dictionary<CarrierEnums, string> ExpectedStatements = new()
        {
            // Assurant manufactured-home program (underwritten by American Bankers Insurance Company).
            [CarrierEnums.Assurant] =
                "This Manufactured Home policy is provided and serviced by American Bankers Insurance Company. " +
                "We work with American Bankers Insurance Company and sell their policies. " +
                "Information about this carrier: " +
                "FORTUNE 500 company highly rated for financial strength. " +
                "Protect your belongings – Assurant has over 50 years of experience in the homeowners insurance business and currently protects more than 32 million homes. " +
                "24x7 Service: Manage your policy with the help of one of our agents or through our self-service website.",
        };
    }
}
