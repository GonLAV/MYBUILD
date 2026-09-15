
namespace Bolt.Automation.ApiClients.CaseManagerApi.Entities.Common
{
    public class MessageModel
    {
        public string UserDetails { get; set; }
        public DateTime DateCreated { get; set; }
        public string MessageAudience { get; set; }
        public NoteModel Note { get; set; }
        public string MessageDirection { get; set; }
    }
}
