using Bolt.Automation.Common.Models.TestData.Data;

namespace Bolt.Automation.Common.Models.TestData.DataModels.CommercialLine
{
    public partial class CommercialLineData : BaseLineData
    {
        public List<Location>? Locations { get; set; }
    }
    public class Location
    {
        public Guid Id { get; set; }
        public List<Building>? Buildings { get; set; }
        public List<EmployeeClassDetail>? EmployeeClassDetails { get; set; }
        public int SequenceNum { get; set; }
        public bool HasEmployees { get; set; }
    }
    public class Building
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
        public int? AnnualPayroll { get; set; }
        public int? PersonalPropertyReplacementCost { get; set; }
    }
    public class EmployeeClassDetail
    {
        public Guid Id { get; set; }
        public int FullTime { get; set; }
        public decimal TotalPayroll { get; set; }
        public string? Description { get; set; }
        public int SequenceNum { get; set; }
        public int PartTime { get; set; }
        public string? ClassCode { get; set; }
    }

}
