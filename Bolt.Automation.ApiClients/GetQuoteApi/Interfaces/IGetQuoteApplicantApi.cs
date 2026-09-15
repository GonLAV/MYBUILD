using Bolt.Automation.ApiClients.GetQuoteApi.Models.Applicant;
using Bolt.Automation.ApiClients.Infrastructure;
using Refit;

namespace Bolt.Automation.ApiClients.GetQuoteApi.Interfaces
{
    public partial interface IGetQuoteApplicantApi
    {
        [Get("/applicants/{applicantId}")]
        Task<ApiResponse<GetApplicantResponseModel>> GetApplicantAsync(string applicantId);

        async Task<ApiResponse<GetApplicantResponseModel>> GetApplicantWithRetryAsync(string applicantId) =>
            await RetryHelper.RetryAsync(() => GetApplicantAsync(applicantId));

        [Post("/applicants")]
        Task<ApiResponse<PostApplicantResponseModel>> CreateApplicantAsync([Body] ApplicantRequestModel request);

        async Task<ApiResponse<PostApplicantResponseModel>> CreateApplicantWithRetryAsync(ApplicantRequestModel applicant, Predicate<ApiResponse<PostApplicantResponseModel>> predicate) =>
            await RetryHelper.RetryAsync(() => CreateApplicantAsync(applicant), predicate);

        [Put("/applicants/{applicantId}")]
        Task<ApiResponse<GetApplicantResponseModel>> UpdateApplicantAsync(
            string applicantId,
            [Body] ApplicantRequestModel request
        );

        [Put("/applicants/{applicantId}/applications/{applicationId}")]
        Task<ApiResponse<PostApplicantResponseModel>> ConnectApplicantToApplicationAsync(
            string applicantId,
            string applicationId
        );
    }
}
