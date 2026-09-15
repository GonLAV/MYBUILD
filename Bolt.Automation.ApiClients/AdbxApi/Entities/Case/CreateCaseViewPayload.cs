namespace Bolt.Automation.ApiClients.AdbxApi.Entities.Case
{
    public class CreateCaseViewPayload
    {
        public string? Referred { get; set; }
        public IEnumerable<CaseViewQueryElement>? Clauses { get; set; }
        public string? SaveQueryFor { get; set; }
    }
}
