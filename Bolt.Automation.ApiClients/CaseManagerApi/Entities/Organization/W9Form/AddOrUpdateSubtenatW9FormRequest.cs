using Bolt.Automation.ApiClients.CaseManagerApi.Entities.Common;
using Bolt.Automation.Common.Models.TestData.Data;

namespace Bolt.Automation.ApiClients.CaseManagerApi.Entities.Organization.W9Form
{
    public class AddOrUpdateSubtenatW9FormRequest
    {
        public string? BusinessType { get; set; }
        public string? SSN { get; set; }
        public string? BusinessName { get; set; }
        public string? AgencyDbaName { get; set; }
        public Address? MailingAddress { get; set; }
        public FileModel? File { get; set; }
        public DateTime? DateCreated { get; set; }
    }
}
