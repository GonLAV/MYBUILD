using Bolt.Automation.ApiClients.CaseManagerApi.Entities.Consumers.CreateConsumer;
using Bolt.Automation.Common.Utils;
using static Bolt.Automation.TestDataProvider.TestData.ApplicationTestData.ApplicationTestData;

namespace Bolt.Automation.TestDataProvider.Providers.CaseManagerAPIDataProvider
{
    public static class CreateConsumerDataProvider
    {
        public static CreateConsumerRequest CreateConsumerData(string externalId, ConsumerLine consumerLine = ConsumerLine.Personal)
        {
            var personalInfo = PersonalInfo.GetRandomPersonalInfo();

            return new CreateConsumerRequest
            {
                ProducerExternalId = externalId,
                ConsumerLine = consumerLine,
                FirstName = personalInfo.FirstName,
                LastName = personalInfo.LastName,
                BusinessName = consumerLine == ConsumerLine.Commercial
                ? RandomManager.GetRandomString(6) : null
            };
        }
    }
}
