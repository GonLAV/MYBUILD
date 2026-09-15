namespace Bolt.Automation.ApiClients.AdbxApi.Entities.Quote
{
    public class QuoteListItemModel
    {
        public string? Id { get; set; }
        public string? ApplicationNumber { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime DateUpdated { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? PropertyState { get; set; }
        public string? PropertyCity { get; set; }
        public string? PropertyZipcode { get; set; }
        public string? PropertyCounty { get; set; }
        public string? PropertyAddressline1 { get; set; }
        public string? ForeignCountry { get; set; }
        public string? Products { get; set; }
        public DateTime? EffectiveDate { get; set; }
        public string? CurrentQuoteStage { get; set; }
        public int QuoteType { get; set; }
        public string? QuoteTypeName { get; set; }
        public int SourceMedia { get; set; }
        public string? SourceMediaName { get; set; }
        public string? SourceKeyword { get; set; }
        public string? SourceKeywordDisplayName { get; set; }
        public string? Status { get; set; }
        public int TotalCount { get; set; }
        public string? BusinessName { get; set; }
    }
}
