using Bolt.Automation.ApiClients.PlatformApi.Entities.Quote.QuoteRetrieve;
using Refit;

namespace Bolt.Automation.ApiClients.PlatformApi.Interfaces
{
    public partial interface IPlatformQuoteRetrievalApi
    {
        [Post("/QuoteRetrieval")]
        Task<ApiResponse<QuoteRetriveResponseModel>> QuoteRetrievalAsync([Body] RetrievalRequestWrapperModel request);
    }
}
