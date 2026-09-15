
namespace Bolt.Automation.ApiClients.AdbxApi.Entities.Lead
{
    public class CommunicationListItemModel
    {
        public string? CommunicationType { get; set; }
        public string? FriendlyId { get; set; }
        public Guid? CaseId { get; set; }
        public string? CaseNumber { get; set; }
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
        public List<CommunicationAttachmentListItemModel>? Attachments { get; set; }
        public List<CommunicationAttachmentMetaDataModel>? AttachmentsMetaData { get; set; }
        public List<string>? Messages { get; set; }
        public List<string>? Channels { get; set; }
        public bool ReplyDisabled { get; set; }
    }
    public class CommunicationAttachmentListItemModel
    {
        public Guid Id { get; set; }
        public string? ExternalId { get; set; }
        public string? Name { get; set; }
        public int SizeBytes { get; set; }
        public string? Type { get; set; }
    }

    public class CommunicationAttachmentMetaDataModel
    {
        public Guid CaseMessageId { get; set; }
        public Guid CaseId { get; set; }
        public string? MessageId { get; set; }
        public string? FileId { get; set; }
        public string? Name { get; set; }
    }
}
