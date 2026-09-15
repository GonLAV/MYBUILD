using Bolt.Automation.Common.Models.TestData.DataModels.CommercialLine;
using Address = Bolt.Automation.Common.Models.TestData.Data.Address;

namespace Bolt.Automation.TestDataProvider.TestData.ApplicationTestData
{
    public static partial class ApplicationTestData
    {
        public static class BuildingData
        {
            public static readonly Building BuildingA = new()
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                LocationAddress = new Address
                {
                    AddressLine1 = "4906 Cotton Row NW",
                    State = "AL",
                    City = "Huntsville",
                    ZipCode = "35816",
                    County = "MADISON"
                },
                AnySubcontractedWork = false,
                SquareFootageOccupied = 200,
                SequenceNum = 0,
                LocationOption = "HomeOffice",
                YearOriginalConstruction = 2000,
                NumberOfStories = "1",
                AnnualSales = 500000,
                ConstructionType = "Frame"
            };

            public static readonly Building BuildingB = new()
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                LocationAddress = new Address
                {
                    AddressLine1 = "1234 Main St",
                    State = "TX",
                    City = "Dallas",
                    ZipCode = "75201",
                    County = "DALLAS"
                },
                AnySubcontractedWork = true,
                SquareFootageOccupied = 500,
                SequenceNum = 1,
                LocationOption = "BranchOffice",
                YearOriginalConstruction = 2010,
                NumberOfStories = "2",
                AnnualSales = 200000,
            };
        }
    }
}
