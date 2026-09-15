
namespace Bolt.Automation.ApiClients.AdbxApi.Entities.Case
{
    public class UserCaseQueueModel
    {
        public List<CaseQueueModel> PinnedQueues { get; set; }
        public List<CaseQueueModel> UnpinnedQueues { get; set; }
    }

    public class CaseQueueModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public short Order { get; set; }
        public int Count { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public bool IsEditable { get; set; }
    }
}
