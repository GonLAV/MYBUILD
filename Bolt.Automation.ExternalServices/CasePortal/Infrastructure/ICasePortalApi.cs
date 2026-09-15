using Bolt.Automation.ExternalServices.CasePortal.Entities;
using Refit;

namespace Bolt.Automation.ExternalServices.CasePortal.Infrastructure
{
    public interface ICasePortalApi
    {
        [Get("/Message/GetAllMessagesByCaseId")]
        Task<ApiResponse<GetAllMessagesByCaseIdResponse>> GetAllMessagesByCaseIdAsync(
            [AliasAs("partner")] string partner,
            [AliasAs("caseId")] string caseId,
            [AliasAs("userId")] string userId);

    }
}
