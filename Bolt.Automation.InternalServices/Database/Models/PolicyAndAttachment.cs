namespace Bolt.Automation.InternalServices.Database.Models;

public class PolicyAndAttachmentData
{
    // Policy properties
    public Guid Id { get; set; }
    public string? FriendlyId { get; set; }
    public string? ExternalId { get; set; }
    public DateTime DateCreated { get; set; }
    public Guid ConsumerId { get; set; }
    public short CurrentStage { get; set; }
    public short LastVisitedStage { get; set; }
    public string? PolicyData { get; set; }

    // PolicyAttachment properties
    public Guid AttachmentId { get; set; }
    public Guid PolicyId { get; set; }
    public DateTime AttachmentDateCreated { get; set; }
    public bool? IsActive { get; set; }
    public DateTime? DateUpdated { get; set; }
    public string? Content { get; set; }
    public short? AttachmentType { get; set; }

    
}