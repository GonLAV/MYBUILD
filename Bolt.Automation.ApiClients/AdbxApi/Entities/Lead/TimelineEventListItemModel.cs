
namespace Bolt.Automation.ApiClients.AdbxApi.Entities.Lead
{
    public class TimelineEventListItemModel
    {
        public Guid Id { get; set; }
        public DateTime DateCreated { get; set; }
        public string? Subject { get; set; }
        public string? Description { get; set; }
        public bool IsAutomatic { get; set; }
        public Guid CreatorId { get; set; }
        public string? CreatorDisplayName { get; set; }
        public string? FriendlyId { get; set; }
        public List<TimelineAttachmentListItemModel>? Attachments { get; set; }
    }

    public class TimelineAttachmentListItemModel
    {
        public Guid Id { get; set; }
        public string? ExternalId { get; set; }
        public string? Name { get; set; }
        public int SizeBytes { get; set; }
        public string? Type { get; set; }
    }
}
