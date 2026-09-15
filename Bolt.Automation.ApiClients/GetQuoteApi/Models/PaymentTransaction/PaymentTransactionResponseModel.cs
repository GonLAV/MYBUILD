namespace Bolt.Automation.ApiClients.GetQuoteApi.Models.PaymentTransaction
{
    public class PaymentTransactionResponseModel
    {
        public string? Status { get; set; }
        public decimal Amount { get; set; }
        public Dictionary<string, string>? AdditionalData { get; set; }
    }
}
