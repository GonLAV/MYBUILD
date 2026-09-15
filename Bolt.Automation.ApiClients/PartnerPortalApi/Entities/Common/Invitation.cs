
namespace Bolt.Automation.ApiClients.PartnerPortalApi.Entities.Common
{
    public class Invitation
    {
        public Guid Id { get; set; }
        public Guid PlatformQuoteId { get; set; }
        public Guid UserID { get; set; }
        public string? InviteStatus { get; set; }
        public DateTime DateCreated { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? Lob { get; set; }
        public int CurrentFlow { get; set; }
        public string? JourneyType { get; set; }
        public bool ShouldResend { get; set; }
        public string? LogoHeight { get; set; }
        public string? LogoWidth { get; set; }
        public bool SendAgentEmailCopy { get; set; }
        public string? AgentEmail { get; set; }
        public string? Referrer { get; set; }
        public Guid? LeadId { get; set; }
        public bool IsFromAdbx { get; set; }
        public string? QuoteNumber { get; set; }
        public string? Origin { get; set; }
        public string? PolicyIssuedDate { get; set; }
    }
}
