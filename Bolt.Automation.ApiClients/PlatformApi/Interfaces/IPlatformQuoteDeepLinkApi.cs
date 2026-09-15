using Bolt.Automation.ApiClients.PlatformApi.Entities.Quote.QuoteDeepLink;
using Refit;

namespace Bolt.Automation.ApiClients.PlatformApi.Interfaces
{
    public partial interface IPlatformQuoteDeepLinkApi
    {
        [Post("/DeepLink")]
        Task<ApiResponse<QuoteDeepLinkResponseModel>> DeeplinkAsync([Body] QuoteDeepLinkRequestModel request);
    }
}
