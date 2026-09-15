using Bolt.Automation.ApiClients.PlatformApi.Entities.Common;
using Bolt.Automation.ApiClients.PlatformApi.Entities.Quote.QuoteStart;

namespace Bolt.Automation.TestDataProvider.TestData.PlatformAPITestData.QuoteStartTestData
{
    public static partial class QuoteStartRequestTestData
    {
        public static string ClientDt => DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        public static string SPName => SPNames.BOLTAPI.ToString();
        public static readonly string ProxyClientName = DeviceIndicator.Desktop.ToString();
        public static string ChannelIndicator => ChannelIndicators.Direct.ToString();
        public static readonly string RedirectURL = "https://www.progressive.com/";
        public static readonly string LOBCd = Lobs.Home.ToString();
        public static readonly string TransType = TransitionType.QuoteStart.ToString();
        public static readonly string Org = Orgs.ProgressivePL.ToString();
        public static readonly string SourceName = "Progressive.com";

        public static readonly QuoteStartRequestModel DefaultQuoteStartRequest = new()
        {
            ClientDt = ClientDt,
            SPName = SPName,
            ProxyClientName = ProxyClientName,
            ChannelIndicator = ChannelIndicator,
            RedirectURL = RedirectURL,
            LOBCd = LOBCd,
            PropertyAddr = Addresses.Mapped_OH,
            ApplicantDetails = Applicants.Credipulse,
            PrefillData = new Dictionary<string, string>(Prefills.PrefillData),
            TransType = TransType,
            Org = Org,
            SourceName = SourceName
        };
    }
}
