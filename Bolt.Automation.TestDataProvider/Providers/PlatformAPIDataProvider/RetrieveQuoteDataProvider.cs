using Bolt.Automation.ApiClients.PlatformApi.Entities.Quote.QuoteRetrieve;
using Bolt.Automation.TestDataProvider.TestData.PlatformAPITestData;

namespace Bolt.Automation.TestDataProvider.Providers.PlatformAPIDataProvider
{
    public static class RetrieveQuoteDataProvider
    {
        public static RetrievalRequestWrapperModel CreateQuoteRerievalData(string friendlyId, string lastName, string zipCode) =>
            CreateQuoteRetrievalRequest(friendlyId, lastName, zipCode);

        public static RetrievalRequestWrapperModel CreateQuoteRetrievalRequest() =>
            CreateQuoteRetrievalRequest(
                QuoteRetrievalRequestTestData.AppId,
                QuoteRetrievalRequestTestData.LastName,
                QuoteRetrievalRequestTestData.ZipCode);

        public static RetrievalRequestWrapperModel CreateQuoteRetrievalRequest(string friendlyId, string lastName, string zipCode)
        {
            var retrievalRequest = new RetrievalRequestWrapperModel
            {
                RetrievalRq = new QuoteRetrievalRequestModel
                {
                    AppId = friendlyId,
                    LastName = lastName,
                    ZipCode = zipCode,
                    RqUID = QuoteRetrievalRequestTestData.RqUID
                }
            };

            return retrievalRequest;
        }
    }
}
