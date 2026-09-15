using Bolt.Automation.Common.Models.TestData.PersonalAutoModels;

namespace Bolt.Automation.TestDataProvider.TestData.ApplicationTestData
{
    public static partial class ApplicationTestData
    {
        public static class Violations
        {
            public static readonly ViolationModel DefectiveEquipment = new()
            {
                SequenceNum = 0,
                Id = "73720262-7463-11eb-9439-0242ac130002",
                Description = "DefectiveEquipment",
                DateOfViolation = "2024-03-15"
            };
        }
    }
}
