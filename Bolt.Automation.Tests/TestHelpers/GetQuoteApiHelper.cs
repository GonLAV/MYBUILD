using Bolt.Automation.ApiClients.GetQuoteApi.Extensions;
using Bolt.Automation.ApiClients.GetQuoteApi.Interfaces;
using Bolt.Automation.ApiClients.GetQuoteApi.Models.Application;
using Bolt.Automation.ApiClients.Infrastructure;
using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Exceptions;


namespace Bolt.Automation.Tests.TestHelpers
{
    public class GetQuoteApiHelper(IGetQuoteApi getQuoteApi, IScopeContext scopeContext)
    {
        private readonly IGetQuoteApi _getQuoteApi = getQuoteApi ?? throw new ArgumentNullException(nameof(getQuoteApi));
        private readonly IScopeContext _scopeContext = scopeContext ?? throw new ArgumentNullException(nameof(scopeContext));

        public async Task<SubmissionResponseModel> CreateAndSubmitApplicationWithPollingAsync<T>(ApplicationRequestModel<T> request, bool ratesCheck = true)
        {
            // 1. Create application
            var createResponse = await RetryHelper.RetryOnTransientAsync(
                () => _getQuoteApi.CreateApplicationAsync(request), "Failed to create application");
            createResponse.MapIdentifiers(_scopeContext);
            var applicationId = createResponse.Id
                ?? throw new ApiResponseException("Application ID is null — the create application response did not return an ID.");

            return await SubmitApplicationWithPollingAsync(applicationId, ratesCheck);
        }

        public async Task<QuestionnaireResponseModel> CreateApplication_GetQuestionnaire<T>(ApplicationRequestModel<T> request)
        {
            // 1. Create application
            var createResponse = await RetryHelper.RetryOnTransientAsync(
                () => _getQuoteApi.CreateApplicationAsync(request), "Failed to create application");
            createResponse.MapIdentifiers(_scopeContext);
            var applicationId = createResponse.Id
                ?? throw new ApiResponseException("Application ID is null — the create application response did not return an ID.");
            var getQuestionnaireResp = await RetryHelper.RetryOnTransientAsync(
                () => _getQuoteApi.GetQuestionnaireAsync(applicationId), "Failed to get questionnaire");
            return getQuestionnaireResp;
        }

        /// <summary>
        /// Gets the full-quote question ids for an application, so a test can assert on set membership.
        /// Pass <paramref name="fullQuestionSet"/> true for the complete set; false returns the filtered set.
        /// Returns an empty list when the response carries no questions.
        /// </summary>
        public async Task<List<string>> GetQuestionIdsFullQuoteAsync(
            string applicationId,
            string carrier,
            string product,
            bool fullQuestionSet = false)
        {
            var response = await _getQuoteApi.GetApplicationQuestionsFullQuoteAsync(applicationId, carrier, product, fullQuestionSet)
                .EnsureSuccessContentAsync(
                    $"Failed to get full-quote questions for application '{applicationId}' " +
                    $"(QuestionSet={(fullQuestionSet ? "All" : "<omitted>")})");

            return response.Questions?.Select(question => question.Id).OfType<string>().ToList() ?? [];
        }

        public async Task<SubmissionResponseModel> SubmitApplicationWithPollingAsync(string applicationId, bool ratesCheck = true)
        {
            var submitResponse = await RetryHelper.RetryOnTransientAsync(
                () => _getQuoteApi.SubmitApplicationAsync(applicationId), "Failed to submit request");

            var submissionResponse = await RetryHelper.RetryOnTransientAsync(
                () => _getQuoteApi.GetSubmissionWithPollingAsync(applicationId), "Failed to get submission");
            if (ratesCheck)
            {
                if (!submissionResponse.Quotes.Any(x => x.Status == "Success"))
                {
                    throw new ApiResponseException("No rates returned — submission completed but no quote reached 'Success' status.");
                }
            }
            return submissionResponse;
        }
    }
}
