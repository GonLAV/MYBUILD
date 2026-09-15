using Bolt.Automation.ApiClients.GetQuoteApi.Models.Common;

namespace Bolt.Automation.ApiClients.GetQuoteApi.Models.Application
{
    public class ApplicationCreateContactRequestModel
    {
        public string? CaseType { get; set; }

        public string? DueDate { get; set; }

        public string? Severity { get; set; }

        public string? AssignedTo { get; set; }

        public ApplicantContactDetails? ApplicantData { get; set; }
    }
}
