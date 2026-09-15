using Bolt.Automation.Common.Enums;
using NUnit.Framework;

namespace Bolt.Automation.Tests.TestData.Progressive;

public static class CovModTestCases
{
    /// <summary>
    /// State + carrier permutations for coverage modification dropdown validation in HQX 2.0.
    /// </summary>
    public static IEnumerable<TestCaseData> DropdownCases
    {
        get
        {
            // Homesite
            yield return new TestCaseData(AddressKey.AZ, CarrierEnums.Homesite).SetProperty("TestCaseId", "231014");
            yield return new TestCaseData(AddressKey.IL_Peoria, CarrierEnums.Homesite).SetProperty("TestCaseId", "219185");
            yield return new TestCaseData(AddressKey.NV, CarrierEnums.Homesite).SetProperty("TestCaseId", "199791");
            yield return new TestCaseData(AddressKey.GA, CarrierEnums.Homesite).SetProperty("TestCaseId", "199786");
            yield return new TestCaseData(AddressKey.KY, CarrierEnums.Homesite).SetProperty("TestCaseId", "199790");
            yield return new TestCaseData(AddressKey.OH, CarrierEnums.Homesite).SetProperty("TestCaseId", "171930");
            yield return new TestCaseData(AddressKey.IN, CarrierEnums.Homesite).SetProperty("TestCaseId", "1705671"); //Pending Manual TC
            yield return new TestCaseData(AddressKey.WI, CarrierEnums.Homesite).SetProperty("TestCaseId", "219293");
            yield return new TestCaseData(AddressKey.TX_Magnolia, CarrierEnums.Homesite).SetProperty("TestCaseId", "199792");
            yield return new TestCaseData(AddressKey.MO, CarrierEnums.Homesite).SetProperty("TestCaseId", "219294");
            yield return new TestCaseData(AddressKey.PA, CarrierEnums.Homesite).SetProperty("TestCaseId", "219295");
            yield return new TestCaseData(AddressKey.TN, CarrierEnums.Homesite).SetProperty("TestCaseId", "219297");
            yield return new TestCaseData(AddressKey.NJ, CarrierEnums.Homesite).SetProperty("TestCaseId", "199793");

            // ASI
            yield return new TestCaseData(AddressKey.AZ, CarrierEnums.ASI).SetProperty("TestCaseId", "199794");
            yield return new TestCaseData(AddressKey.OR, CarrierEnums.ASI).SetProperty("TestCaseId", "199893");
            yield return new TestCaseData(AddressKey.WA, CarrierEnums.ASI).SetProperty("TestCaseId", "199892");
            yield return new TestCaseData(AddressKey.UT, CarrierEnums.ASI).SetProperty("TestCaseId", "199905");
            yield return new TestCaseData(AddressKey.NV, CarrierEnums.ASI).SetProperty("TestCaseId", "219183");
            yield return new TestCaseData(AddressKey.ID, CarrierEnums.ASI).SetProperty("TestCaseId", "199894");
            //yield return new TestCaseData(AddressKey.TN, CarrierEnums.ASI).SetProperty("TestCaseId", "229306"); //236338
            //yield return new TestCaseData(AddressKey.WI, CarrierEnums.ASI).SetProperty("TestCaseId", "1998943"); //Pending Manual TC
            //yield return new TestCaseData(AddressKey.IN, CarrierEnums.ASI).SetProperty("TestCaseId", "232267"); //236338
            //yield return new TestCaseData(AddressKey.PA, CarrierEnums.ASI).SetProperty("TestCaseId", "1998942"); //Pending Manual TC
            //yield return new TestCaseData(AddressKey.KY, CarrierEnums.ASI).SetProperty("TestCaseId", "232206");
            //yield return new TestCaseData(AddressKey.MN, CarrierEnums.ASI).SetProperty("TestCaseId", "1998945"); //Pending Manual TC
            yield return new TestCaseData(AddressKey.OH, CarrierEnums.ASI).SetProperty("TestCaseId", "229305");
            yield return new TestCaseData(AddressKey.MI, CarrierEnums.ASI).SetProperty("TestCaseId", "229300");
            //yield return new TestCaseData(AddressKey.NY_Averill_Park, CarrierEnums.ASI).SetProperty("TestCaseId", "229304");

            // PlymouthRock
            yield return new TestCaseData(AddressKey.NJ_Bridgewater, CarrierEnums.PlymouthRock).SetProperty("TestCaseId", "212975");
            yield return new TestCaseData(AddressKey.CT_Cheshire, CarrierEnums.PlymouthRock).SetProperty("TestCaseId", "212973");
            yield return new TestCaseData(AddressKey.NH_NOTTINGHAM, CarrierEnums.PlymouthRock).SetProperty("TestCaseId", "212976");
            yield return new TestCaseData(AddressKey.MA_Westford, CarrierEnums.PlymouthRock).SetProperty("TestCaseId", "212967");
            yield return new TestCaseData(AddressKey.PA_Bensalem, CarrierEnums.PlymouthRock).SetProperty("TestCaseId", "212958");
        }
    }
}
