using Bolt.Automation.ApiClients.PlatformApi.Entities.Common;

namespace Bolt.Automation.ApiClients.PlatformApi.Entities.Quote.QuoteDeepLink
{
    public record QuoteDeepLinkRequestModel : AcordBaseRequest
    {
        public string? BOLTExternalId { get; set; }
        public string? RedirectURL { get; set; }
        public string? ClientDt { get; set; }
    }
}
