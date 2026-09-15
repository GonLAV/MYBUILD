namespace Bolt.Automation.ApiClients.PlatformApi.Entities.Provision
{
    public class UserProvisioningItem
    {
        public string RqUID { get; set; } = string.Empty;
        public User User { get; set; } = new();
        public ProvisioningParameters Parameters { get; set; } = new();
    }
}