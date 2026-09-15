using Bolt.Automation.ApiClients.CaseManagerApi.Entities.Quotes;
using Refit;

namespace Bolt.Automation.ApiClients.CaseManagerApi.Interfaces
{
    public partial interface ICaseManagerQuotesApi
    {
        [Get("/{tenant}/quotes/{externalId}/results")]
        Task<ApiResponse<GetQuoteResultsResponse>> GetQuoteResultsAsync([AliasAs("tenant")] string tenant, string externalId);
    }
}
