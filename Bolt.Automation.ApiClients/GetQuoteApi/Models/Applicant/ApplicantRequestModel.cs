using Bolt.Automation.ApiClients.GetQuoteApi.Models.Common;
using Bolt.Automation.Common.Models.TestData.Data;

namespace Bolt.Automation.ApiClients.GetQuoteApi.Models.Applicant
{
    public record ApplicantRequestModel
    {
        public string? BusinessName { get; set; }
        public string? WebsiteAddress { get; set; }
        public string? FirstName { get; set; }
        public string? MiddleName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? ExternalId { get; set; }
        public bool? AgreeToReceiveEmailsByBolt { get; set; }
        public DateTime? DateCreated { get; set; }
        public List<PhoneNumber>? PhoneNumbers { get; set; }
        public List<string>? Options { get; set; }
        public List<Id>? Ids { get; set; }
        public List<Address>? Addresses { get; set; }
        public string? SourceMedia { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? PartnerPolicyNumber { get; set; }
        public List<string>? Optins { get; set; }
    }
}
