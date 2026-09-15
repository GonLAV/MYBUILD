using Bolt.Automation.Common.Enums;
using NUnit.Framework;

namespace Bolt.Automation.Tests.TestData.Progressive;

public static class HQXConsumerTestCases
{
    /// <summary>
    /// States used to verify dog breed and bite history questions are hidden by default.
    /// </summary>
    public static IEnumerable<TestCaseData> DogBreedCases
    {
        get
        {
            yield return new TestCaseData(AddressKey.NY_Averill_Park).SetProperty("TestCaseId", "170567");
            yield return new TestCaseData(AddressKey.AZ).SetProperty("TestCaseId", "174827");
            yield return new TestCaseData(AddressKey.CO).SetProperty("TestCaseId", "174828");
            yield return new TestCaseData(AddressKey.IL_Peoria).SetProperty("TestCaseId", "174829");
            yield return new TestCaseData(AddressKey.MN).SetProperty("TestCaseId", "174830");
        }
    }

    /// <summary>
    /// LOB + state combinations for coverage display name and help text tooltip verification.
    /// </summary>
    public static IEnumerable<TestCaseData> CoverageVerificationCases
    {
        get
        {
            yield return new TestCaseData(LOBEnums.HO6, AddressKey.FL).SetProperty("TestCaseId", "236244");
            yield return new TestCaseData(LOBEnums.HO6, AddressKey.OH).SetProperty("TestCaseId", "236245");
            yield return new TestCaseData(LOBEnums.HO6, AddressKey.NY_Averill_Park).SetProperty("TestCaseId", "219854");
            yield return new TestCaseData(LOBEnums.HO3, AddressKey.NY_Averill_Park).SetProperty("TestCaseId", "219852");
            yield return new TestCaseData(LOBEnums.HO3, AddressKey.OH).SetProperty("TestCaseId", "236246");
            yield return new TestCaseData(LOBEnums.HO3, AddressKey.FL).SetProperty("TestCaseId", "219855");
        }
    }
}
