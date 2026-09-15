using Bolt.Automation.Common.Models.TestData.DataModels.CommercialLine;

namespace Bolt.Automation.TestDataProvider.TestData.ApplicationTestData
{
    public static partial class ApplicationTestData
    {
        public static class LocationsData
        {
            public static readonly Location LocationA = new()
            {
                Id = Guid.Parse("22503c09-57dd-4a3e-8e49-3609c9477130"),
                SequenceNum = 0,
                HasEmployees = true,
                Buildings = new List<Building> { BuildingData.BuildingA },
                EmployeeClassDetails = new List<EmployeeClassDetail>
                {
                    EmployeeClassData.EmployeeClassA,
                    EmployeeClassData.EmployeeClassB,
                    EmployeeClassData.EmployeeClassC
                }
            };

            public static readonly Location LocationB = new()
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                SequenceNum = 1,
                HasEmployees = false,
                Buildings = new List<Building> { BuildingData.BuildingB },
                EmployeeClassDetails = new List<EmployeeClassDetail>
                {
                    // ... different employee class details ...
                }
            };
        }
    }
}
