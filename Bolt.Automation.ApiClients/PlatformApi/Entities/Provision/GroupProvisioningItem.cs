namespace Bolt.Automation.ApiClients.PlatformApi.Entities.Provision
{
    public class GroupProvisioningItem
    {
        public string RqUID { get; set; } = string.Empty;
        public Group Group { get; set; } = new();
        public ProvisioningParameters Parameters { get; set; } = new();
    }
}