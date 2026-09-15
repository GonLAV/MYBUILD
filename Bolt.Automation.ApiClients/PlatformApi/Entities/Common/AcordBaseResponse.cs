namespace Bolt.Automation.ApiClients.PlatformApi.Entities.Common
{
    public class AcordBaseResponse
    {
        public Guid RqUid { get; set; }
        public string BoltExternalId { get; set; }
        public string MsgStatusCd { get; set; }
        public List<Error> Errors { get; set; }
        public string Org { get; set; }
        public string CorrelationId { get; set; }
    }
}
