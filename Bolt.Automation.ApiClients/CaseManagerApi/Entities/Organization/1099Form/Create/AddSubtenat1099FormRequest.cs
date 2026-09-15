using Bolt.Automation.ApiClients.CaseManagerApi.Entities.Common;

namespace Bolt.Automation.ApiClients.CaseManagerApi.Entities.Organization._1099Form.Create
{
    public class AddSubtenat1099FormRequest
    {
        public FileModel File { get; set; }
        public string ActionType { get; set; }
        public string AttachmentExternalId { get; set; }
        public DateTime? DateCreated { get; set; }
    }
}
