using Bolt.Automation.ApiClients.Infrastructure;
using Bolt.Automation.ApiClients.PlatformApi.Entities.Quote.QuoteStart;
using Refit;

namespace Bolt.Automation.ApiClients.PlatformApi.Interfaces
{
    public partial interface IPlatformQuoteStartApi
    {
        [Post("/QuoteStart")]
        Task<ApiResponse<QuoteStartResponseModel>> QuoteStart([Body] QuoteStartRequestModel request);

        async Task<ApiResponse<QuoteStartResponseModel>> QuoteStartWithRetryAsync(QuoteStartRequestModel request) =>
            await RetryHelper.RetryAsync(() => QuoteStart(request));
    }
}
