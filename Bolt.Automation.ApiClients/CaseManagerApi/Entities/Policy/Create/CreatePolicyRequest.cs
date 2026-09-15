using Bolt.Automation.ApiClients.CaseManagerApi.Entities.Common;

namespace Bolt.Automation.ApiClients.CaseManagerApi.Entities.Policy.Create
{
    public class CreatePolicyRequest
    {
        public string AccountExternalId { get; set; }
        public string QuoteExternalId { get; set; }
        public string Carrier { get; set; }
        public string Lob { get; set; }
        public DateTime EffectiveDate { get; set; }
        public DateTime ExpirationDate { get; set; }
        public double Premium { get; set; }
        public string PolicyNumber { get; set; }
        public List<PolicyFileModel> Files { get; set; }
        public DateTime? DateCreated { get; set; }
        public DateTime? DateUpdated { get; set; }
        public string ApptType { get; set; }
        public bool? RenewalPolicy { get; set; }
        public string Status { get; set; }
        public string RenewedPolicyId { get; set; }
    }

    public class PolicyFileModel : FileModel
    {
        public string Description { get; set; }
        public string Subject { get; set; }
        public string NoteType { get; set; }
        public DateTime? DateCreated { get; set; }
        public DateTime? DateUpdated { get; set; }
    }
}
