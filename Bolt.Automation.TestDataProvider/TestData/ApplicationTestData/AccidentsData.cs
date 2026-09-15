using Bolt.Automation.Common.Models.TestData.PersonalAutoModels;

namespace Bolt.Automation.TestDataProvider.TestData.ApplicationTestData
{
    public static partial class ApplicationTestData
    {
        public static class Accidents
        {
            public static readonly AccidentModel AtFaultWithNoInjury = new()
            {
                SequenceNum = "0",
                Id = "ddd9ac72-7463-11eb-9439-0242ac130002",
                DateOfAccident = "2018-05-05",
                AccidentPDAmount = "5000",
                AccidentVehicleInvolved = "1",
                Description = "AtFaultWithNoInjury",
                PersonalInjuryAmount = "2000",
                AccidentBIAmount = "600"
            };
        }
    }
}
