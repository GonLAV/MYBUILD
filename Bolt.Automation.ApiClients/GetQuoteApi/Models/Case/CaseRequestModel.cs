namespace Bolt.Automation.ApiClients.GetQuoteApi.Models.Case
{
    public record CaseRequestModel
    {
        public string? CaseType { get; set; }
        public string? DueDate { get; set; }
        public string? Severity { get; set; }
    }
}
