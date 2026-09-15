using Bolt.Automation.ApiClients.PlatformApi.Entities.Provision.BatchUserGroup;
using Refit;

namespace Bolt.Automation.ApiClients.PlatformApi.Interfaces
{
    public partial interface IPlatformGroupUserBatchApi
    {
        [Post("/GroupUserBatch")]
        Task<ApiResponse<BatchUserGroupResponse>> CreateUserGroupBatch([Body] BatchUserGroupRequestModel request);
    }
}
