using Bolt.Automation.ApiClients.GetQuoteApi.Models.Quote;

namespace Bolt.Automation.ApiClients.GetQuoteApi.Models.Application
{
    public class SubmissionResponseModel
    {
        public string? ApplicationId { get; set; }
        public string? FriendlyId { get; set; }
        public string? Status { get; set; }
        public IEnumerable<QuoteResponseModel>? Quotes { get; set; }
    }
}
