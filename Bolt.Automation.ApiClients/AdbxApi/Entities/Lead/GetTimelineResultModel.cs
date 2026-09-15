
namespace Bolt.Automation.ApiClients.AdbxApi.Entities.Lead
{
    public class GetTimelineResultModel
    {
        public Guid ParentId { get; set; }
        public List<TimelineEventListItemModel>? Events { get; set; }
    }
}
