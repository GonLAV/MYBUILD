
namespace Bolt.Automation.ApiClients.AdbxApi.Entities.Lead
{
    public class LeadQuoteListItemModel
    {
        public Guid QuoteId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Product { get; set; }
        public string? State { get; set; }
        public string? Addressline1 { get; set; }
        public string? City { get; set; }
        public string? County { get; set; }
        public string? ZipCode { get; set; }
        public DateTime? EffectiveDate { get; set; }
        public string? ApplicationNumber { get; set; }
        public string? Status { get; set; }
        public DateTime? DateCreated { get; set; }
    }
}
