
namespace Bolt.Automation.ApiClients.CaseManagerApi.Entities.Policy.Update
{
    public class UpdatePolicyResponse
    {
        public string PolicyExternalId { get; set; }
        public List<string> AttachmentsIds { get; set; }
        public string Error { get; set; }

    }
}
