using Bolt.OdmEntities.Entities.Payment;

namespace Bolt.Automation.ApiClients.GetQuoteApi.Models.Quote
{
    public class QuotePaymentPlanResponseModel : PaymentPlan
    {
        public string? Url { get; set; }
        public string? TransactionId { get; set; }
        public string? Id { get; set; }
        public bool Selected { get; set; }
    }
}
