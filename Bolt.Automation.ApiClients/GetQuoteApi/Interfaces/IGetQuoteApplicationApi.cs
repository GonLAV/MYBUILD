using Bolt.Automation.ApiClients.GetQuoteApi.Models.Application;
using Bolt.Automation.ApiClients.GetQuoteApi.Models.Common;
using Bolt.Automation.ApiClients.GetQuoteApi.Models.Quote;
using Bolt.Automation.ApiClients.Infrastructure;
using Refit;

namespace Bolt.Automation.ApiClients.GetQuoteApi.Interfaces
{
    public partial interface IGetQuoteApplicationApi
    {
        [Get("/applications/{applicationId}/submission")]
        Task<ApiResponse<SubmissionResponseModel>> GetSubmissionAsync(string applicationId);

        /// <summary>
        /// Polls the submission until the application's roll-up status reaches "Completed",
        /// i.e. every solicited carrier has come back (a carrier that declines or errors is
        /// still "come back" - the roll-up only waits on the ones still rating).
        /// The default window has to cover the slowest carrier in the appetite, not the fastest:
        /// a multi-carrier home appetite on QA/UAT routinely needs well over two minutes, and
        /// polling out early reads as "no rates" when the rates were merely still in flight.
        /// Pass <paramref name="timeoutSeconds"/> to widen or tighten it for a specific call.
        /// </summary>
        async Task<ApiResponse<SubmissionResponseModel>> GetSubmissionWithPollingAsync(
            string applicationId,
            int timeoutSeconds = 300) =>
            await RetryHelper.RetryUntilAsync(
                () => GetSubmissionAsync(applicationId),
                content => content.Status == "Completed",
                timeout: TimeSpan.FromSeconds(timeoutSeconds),
                label: $"Submission Status='Completed' for application '{applicationId}'");

        [Get("/applications/{applicationId}/submission/{inputParams}")]
        Task<ApiResponse<SubmissionResponseModel>> GetSpecificSubmissionWithParamsAsync(string applicationId, string inputParams);

        [Post("/applications/{applicationId}/submit")]
        Task<ApiResponse<SubmitApplicationResponseModel>> SubmitApplicationAsync(string applicationId);

        [Post("/applications")]
        Task<ApiResponse<PostApplicationResponseModel<T>>> CreateApplicationAsync<T>([Body] ApplicationRequestModel<T> request);

        [Patch("/applications/{applicationId}")]
        Task<ApiResponse<PostApplicationResponseModel<T>>> PatchApplicationAsync<T>(string applicationId, [Body] ApplicationRequestModel<T> request);

        [Post("/applications/create/submit")]
        Task<ApiResponse<CreateAndSubmitApplication>> CreateAndSubmitApplicationAsync<T>([Body] ApplicationRequestModel<T> request);

        [Get("/applications/{applicationId}/questionnaire")]
        Task<ApiResponse<QuestionnaireResponseModel>> GetQuestionnaireAsync(string applicationId);

        [Post("/applications/create/submit}")]
        Task<ApiResponse<NoContent>> CreateContactCaseAsync([Body] ApplicationCreateContactRequestModel request);

        [Get("/applications/headers/{applicationId}")]
        Task<ApiResponse<ApplicationHeader>> GetApplicationHeadersByApplicationIdAsync(string applicationId);

        [Post("/applications/search")]
        Task<ApiResponse<List<ApplicationHeader>>> SearchApplicationsAsync([Body] SearchApplicationsRequestModel request);

        [Get("/applications/{applicationId}/questions/fullquote?Carrier={carrier}&Product={product}")]
        Task<ApiResponse<ApplicationQuestionsFullQuote>> GetApplicationQuestionsFullQuoteAsync(
            string applicationId,
            string carrier,
            string product,
            [AliasAs("QuestionSet")] string? questionSet = null);

        Task<ApiResponse<ApplicationQuestionsFullQuote>> GetApplicationQuestionsFullQuoteAsync(
            string applicationId,
            string carrier,
            string product,
            bool fullQuestionSet) =>
            GetApplicationQuestionsFullQuoteAsync(applicationId, carrier, product, fullQuestionSet ? "All" : null);

        [Patch("/applications//{applicationId}/questions")]
        Task<ApiResponse<ApplicationQuestionsFullQuote>> PatchApplicationQuestionsFullQuoteAsync<T>(string applicationId, [Body] PatchApplicationRequestModel<T> request);

        [Get("/applications/{externalId}/FindData?Type={lob}")]
        Task<ApiResponse<GetApplicationResponseModel>> GetApplicationByFindDataAsync(string externalId, string lob);

        [Get("/applications/{applicationId}/quotes")]
        Task<ApiResponse<List<QuoteResponseModel>>> GetQuotesByApplicationIdAsync(string applicationId);

        [Get("/applications/headers/friendlyid/{friendlyId}")]
        Task<ApiResponse<List<string>>> GetApplicationByFriendlyIdAsync(string friendlyId);

        [Get("/applications/applicant/{applicantId}")]
        Task<ApiResponse<List<string>>> GetApplicationByApplicantIdAsync(string applicantId);

        [Get("/applications/externalid/{carrierName}/{carrierQuoteNumb}")]
        Task<ApiResponse<string>> GetCarriersQuotesByQuoteExternalIdAsync(string carrierName, string carrierQuoteNumb);
    }
}
