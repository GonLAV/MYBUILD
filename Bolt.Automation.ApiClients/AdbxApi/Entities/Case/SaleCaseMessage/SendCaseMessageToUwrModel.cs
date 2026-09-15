namespace Bolt.Automation.ApiClients.AdbxApi.Entities.Case.SaleCaseMessage
{
    public class SendCaseMessageToUwrModel
    {
        public string? Subject { get; set; }
        public string? Description { get; set; }
        public List<Guid>? AttachmentIds { get; set; }
    }
}
