
namespace Bolt.Automation.ApiClients.PartnerPortalApi.Entities.Invite
{
    public class GetInvitationDetailsResponse
    {
        public Guid UserID { get; set; }
        public Guid GroupId { get; set; }
        public int TotalInviteSent { get; set; }
        public int TotalQuoted { get; set; }
        public int TotalIssued { get; set; }
    }
}
