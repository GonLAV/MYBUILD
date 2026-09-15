namespace Bolt.Automation.ApiClients.CaseManagerApi.Entities.Common
{
    public class MessageListModel
    {
        public List<MessageListItemModel> Messages { get; set; }

        public MessageListModel()
        {
            Messages = new List<MessageListItemModel>();
        }
    }
}
