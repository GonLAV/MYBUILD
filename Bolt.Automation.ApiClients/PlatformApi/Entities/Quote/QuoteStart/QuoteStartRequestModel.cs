using Bolt.Automation.ApiClients.PlatformApi.Entities.Common;

namespace Bolt.Automation.ApiClients.PlatformApi.Entities.Quote.QuoteStart
{
    public record QuoteStartRequestModel : AcordBaseRequest
    {
        public string? ClientDt { get; set; }
        public Guid? CarrierUserAgentId { get; set; }
        public Guid? CarrierUserConsumerAgentId { get; set; }
        public string? SPName { get; set; }
        public string? ProxyClientName { get; set; }
        public string? ChannelIndicator { get; set; }
        public string? RedirectURL { get; set; }
        public string? LOBCd { get; set; }
        public string? PartnerPolicyNumber { get; set; }
        public AddressModel? PropertyAddr { get; set; }
        public AddressModel? MailingAddress { get; set; }
        public ApplicantDetails? ApplicantDetails { get; set; }
        public ApplicantDetails? CoApplicantDetails { get; set; }
        public Dictionary<string, string>? PrefillData { get; set; }
        public Dictionary<string, string>? ThirdPartyPrefillData { get; set; }
        public string? AgentExternalId { get; set; }
        public string? GroupExternalId { get; set; }
        public string? AccountExternalId { get; set; }
        public string? ConsumerLine { get; set; }
        public string? SourceType { get; set; }
    }
}
