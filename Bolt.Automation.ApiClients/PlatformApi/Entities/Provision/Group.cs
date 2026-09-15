namespace Bolt.Automation.ApiClients.PlatformApi.Entities.Provision
{
    public class Group
    {
        public string GroupId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string ExternalGroupType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public List<ParentGroup> ParentGroups { get; set; } = [];
        public Address? PhysicalAddress { get; set; }
        public Contact? Contact { get; set; }
    }
}