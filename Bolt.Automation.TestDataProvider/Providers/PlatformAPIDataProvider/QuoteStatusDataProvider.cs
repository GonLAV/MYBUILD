using Bolt.Automation.ApiClients.PlatformApi.Entities.Quote.QuoteStatus;
using Bolt.Automation.TestDataProvider.TestData.PlatformAPITestData;

namespace Bolt.Automation.TestDataProvider.Providers.PlatformAPIDataProvider
{
    public static class QuoteStatusDataProvider
    {
        public static QuoteStatusRequestModel CreateQuoteStatusData(string externalId, string sourceName)
        {
            return new QuoteStatusRequestModel
            {
                BOLTExternalId = externalId,
                ClientDt = QuoteStatusRequestTestData.ClientDt,
                TransType = QuoteStatusRequestTestData.TransType,
                Org = QuoteStatusRequestTestData.Org,
                RqUID = QuoteStatusRequestTestData.RqUID,
                SourceName = sourceName
            };
        }
    }
}
