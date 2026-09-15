
namespace Bolt.Automation.ApiClients.PartnerPortalApi.Entities.Invite
{
    public class SendInviteResponse
    {
        public string Message { get; set; }
        public bool IsSuccess { get; set; }
        public string RedirectToPage { get; set; }
        public Guid InviteId { get; set; }
    }
}
