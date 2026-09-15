using Bolt.Automation.ApiClients.Infrastructure;
using Bolt.Automation.ApiClients.PlatformApi.Entities.Common;
using Bolt.Automation.ApiClients.PlatformApi.Entities.Quote.QuoteStatus;
using Refit;

namespace Bolt.Automation.ApiClients.PlatformApi.Interfaces
{
    public partial interface IPlatformQuoteStatusApi
    {
        [Post("/QuoteStatus")]
        Task<ApiResponse<QuoteStatusResponseModel>> QuoteStatusAsync([Body] QuoteStatusRequestModel request);

        async Task<ApiResponse<QuoteStatusResponseModel>> QuoteStatusWithPollingAsync(
            QuoteStatusRequestModel request,
            QuoteStatus expectedStatus = QuoteStatus.Complete,
            QuoteSecondaryStatus? expectedSecondaryStatus = null,
            TimeSpan? timeout = null) =>
            await RetryHelper.RetryUntilAsync(
                () => QuoteStatusAsync(request),
                content => content.QuoteStatus == expectedStatus.ToString()
                    && (expectedSecondaryStatus == null
                        || content.SecondaryStatus == expectedSecondaryStatus.ToString()),
                timeout: timeout ?? TimeSpan.FromSeconds(100),
                label: $"QuoteStatus='{expectedStatus}'"
                    + (expectedSecondaryStatus == null ? "" : $" / SecondaryStatus='{expectedSecondaryStatus}'"));

        /// <summary>
        /// Polls /QuoteStatus until <paramref name="until"/> holds, returning the last status seen if it never does.
        /// </summary>
        /// <remarks>
        /// Prefer this when the status is under test: <see cref="QuoteStatusWithPollingAsync"/> only returns
        /// a response its predicate matched, so an assertion after it can never fail. Transport still throws.
        /// </remarks>
        async Task<QuoteStatusResponseModel> QuoteStatusUntilAsync(
            QuoteStatusRequestModel request,
            Func<QuoteStatusResponseModel, bool> until,
            int maxAttempts = 20,
            TimeSpan? delay = null) =>
            await RetryHelper.RetryAsync(
                () => QuoteStatusAsync(request).EnsureSuccessContentAsync("CheckQuoteStatus call failed"),
                status => until(status),
                maxAttempts: maxAttempts,
                delay: delay ?? TimeSpan.FromSeconds(5));
    }
}
