using Bolt.Automation.Common.Models.TestData.DataModels.CommercialLine;

namespace Bolt.Automation.TestDataProvider.TestData.ApplicationTestData
{
    public static partial class ApplicationTestData
    {
        public static class EmployeeClassData
        {
            public static readonly EmployeeClassDetail EmployeeClassA = new()
            {
                Id = Guid.Parse("7af9df5a-c251-4342-a120-72518f5f6434"),
                FullTime = 1,
                TotalPayroll = 75000,
                Description = "Stores - dry cleaning or laundry collecting or distributing store",
                SequenceNum = 0,
                PartTime = 0,
                ClassCode = "8017"
            };

            public static readonly EmployeeClassDetail EmployeeClassB = new()
            {
                Id = Guid.Parse("6b38cef4-880e-11ec-a8a3-0242ac120002"),
                FullTime = 2,
                TotalPayroll = 98000,
                Description = "Store - clothing, wearing apparel or dry goods - wholesale",
                SequenceNum = 1,
                PartTime = 0,
                ClassCode = "8032"
            };

            public static readonly EmployeeClassDetail EmployeeClassC = new()
            {
                Id = Guid.Parse("b463322c-880e-11ec-a8a3-0242ac120002"),
                FullTime = 2,
                TotalPayroll = 145000,
                Description = "Dry cleaning or laundry - commercial and route salespersons, drivers",
                SequenceNum = 2,
                PartTime = 0,
                ClassCode = "2591"
            };
        }
    }
}
