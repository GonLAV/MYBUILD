namespace Bolt.Automation.ApiClients.AdbxApi.Entities.Quote
{
    public class QuotePolicyListItemModel
    {
        public Guid Id { get; set; }
        public string? PolicyNumber { get; set; }
        public string? Carrier { get; set; }
        public decimal Premium { get; set; }
        public DateTime? EffectiveDate { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public string? Product { get; set; }
        public string? Status { get; set; }
        public string? Source { get; set; }
        public DateTime DateCreated { get; set; }
        public string? Appointment { get; set; }
    }
}
