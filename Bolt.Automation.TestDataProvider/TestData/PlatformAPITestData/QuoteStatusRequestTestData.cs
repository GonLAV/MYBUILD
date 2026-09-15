using Bolt.Automation.ApiClients.PlatformApi.Entities.Common;

namespace Bolt.Automation.TestDataProvider.TestData.PlatformAPITestData
{
    public record QuoteStatusRequestTestData
    {
        public static readonly string TransType = nameof(TransitionType.QuoteStatus);
        public static readonly string BOLTExternalId = "OK0W-8ATE-C429";
        public static string Org => nameof(Orgs.ProgressivePL);
        public static string ClientDt => DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        public static string SourceName = nameof(SourceNames.MPQ3);
        public static Guid RqUID => Guid.NewGuid();
    }
}
