using Bolt.Automation.Common.Models.TestData.Data;

namespace Bolt.Automation.ApiClients.GetQuoteApi.Models.Common
{
    public class ApplicantContactDetails
    {
        public string? BusinessName { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public List<PhoneNumber>? PhoneNumbers { get; set; }
        public List<Address>? Addresses { get; set; }
    }
}
