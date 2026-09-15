
namespace Bolt.Automation.ApiClients.AdbxApi.Entities.Case
{
    public record CaseModel
    {
        public Guid Id { get; set; }
        public DateTime? DueDate { get; set; }
        public string? Originated { get; set; }
        public string? SubCaseData { get; set; }
        public string? AddressTimeZone { get; set; }
        public string? ChangeReason { get; set; }
        public string? BusinessFlowPhase { get; set; }
        public string? BusinessFlowCollection { get; set; }
        public string? Product { get; set; }
        public string? ProductId { get; set; }
        public string? BusinessFlowStatus { get; set; }
        public string? BusinessName { get; set; }
        public bool IsSLAMet { get; set; }
        public string? CaseNumber { get; set; }
        public string? Severity { get; set; }
        public string? SubCaseType { get; set; }
        public string? SubType { get; set; }
        public string? CaseType { get; set; }
        public string? BusinessType { get; set; }
        public string? Status { get; set; }
        public virtual List<CaseAttachmentModel> Attachments { get; set; }
        public DateTime? DateCreated { get; set; }
        public DateTime? DateUpdated { get; set; }
        public bool IsActive { get; set; }
        public CaseAssignmentModel? ResourceAssignment { get; set; }
        public string? InsuredName { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? AddressLine1 { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? County { get; set; }
        public string? ZipCode { get; set; }
        public bool IsBoltAccessCase { get; set; }
        public bool HasPaymentRequest { get; set; }
        public bool? IsSubmitted { get; set; }
        public ChangeTypesData? ChangeTypesData { get; set; }
    }

    public class ChangeTypesData
    {
        public string ChangeType { get; set; }
        public Dictionary<string, string> Data { get; set; }
    }
}

