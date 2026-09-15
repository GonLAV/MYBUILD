namespace Bolt.Automation.ApiClients.GetQuoteApi.Models.Policy
{
    public record PolicyRequestModel
    {
        public string? Carrier { get; set; }
        public string? LineOfBusiness { get; set; }
        public string? PolicyNumber { get; set; }
        public decimal Premium { get; set; }
        public DateTime? EffectiveStart { get; set; }
        public DateTime? EffectiveEnd { get; set; }
        public string? TransactionType { get; set; }
        public string? Appointment { get; set; }
        public string? BusinessType { get; set; }
        public Guid? ExternalId { get; set; }
        public string? PriorPolicyId { get; set; }
        public string? ApplicantId { get; set; }
        public string? ApplicationId { get; set; }
        public Guid? AccountId { get; set; }
        public bool? DontUpdateCrm { get; set; }
    }
}
