using Bolt.Automation.Common.Enums;
using NUnit.Framework;

namespace Bolt.Automation.Tests.TestData.Progressive;

public static class PaaTestCases
{
    /// <summary>
    /// Carrier + state combinations for Homeowners (HO3) carrier-specific questions validation.
    /// </summary>
    public static IEnumerable<TestCaseData> CarrierQuestionsHomeOwnersCases
    {
        get
        {
            yield return new TestCaseData(CarrierEnums.Bamboo, AddressKey.CA)
                .SetProperty("TestCaseId", "228954");
            // Excess & Surplus (Bamboo Surplus) — reached via the "Get E&S HO rates" button on
            // the Selected Carrier page rather than a carrier tile; selection branch lives in
            // PaaTestHelper.SelectCarrierAndContinueAsync.
            yield return new TestCaseData(CarrierEnums.BambooSurplus, AddressKey.TX_Crowley)
                .SetProperty("TestCaseId", "245315");
            yield return new TestCaseData(CarrierEnums.Homesite, AddressKey.OH)
                .SetProperty("TestCaseId", "214136");
            yield return new TestCaseData(CarrierEnums.StillWater, AddressKey.TN)
                .SetProperty("TestCaseId", "216030");
            yield return new TestCaseData(CarrierEnums.PlymouthRock, AddressKey.NJ_Bridgewater)
                .SetProperty("TestCaseId", "216939");
            yield return new TestCaseData(CarrierEnums.Nationwide, AddressKey.UT)
                .SetProperty("TestCaseId", "219125");
            yield return new TestCaseData(CarrierEnums.ASI, AddressKey.UT)
                .SetProperty("TestCaseId", "214941");
            yield return new TestCaseData(CarrierEnums.Openly, AddressKey.UT)
                .SetProperty("TestCaseId", "219247");
            yield return new TestCaseData(CarrierEnums.TowerHill, AddressKey.FL)
                .SetProperty("TestCaseId", "219259");
            yield return new TestCaseData(CarrierEnums.AII, AddressKey.FL)
                .SetProperty("TestCaseId", "219249");
            yield return new TestCaseData(CarrierEnums.TowerHill, AddressKey.TN)
                .SetProperty("TestCaseId", "227984");
            yield return new TestCaseData(CarrierEnums.ForemostSignature, AddressKey.TN)
                .SetProperty("TestCaseId", "228773");
            yield return new TestCaseData(CarrierEnums.True, AddressKey.FL)
                .SetProperty("TestCaseId", "219570");
        }
    }

    /// <summary>
    /// Carrier + state combinations for Dwelling Fire (DF) carrier-specific questions validation.
    /// </summary>
    public static IEnumerable<TestCaseData> CarrierQuestionsDwellingFireCases
    {
        get
        {
            yield return new TestCaseData(CarrierEnums.Foremost, AddressKey.TN)
                .SetProperty("TestCaseId", "219595");
            yield return new TestCaseData(CarrierEnums.AmericanModern, AddressKey.TN)
                .SetProperty("TestCaseId", "219593");
        }
    }

    /// <summary>
    /// Carrier + state combinations for Condominium (HO6) carrier-specific questions validation.
    /// </summary>
    public static IEnumerable<TestCaseData> CarrierQuestionsCondominiumCases
    {
        get
        {
            yield return new TestCaseData(CarrierEnums.Homesite, AddressKey.OH)
                .SetProperty("TestCaseId", "215323");
            yield return new TestCaseData(CarrierEnums.StillWater, AddressKey.TN)
                .SetProperty("TestCaseId", "219384");
            yield return new TestCaseData(CarrierEnums.PlymouthRock, AddressKey.NJ_Bridgewater)
                .SetProperty("TestCaseId", "221193");
            yield return new TestCaseData(CarrierEnums.Nationwide, AddressKey.UT)
                .SetProperty("TestCaseId", "221194");
            yield return new TestCaseData(CarrierEnums.ASI, AddressKey.UT)
                .SetProperty("TestCaseId", "219138");
        }
    }

    /// <summary>
    /// Carrier + state combinations for Manufactured Home (MH) carrier-specific questions validation.
    /// </summary>
    public static IEnumerable<TestCaseData> CarrierQuestionsManufacturedHomeCases
    {
        get
        {
            yield return new TestCaseData(CarrierEnums.Assurant, AddressKey.TN)
                .SetProperty("TestCaseId", "219572");
            yield return new TestCaseData(CarrierEnums.Foremost, AddressKey.TN)
                .SetProperty("TestCaseId", "219573");
            yield return new TestCaseData(CarrierEnums.AmericanModern, AddressKey.TN)
                .SetProperty("TestCaseId", "219567");
        }
    }

    /// <summary>
    /// Carrier + state combinations for Dwelling Fire (DF) carrier bridge URL validation.
    /// </summary>
    public static IEnumerable<TestCaseData> CarrierBridgeDwellingFireCases
    {
        get
        {
            yield return new TestCaseData(CarrierEnums.AmericanModern, AddressKey.TN)
                .SetProperty("TestCaseId", "214367");
            yield return new TestCaseData(CarrierEnums.Foremost, AddressKey.TN)
                .SetProperty("TestCaseId", "214428");
        }
    }

    /// <summary>
    /// Carrier + state combinations for Homeowners (HO3) carrier bridge URL validation.
    /// </summary>
    public static IEnumerable<TestCaseData> CarrierBridgeHomeOwnersCases
    {
        get
        {
            yield return new TestCaseData(CarrierEnums.Homesite, AddressKey.OH)
                .SetProperty("TestCaseId", "219602");
            yield return new TestCaseData(CarrierEnums.StillWater, AddressKey.TN)
                .SetProperty("TestCaseId", "221569");
            yield return new TestCaseData(CarrierEnums.PlymouthRock, AddressKey.NJ_Bridgewater)
                .SetProperty("TestCaseId", "221522");
            yield return new TestCaseData(CarrierEnums.Nationwide, AddressKey.UT)
                .SetProperty("TestCaseId", "219606");
            yield return new TestCaseData(CarrierEnums.ASI, AddressKey.UT)
                .SetProperty("TestCaseId", "219601");
            yield return new TestCaseData(CarrierEnums.Openly, AddressKey.UT)
                .SetProperty("TestCaseId", "221520");
            yield return new TestCaseData(CarrierEnums.TowerHill, AddressKey.FL)
                .SetProperty("TestCaseId", "221568");
            // Excess & Surplus (Bamboo Surplus) — reached via the "Get E&S HO rates" button;
            // selection branch lives in PaaTestHelper.SelectCarrierAndContinueAsync.
            yield return new TestCaseData(CarrierEnums.BambooSurplus, AddressKey.TX_Crowley)
                .SetProperty("TestCaseId", "245309");
        }
    }

    /// <summary>
    /// Carrier + state combinations for Manufactured Home (MH) carrier bridge URL validation.
    /// </summary>
    public static IEnumerable<TestCaseData> CarrierBridgeManufacturedHomeCases
    {
        get
        {
            yield return new TestCaseData(CarrierEnums.Assurant, AddressKey.TN)
                .SetProperty("TestCaseId", "221524");
        }
    }
}
