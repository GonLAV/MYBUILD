using Bolt.Automation.ApiClients.PlatformApi.Entities.Common;

namespace Bolt.Automation.ApiClients.PlatformApi.Entities.Quote.QuoteStatus
{
    public class QuoteStatusResponseModel : AcordBaseResponse
    {
        public string QuoteStatus { get; set; }
        public string SecondaryStatus { get; set; }
        public int VerifyStatus { get; set; }
        public string WebsiteURL { get; set; }
    }
}
