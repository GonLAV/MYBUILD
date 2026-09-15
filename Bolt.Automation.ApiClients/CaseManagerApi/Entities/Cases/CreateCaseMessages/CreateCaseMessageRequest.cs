using Bolt.Automation.ApiClients.CaseManagerApi.Entities.Common;

namespace Bolt.Automation.ApiClients.CaseManagerApi.Entities.Cases.CreateCaseMessages
{
    public class CreateCaseMessageRequest :MessageModel
    {
        public List<FileModel> Files { get; set; }

        public CreateCaseMessageRequest()
        {
            Files = new List<FileModel>();
        }
    }
}
