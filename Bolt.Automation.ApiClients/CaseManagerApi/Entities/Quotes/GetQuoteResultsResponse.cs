
namespace Bolt.Automation.ApiClients.CaseManagerApi.Entities.Quotes
{
    public class GetQuoteResultsResponse
    {
        public string QuoteExternalId { get; set; }
        public List<QuoteResultListItemModel> Results { get; set; }
        public GetQuoteResultsResponse()
        {
            Results = new List<QuoteResultListItemModel>();
        }
    }

    public class QuoteResultListItemModel
    {
        public string QuoteResultExternalId { get; set; }
        public string QuoteNumber { get; set; }
        public string Lob { get; set; }
        public string Carrier { get; set; }
        public decimal Premium { get; set; }
        public string Logo { get; set; }
        public string BridgeUrl { get; set; }
    }
}
