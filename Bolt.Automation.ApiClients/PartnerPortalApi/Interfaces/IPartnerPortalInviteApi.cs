using Bolt.Automation.ApiClients.PartnerPortalApi.Entities.Common;
using Bolt.Automation.ApiClients.PartnerPortalApi.Entities.Invite;
using Refit;

namespace Bolt.Automation.ApiClients.PartnerPortalApi.Interfaces
{
    public partial interface IPartnerPortalInviteApi
    {
        [Post("/invite/sendinvite")]
        Task<ApiResponse<SendInviteResponse>> SendInviteAsync([Body] SendInviteRequest request);

        [Post("/invite/reSendinvite")]
        Task<ApiResponse<SendInviteResponse>> ReSendInviteAsync([Body] Invitation request);

        [Get("/invite/getInvitations")]
        Task<ApiResponse<GetInvitationDetailsResponse>> GetInvitationDetailsAsync();

        [Get("/invite/getJourneys")]
        Task<ApiResponse<List<JourneyDto>>> GetJourneysAsync();
    }
}
