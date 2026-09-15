using Bolt.Automation.Common.Models.TestData.Data;

namespace Bolt.Automation.ApiClients.AdbxApi.Entities.Lead
{
    public class AccountInformationModel
    {
        public Guid AccountId { get; set; }
        public string? InsuredName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public Address? Address { get; set; }
        public DateTime DateCreated { get; set; }
        public string? AccountType { get; set; }
        public string? BusinessName { get; set; }
        public string? ExternalAccountId { get; set; }
        public string? Website { get; set; }
    }
}
