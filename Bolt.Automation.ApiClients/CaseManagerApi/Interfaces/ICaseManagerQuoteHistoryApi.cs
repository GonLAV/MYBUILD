using Bolt.Automation.ApiClients.CaseManagerApi.Entities.QuoteHistory;
using Refit;

namespace Bolt.Automation.ApiClients.CaseManagerApi.Interfaces
{
    public partial interface ICaseManagerQuoteHistoryApi
    {
        [Get("/{tenant}/quote-history/{caseExternalId}")]
        Task<ApiResponse<GetTransactionsResponse>> GetTransactionsAsync([AliasAs("tenant")] string tenant, string caseExternalId);

        [Get("/{tenant}/quote-history/{caseExternalId}/transaction-data/{Id}")]
        Task<ApiResponse<GetTransactionDataResponse>> GetTransactionDataAsync([AliasAs("tenant")] string tenant, Guid Id, string caseExternalId);
    }
}
