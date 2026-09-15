using Bolt.Automation.ApiClients.CaseManagerApi.Entities.Deeplink;
using Refit;

namespace Bolt.Automation.ApiClients.CaseManagerApi.Interfaces
{
    public partial interface ICaseManagerDeepLinkApi
    {
        [Get("/{tenant}/quote-deeplinks/{quoteExternalId}")]
        Task<ApiResponse<QuoteDeeplinkResponse>> GetQuoteDeeplinkAsync([AliasAs("tenant")] string tenant, string quoteExternalId);

        [Get("/{tenant}/case-deeplinks/{caseExternalId}")]
        Task<ApiResponse<CaseDeeplinkResponse>> GetCaseDeeplinkAsync(
        [AliasAs("tenant")] string tenant, string caseExternalId, [Query] bool backendCase);

        [Get("/{tenant}/user-deeplinks/{userExternalId}")]
        Task<ApiResponse<UserDeeplinkResponse>> GetUserDeeplinkAsync([AliasAs("tenant")] string tenant, string userExternalId);

        [Get("/{tenant}/org-deeplinks/{organizationExternalId}")]
        Task<ApiResponse<OrganizationDeeplinkResponse>> GetOrganizationDeeplinkAsync([AliasAs("tenant")] string tenant, string organizationExternalId);

        [Get("/{tenant}/policy-url/{amsPolicyId}")]
        Task<ApiResponse<PolicyUrlResponse>> GetPolicyUrlAsync([AliasAs("tenant")] string tenant, string amsPolicyId);
    }
}
