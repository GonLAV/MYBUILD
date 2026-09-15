using Bolt.Automation.Common.Models.TestData.Data;

namespace Bolt.Automation.ApiClients.CaseManagerApi.Entities.Consumers.CreateConsumer
{
    public class CreateConsumerRequest
    {
        public string? ProducerExternalId { get; set; }
        public ConsumerLine ConsumerLine { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? BusinessName { get; set; }
        public string? Email { get; set; }
        public Address? MailingAddress { get; set; }
        public string? PhoneNumber { get; set; }

    }

    public enum ConsumerLine : short
    {
        Commercial = 0,
        Personal = 1,
        NA = 2
    }

}
