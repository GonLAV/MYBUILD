using Bolt.Automation.ApiClients.CaseManagerApi.Entities.Policy.Create;

namespace Bolt.Automation.ApiClients.CaseManagerApi.Entities.Policy.Update
{
    public class UpdatePolicyRequest
    {
        public string PolicyExternalId { get; set; }
        public string Carrier { get; set; }
        public string Lob { get; set; }
        public DateTime EffectiveDate { get; set; }
        public DateTime ExpirationDate { get; set; }
        public double Premium { get; set; }
        public string PolicyNumber { get; set; }
        public List<PolicyFileModel> Files { get; set; }
        public string Status { get; set; }
        public string RenewedPolicyId { get; set; }
        public string ApptType { get; set; }
    }
}
