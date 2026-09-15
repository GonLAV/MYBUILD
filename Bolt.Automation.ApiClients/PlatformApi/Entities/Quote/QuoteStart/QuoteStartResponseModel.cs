using Bolt.Automation.ApiClients.PlatformApi.Entities.Common;

namespace Bolt.Automation.ApiClients.PlatformApi.Entities.Quote.QuoteStart
{
    public class QuoteStartResponseModel : AcordBaseResponse
    {
        public string SPName { get; set; }
        public string Org { get; set; }
        public string MsgErrorCd { get; set; }
        public string ExtendedStatus { get; set; }
        public string WebsiteURL { get; set; }
        public Guid? QuoteId { get; set; }
    }
}
