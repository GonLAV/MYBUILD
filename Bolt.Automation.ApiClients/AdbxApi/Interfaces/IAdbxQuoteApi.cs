using Bolt.Automation.ApiClients.AdbxApi.Entities.Quote;
using Refit;

namespace Bolt.Automation.ApiClients.AdbxApi.Interfaces
{
    public partial interface IAdbxQuoteApi
    {
        [Get("/quotes/{quoteId}")]
        Task<ApiResponse<QuoteModel>> GetQuoteByQuoteId(string quoteId);

        [Get("/quotes/?take={take}&sortProperty={sortProperty}&sortOrder={sortOrder}&skip={skip}")]
        Task<ApiResponse<IEnumerable<QuoteListItemModel>>> GetQuotes(string sortProperty = "dateCreated", string sortOrder = "desc",
            int? take = 10, int? skip = 0);

        [Get("/quotes/{quoteId}/timeline")]
        Task<ApiResponse<QuoteTimelineResponseModel>> GetQuoteTimeline(string quoteId);

        [Get("/quotes/{quoteId}/policies")]
        Task<ApiResponse<IEnumerable<QuotePolicyListItemModel>>> GetQuotePolicies(string quoteId);

    }
}
