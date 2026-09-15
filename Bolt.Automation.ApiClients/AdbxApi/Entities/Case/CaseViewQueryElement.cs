namespace Bolt.Automation.ApiClients.AdbxApi.Entities.Case
{
    public class CaseViewQueryElement
    {
        public string? FieldType { get; set; }
        public string? Operator { get; set; }
        public object? Value { get; set; }
        public string? Type { get; set; }
        public string? OrderDirection { get; set; }
    }
}
