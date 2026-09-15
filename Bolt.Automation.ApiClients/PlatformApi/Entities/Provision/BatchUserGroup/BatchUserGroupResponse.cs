namespace Bolt.Automation.ApiClients.PlatformApi.Entities.Provision.BatchUserGroup
{
    public class BatchUserGroupResponse
    {
        public string? BatchId { get; set; }

        public BatchEntitiesResponse UserResponses { get; set; } = new();

        public BatchEntitiesResponse GroupResponses { get; set; } = new();
    }
}
