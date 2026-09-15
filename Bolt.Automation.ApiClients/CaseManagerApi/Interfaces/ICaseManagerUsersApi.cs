using Bolt.Automation.ApiClients.CaseManagerApi.Entities.Users;
using Refit;

namespace Bolt.Automation.ApiClients.CaseManagerApi.Interfaces
{
    public partial interface ICaseManagerUsersApi
    {
        [Post("/{tenant}/users")]
        Task<ApiResponse<CreateUpdateUserResponse>> CreateUpdateUserAsync([AliasAs("tenant")] string tenant, [Body] CreateUpdateUserRequest request);
    }
}
