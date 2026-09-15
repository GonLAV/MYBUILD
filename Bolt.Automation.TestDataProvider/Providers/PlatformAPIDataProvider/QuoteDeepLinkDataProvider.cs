using Bolt.Automation.ApiClients.PlatformApi.Entities.Quote.QuoteDeepLink;
using Bolt.Automation.TestDataProvider.TestData.PlatformAPITestData;

namespace Bolt.Automation.TestDataProvider.Providers.PlatformAPIDataProvider
{
    public static class QuoteDeepLinkDataProvider
    {
        public static QuoteDeepLinkRequestModel CreateDeepLinkData(string externalId, string sourceName)
        {
            return new QuoteDeepLinkRequestModel
            {
                BOLTExternalId = externalId,
                TransType = QuoteDeepLinkRequestTestData.TransType,
                Org = QuoteDeepLinkRequestTestData.Org,
                SourceName = sourceName,
                RedirectURL = QuoteDeepLinkRequestTestData.RedirectURL,
                RqUID = QuoteDeepLinkRequestTestData.RqUID,
                ClientDt = QuoteDeepLinkRequestTestData.ClientDt
            };
        }
    }
}
