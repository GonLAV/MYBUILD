using Bolt.Automation.ApiClients.PlatformApi.Entities.Common;

namespace Bolt.Automation.TestDataProvider.TestData.PlatformAPITestData
{
    public record QuoteDeepLinkRequestTestData
    {
        public static readonly string SourceName = nameof(SourceNames.MPQ3);
        public static readonly string TransType = nameof(TransitionType.DeepLink);
        public static readonly string BOLTExternalId = "OK0W-8ATE-C429";
        public static readonly string RedirectURL = "https://www.google.com";
        public static readonly string Org = "ProgressivePL";
        public static string ClientDt => DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        public static  Guid RqUID => Guid.NewGuid();       
    }
}
