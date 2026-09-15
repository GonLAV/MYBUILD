namespace Bolt.Automation.ApiClients.AdbxApi.Entities.Quote
{
    public class QuoteTimelineResponseModel
    {
        public string? ParentId { get; set; }

        public List<TimelineEventModel>? Events { get; set; }
    }

    public class TimelineEventModel
    {
        public string? Id { get; set; }

        public DateTime? DateCreated { get; set; }

        public string? Subject { get; set; }

        public string? Description { get; set; }

        public bool IsAutomatic { get; set; }

        public string? CreatorId { get; set; }

        public string? CreatorDisplayName { get; set; }

        public string? AdditionalData { get; set; }

        public List<object>? Attachments { get; set; }
    }
}