using Bolt.Automation.ApiClients.PlatformApi.Entities.Common;

namespace Bolt.Automation.ApiClients.PlatformApi.Entities.Quote.QuoteRetrieve
{
    public class QuoteRetriveResponseModel : AcordBaseResponse
    {
        public string Status { get; set; }
        public string URL { get; set; }
    }
}
