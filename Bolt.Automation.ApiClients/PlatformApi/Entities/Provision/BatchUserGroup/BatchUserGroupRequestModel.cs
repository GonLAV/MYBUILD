namespace Bolt.Automation.ApiClients.PlatformApi.Entities.Provision.BatchUserGroup
{
    public class BatchUserGroupRequestModel
    {
        public double BatchId { get; set; }
        public List<GroupProvisioningItem> Groups { get; set; } = [];
        public List<UserProvisioningItem> Users { get; set; } = [];
    }
}