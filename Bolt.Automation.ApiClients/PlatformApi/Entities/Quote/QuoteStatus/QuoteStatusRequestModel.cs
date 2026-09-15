using Bolt.Automation.ApiClients.PlatformApi.Entities.Common;

namespace Bolt.Automation.ApiClients.PlatformApi.Entities.Quote.QuoteStatus
{
    public record QuoteStatusRequestModel : AcordBaseRequest
    {
        public string? BOLTExternalId { get; set; }
        public string? ClientDt { get; set; }
    }
}