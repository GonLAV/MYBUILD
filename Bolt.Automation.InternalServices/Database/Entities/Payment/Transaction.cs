namespace Bolt.Automation.InternalServices.Database.Entities.Payment
{
    public class Transaction
    {
        public Guid Id { get; set; }
        public string Type { get; set; } = null!;
        public string ExternalId { get; set; } = null!;
        public string? Status { get; set; }
        public string? StatusDescription { get; set; }
        public Guid EntityId { get; set; }
        public string? EntityType { get; set; }
        public string? EntityBusinessType { get; set; }
        public decimal? Amount { get; set; }
        public string? CurrencyCode { get; set; }
        public string SourceKeyword { get; set; } = null!;
        public DateTime DateCreated { get; set; }
        public DateTime? DateUpdated { get; set; }
        public Guid CreatedBy { get; set; }
        public string? FlowMetadata { get; set; } // XML stored as string
    }
}