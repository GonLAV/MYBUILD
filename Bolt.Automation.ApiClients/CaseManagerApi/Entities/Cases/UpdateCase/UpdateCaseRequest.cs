
namespace Bolt.Automation.ApiClients.CaseManagerApi.Entities.Cases.UpdateCase
{
    public class UpdateCaseRequest
    {
        public string CaseExternalId { get; set; }
        public string CmCaseId { get; set; }
        public string CaseStatus { get; set; }
        public string CaseWorkflow { get; set; }
    }
}
