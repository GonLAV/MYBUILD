namespace Bolt.Automation.ApiClients.GetQuoteApi.Models.Policy
{
    public class PolicyResponseModel
    {
        public string? Id { get; set; }
        public string? PolicyNumber { get; set; }
        public DateTime EffectiveDate { get; set; }
        public DateTime ExpirationDate { get; set; }
        public string? Carrier { get; set; }
        public string? Product { get; set; }
        public decimal Premium { get; set; }
    }
}
