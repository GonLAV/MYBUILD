
namespace Bolt.Automation.ApiClients.AdbxApi.Entities.Account
{
    public class GetAccountResponse
    {
        public Guid Id { get; set; }
        public DateTime? DateCreated { get; set; }
        public DateTime? DateUpdated { get; set; }
        public bool IsActive { get; set; }
        public string? Tenant { get; set; }
        public string? AccountName { get; set; }
        public string? ContactName { get; set; }
        public string? FirstName { get; set; }
        public string? MiddleName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? PrimaryPhoneNumber { get; set; }
        public string? BusinessPhoneNumber { get; set; }
        public string? BusinessName { get; set; }
        public string? BusinessType { get; set; }
        public string? Website { get; set; }
        public string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string? ZipCode { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? County { get; set; }
        public string? Country { get; set; }
        public string? PropertyZipCode { get; set; }
        public string? PropertyAddressLine1 { get; set; }
        public string? PropertyAddressLine2 { get; set; }

        public string? CrmId { get; set; }
        public string? ExternalId { get; set; }
        public string? Source { get; set; }
        public string? IdType { get; set; }
        public string? IdValue { get; set; }
        public DateTime? AccountInfoDOB { get; set; }
        public short SourceMedia { get; set; }
        public short Status { get; set; }
    }
}
