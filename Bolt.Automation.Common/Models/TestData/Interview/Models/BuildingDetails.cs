using Bolt.Automation.Common.Models.TestData.Data;

namespace Bolt.Automation.Common.Models.TestData.Interview.Models
{
    public class BuildingDetails
    {
            public Guid Id { get; set; }
            public Address? LocationAddress { get; set; }
            public bool AnySubcontractedWork { get; set; }
            public int SquareFootageOccupied { get; set; }
            public int SequenceNum { get; set; }
            public string? LocationOption { get; set; }
            public int YearOriginalConstruction { get; set; }
            public string? NumberOfStories { get; set; }
            public string? ConstructionType { get; set; }
            public int? AnnualSales { get; set; }

    }
}
