
namespace Bolt.Automation.ApiClients.AdbxApi.Entities.Case
{
    public record CaseAttachmentModel
    {
        public Guid Id { get; set; }
        public string? ExternalId { get; set; }
        public string? Name { get; set; }
        public int SizeBytes { get; set; }
        public string? Type { get; set; }

        public DateTime? DateCreated { get; set; }
        public DateTime? DateUpdated { get; set; }
        public bool IsActive { get; set; }
    }
}
