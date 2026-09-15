using Bolt.Automation.ApiClients.GetQuoteApi.Models.Common;
using Bolt.Automation.Common.Models.TestData.Data;

namespace Bolt.Automation.ApiClients.GetQuoteApi.Models.Applicant
{
    public class GetApplicantResponseModel
    {
        public string? BusinessName { get; set; }
        public string? WebsiteAddress { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? ExternalId { get; set; }
        public DateTime? DateCreated { get; set; }
        public List<PhoneNumber>? PhoneNumbers { get; set; }
        public List<string>? Options { get; set; }
        public List<Id>? Ids { get; set; }
        public List<Address>? Addresses { get; set; }
    }
}
