using Bolt.Automation.Common.Models.TestData.DataModels.CommercialLine;

namespace Bolt.Automation.Common.Models.TestData.Interview.Models
{
    public class LocationDetails
    {
        public Guid Id { get; set; }
        public List<Building>? Buildings { get; set; }
        public List<EmployeeClassDetail>? EmployeeClassDetails { get; set; }
        public int SequenceNum { get; set; }
        public bool HasEmployees { get; set; }
    }
}
