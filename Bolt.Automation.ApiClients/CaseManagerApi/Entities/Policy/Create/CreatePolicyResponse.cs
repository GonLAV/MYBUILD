
namespace Bolt.Automation.ApiClients.CaseManagerApi.Entities.Policy.Create
{
    public class CreatePolicyResponse
    {
        public string PolicyExternalId { get; set; }
        public List<string> AttachmentsIds { get; set; }
        public string Error { get; set; }
    }
}
