using Bolt.Automation.ApiClients.CaseManagerApi.Entities.Consumers.CreateConsumer;

namespace Bolt.Automation.ApiClients.CaseManagerApi.Entities.Consumers.UpdateConsumer
{
    public class UpdateConsumerRequest : CreateConsumerRequest
    {
        public string? ExternalId { get; set; }
    }
}
