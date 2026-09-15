
namespace Bolt.Automation.ApiClients.AdbxApi.Entities
{
    public class NoteListItemModel
    {
        public string? Id { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime DateUpdated { get; set; }
        public bool Private { get; set; }
        public string? NoteType { get; set; }
        public string? Subject { get; set; }
        public string? Description { get; set; }
        public string? SentTo { get; set; }

        public Guid CreatorId { get; set; }
        public string? CreatorDisplayName { get; set; }
        public Guid AssignedToId { get; set; }
        public string? AssignedToDisplayName { get; set; }
        public Guid OwnerId { get; set; }
        public string? OwnerDisplayName { get; set; }
        public List<NoteAttachmentListItemModel>? Attachments { get; set; }
        public List<NoteAttachmentMetaDataModel>? AttachmentsMetaData { get; set; }
        public int TotalCount { get; set; }
        public string? CaseExternalId { get; set; }
    }

    public class NoteAttachmentListItemModel
    {
        public Guid Id { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime DateUpdated { get; set; }
        public string? ExternalId { get; set; }
        public string? Name { get; set; }
        public int SizeBytes { get; set; }
        public string? Type { get; set; }
    }

    public class NoteAttachmentMetaDataModel
    {
        public Guid CaseMessageId { get; set; }
        public Guid CaseId { get; set; }
        public string? MessageId { get; set; }
        public string? FileId { get; set; }
        public string? Name { get; set; }
    }
}
