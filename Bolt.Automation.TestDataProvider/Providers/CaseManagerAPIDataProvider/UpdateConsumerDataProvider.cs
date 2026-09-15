
using Bolt.Automation.ApiClients.CaseManagerApi.Entities.Consumers.UpdateConsumer;
using static Bolt.Automation.TestDataProvider.TestData.ApplicationTestData.ApplicationTestData;

namespace Bolt.Automation.TestDataProvider.Providers.CaseManagerAPIDataProvider
{
    public static class UpdateConsumerDataProvider
    {
        public static UpdateConsumerRequest UpdateConsumerData(string producerExternalId, string externalId)
        {
            var personalInfo = PersonalInfo.GetRandomPersonalInfo();
            return new UpdateConsumerRequest
            {
                ProducerExternalId = producerExternalId,
                ExternalId = externalId,
                Email = personalInfo.Email
            };
        }
    }
}
