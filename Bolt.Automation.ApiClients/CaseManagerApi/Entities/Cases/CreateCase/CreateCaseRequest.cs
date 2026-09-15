namespace Bolt.Automation.ApiClients.CaseManagerApi.Entities.Cases.CreateCase
{
    public class CreateCaseRequest
    {
        public string? PolicyExternalId { get; set; }
        public string? CaseType { get; set; }
        public string? Originated { get; set; }
        public string? CmCaseId { get; set; }
        public string? AccountExternalId { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
        public string? Lob { get; set; }
        public string? ProducerExternalId { get; set; }
    }
}
