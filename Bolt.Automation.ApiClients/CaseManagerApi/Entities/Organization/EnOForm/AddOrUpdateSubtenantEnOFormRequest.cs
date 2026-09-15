
using Bolt.Automation.ApiClients.CaseManagerApi.Entities.Common;

namespace Bolt.Automation.ApiClients.CaseManagerApi.Entities.Organization.EnOForm
{
    public class AddOrUpdateSubtenantEnOFormRequest
    {
        public string Carrier { get; set; }
        public int Limit { get; set; }
        public DateTime ExpirationDate { get; set; }
        public FileModel File { get; set; }
        public bool? IsCreatedOnCM { get; set; }
    }
}
