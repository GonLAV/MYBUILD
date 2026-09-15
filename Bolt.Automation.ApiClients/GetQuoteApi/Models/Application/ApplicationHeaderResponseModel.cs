namespace Bolt.Automation.ApiClients.GetQuoteApi.Models.Application
{
    public class ApplicationHeader
    {
        public string? QuoteExternalId { get; set; }
        public Guid QuoteId { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime DateModified { get; set; }
        public string? Status { get; set; }
        public string? QuoteSourceKeyword { get; set; }
        public string? FirstName { get; set; }
        public string? MiddleName { get; set; }
        public string? LastName { get; set; }
        public string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? ZipCode { get; set; }
        public string? LineofBusiness { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? QuoteFriendlyId { get; set; }
        public decimal? PreferredCarrierPremium { get; set; }
        public bool LockedByAgent { get; set; }
    }
}
