using Bolt.Automation.ApiClients.PlatformApi.Entities.CRMNote;
using Refit;

namespace Bolt.Automation.ApiClients.PlatformApi.Interfaces
{
    public partial interface IPlatformCRMNoteApi
    {
        [Post("/CRMNote")]
        Task<ApiResponse<CRMNoteResponseModel>> CRMNoteAsync([Body] CRMNoteRequestModel request);
    }
}
