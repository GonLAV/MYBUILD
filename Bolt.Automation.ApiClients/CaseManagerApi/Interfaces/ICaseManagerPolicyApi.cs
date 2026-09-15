using Bolt.Automation.ApiClients.CaseManagerApi.Entities.Policy.Create;
using Bolt.Automation.ApiClients.CaseManagerApi.Entities.Policy.Update;
using Refit;

namespace Bolt.Automation.ApiClients.CaseManagerApi.Interfaces
{
    public partial interface ICaseManagerPolicyApi
    {
        [Post("/{tenant}/policies")]
        Task<ApiResponse<CreatePolicyResponse>> CreatePolicyAsync([AliasAs("tenant")] string tenant, [Body] CreatePolicyRequest request);

        [Put("/{tenant}/policies")]
        Task<ApiResponse<UpdatePolicyResponse>> UpdatePolicyAsync([AliasAs("tenant")] string tenant, [Body] UpdatePolicyRequest request);
    }
}
