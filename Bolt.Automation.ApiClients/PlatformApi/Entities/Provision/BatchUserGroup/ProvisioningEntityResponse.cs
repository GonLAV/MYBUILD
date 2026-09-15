namespace Bolt.Automation.ApiClients.PlatformApi.Entities.Provision.BatchUserGroup
{
    public class ProvisioningEntityResponse
    {
        public Guid? RqUID { get; set; }

        public string?  Id { get; set; }

        public string? Status { get; set; }

        public List<Error>? Errors { get; set; }
    }
}
