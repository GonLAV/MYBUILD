namespace Bolt.Automation.ApiClients.AdbxApi.Entities.Lead
{
    public class LeadPolicyListItemModel
    {
        public Guid Id { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime DateUpdated { get; set; }
        public DateTime EffectiveDate { get; set; }
        public DateTime ExpirationDate { get; set; }
        public string? PolicyNumber { get; set; }
        public string? Carrier { get; set; }
        public string? Premium { get; set; }
        public string? Product { get; set; }
        public string? Status { get; set; }
        public string? Source { get; set; }
        public int TotalCount { get; set; }
        public string? Appointment { get; set; }
    }
}
