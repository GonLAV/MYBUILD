using Bolt.Automation.ApiClients.AdbxApi.Entities.Policy;
using Refit;

namespace Bolt.Automation.ApiClients.AdbxApi.Interfaces
{
    public partial interface IAdbxPolicyApi
    {
        [Get("/policies/get-policies?take={take}&sortProperty={sortProperty}&sortOrder={sortOrder}&skip={skip}")]
        Task<ApiResponse<GetPoliciesResultModel>> GetPolicies(string sortProperty = "dateCreated", string sortOrder = "desc",
            int? take = 50, int? skip = 0);

        [Post("/policies/{policyId}/underwriterCases")]
        Task<ApiResponse<PolicyCaseCreateResultModel>> CreateUnderwriterCaseForPolicy(string policyId, [Body] UnderWriterPolicyCaseCreateModel request);
    }
}
