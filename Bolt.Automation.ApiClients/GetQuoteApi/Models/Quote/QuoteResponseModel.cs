using Bolt.Automation.ApiClients.GetQuoteApi.Models.Common;

namespace Bolt.Automation.ApiClients.GetQuoteApi.Models.Quote
{
    public class QuoteResponseModel
    {
        public string? Id { get; set; }
        public string? Carrier { get; set; }
        public string? Lob { get; set; }
        public string? Term { get; set; }
        public string? PackageType { get; set; }
        public bool? BridgeIndicator { get; set; }
        public decimal? Premium { get; set; }
        public string? Status { get; set; }
        public string? QuoteNumber { get; set; }
        public Dictionary<string, CoverageModel>? Coverages { get; set; }
        public IEnumerable<string>? Messages { get; set; }
        public string? PaymentMessage { get; set; }
        public List<QuotePaymentPlanResponseModel>? PaymentPlans { get; set; }
        public object? CarrierHighlights { get; set; }
    }
}
