namespace Bolt.Automation.ApiClients.PlatformApi.Entities.Provision
{
    public class AffiliatedGroup
    {
        public string GroupId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public List<Role> Roles { get; set; } = [];
        public ContactDetails? ContactDetails { get; set; }
        public List<License> Licenses { get; set; } = [];
    }
}