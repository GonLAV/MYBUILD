namespace Bolt.Automation.ApiClients.PlatformApi.Entities.Provision
{
    public class ParentGroup
    {
        public string ParentGroupId { get; set; } = string.Empty;
        public string HierarchyType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? EndDate { get; set; }
    }
}