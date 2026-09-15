namespace Bolt.Automation.ApiClients.AdbxApi.Entities.Quote
{
    public class QuoteModel
    {
        public Guid Id { get; set; }
        public Guid ConsumerId { get; set; }
        public string? ApplicationNumber { get; set; }
        public string? BusinessType { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime DateUpdated { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? MiddleName { get; set; }
        public DateTime? EffectiveDate { get; set; }
        public string? CurrentQuoteStage { get; set; }
        public string? ExternalId { get; set; }
        public string? Products { get; set; }
        public int QuoteType { get; set; }
        public int SourceMedia { get; set; }
        public string? SourceKeyword { get; set; }
        public string? SourceKeywordDisplayName { get; set; }
        public bool IsActive { get; set; }
        public string? PropertyAddressLine1 { get; set; }
        public string? PropertyAddressLine2 { get; set; }
        public string? PropertyCity { get; set; }
        public string? PropertyState { get; set; }
        public string? PropertyZipCode { get; set; }
        public string? ForeignAddressLine1 { get; set; }
        public string? ForeignAddressLine2 { get; set; }
        public string? ForeignCity { get; set; }
        public string? ForeignCountry { get; set; }
        public string? ForeignPostalCode { get; set; }

        public Guid CreatorId { get; set; }
        public string? CreatorDisplayName { get; set; }
        public Guid AssignedToId { get; set; }
        public string? AssignedToDisplayName { get; set; }
        public Guid OwnerId { get; set; }
        public string? OwnerDisplayName { get; set; }

        public RelatedEntityInfo? Lead { get; set; }
    }

    public class RelatedEntityInfo
    {
        public Guid Id { get; set; }
        public string? Number { get; set; }
    }
}
