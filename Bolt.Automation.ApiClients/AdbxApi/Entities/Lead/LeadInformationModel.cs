
namespace Bolt.Automation.ApiClients.AdbxApi.Entities.Lead
{
    public class LeadInformationModel
    {
        public string? LeadNumber { get; set; }
        public DateTime DateCreated { get; set; }
        public string? Status { get; set; }
        public string? Stage { get; set; }
        public string? Source { get; set; }
        public string? SourceDisplayName { get; set; }
        public DateTime? DueDate { get; set; }
        public Guid? AssignedToId { get; set; }
        public string? AssignedToDisplayName { get; set; }
        public string? CreatedBy { get; set; }
        public bool NeedHelp { get; set; }
        public string? TCPA { get; set; }
    }
}
