using Bolt.Automation.ApiClients.PlatformApi.Entities.Quote.QuoteDeepLink;
using Bolt.Automation.TestDataProvider.TestData.PlatformAPITestData;

namespace Bolt.Automation.TestDataProvider.Providers.PlatformAPIDataProvider
{
    public static class DeepLinkDataProvider
    {
        public static QuoteDeepLinkRequestModel CreateDeepLinkData(string externalId)
        {
            return new QuoteDeepLinkRequestModel
            {
                BOLTExternalId = externalId,
                RedirectURL = QuoteDeepLinkRequestTestData.RedirectURL,
                ClientDt = QuoteDeepLinkRequestTestData.ClientDt,
                TransType = QuoteDeepLinkRequestTestData.TransType,
                Org = QuoteDeepLinkRequestTestData.Org,
                RqUID = QuoteDeepLinkRequestTestData.RqUID,
                SourceName = QuoteDeepLinkRequestTestData.SourceName
            };
        }
    }
}
