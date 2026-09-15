using Bolt.Automation.ApiClients.PartnerPortalApi.Entities.Common;
using Bolt.Automation.ApiClients.PartnerPortalApi.Entities.Progress;
using Refit;

namespace Bolt.Automation.ApiClients.PartnerPortalApi.Interfaces
{
    public partial interface IPartnerPortalProgressApi
    {
        [Get("/progress")]
        Task<ApiResponse<GetProgressPaginationResponse>> GetInvitationDetailsByAgentAsync([Query(CollectionFormat.Multi)] GetItemsPaginationRequest request);
    }
}
