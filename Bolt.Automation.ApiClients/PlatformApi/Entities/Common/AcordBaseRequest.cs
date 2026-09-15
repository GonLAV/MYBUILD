namespace Bolt.Automation.ApiClients.PlatformApi.Entities.Common
{
    public record AcordBaseRequest
    {
        public string TransType { get; set; }
        public string Org { get; set; }
        public Guid RqUID { get; set; } = Guid.NewGuid();
        public string SourceName { get; set; }
    }
}
