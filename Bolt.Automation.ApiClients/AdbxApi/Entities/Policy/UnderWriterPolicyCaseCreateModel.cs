namespace Bolt.Automation.ApiClients.AdbxApi.Entities.Policy
{
    public class UnderWriterPolicyCaseCreateModel
    {
        public string CaseType { get; set; }
        public Guid? AssignTo { get; set; }
        public string Notes { get; set; }
        public string TimeZone { get; set; }
        public List<Guid> AttachmentIds { get; set; }
        public ChangeTypesData ChangeTypesData { get; set; }
    }

    public class ChangeTypesData
    {
        public string ChangeType { get; set; }
        public Dictionary<string, string> Data { get; set; }
    }
}
