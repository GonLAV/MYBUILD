using Bolt.Automation.ApiClients.GetQuoteApi.Models.Common;
using Bolt.Automation.ApiClients.GetQuoteApi.Models.Policy;
using Bolt.Automation.ApiClients.GetQuoteApi.Models.Quote;
using Refit;

namespace Bolt.Automation.ApiClients.GetQuoteApi.Interfaces
{
    public partial interface IGetQuoteQuotesApi
    {
        [Get("/quotes/{quoteId}/policy")]
        Task<ApiResponse<PolicyResponseModel>> GetQuotePolicyAsync(string quoteId);

        [Post("/quotes/{quoteId}/select")]
        Task<ApiResponse<NoContent>> SelectQuoteAsync(string quoteId);

        [Post("/quotes/{quoteId}/plans/{planId}/select")]
        Task<ApiResponse<NoContent>> SelectQuotePaymentPlanAsync(string quoteId, string planId);

        [Post("/quotes/{quoteId}/plans/{planId}/pay")]
        Task<ApiResponse<QuotePaymentPlanResponseModel>> PayQuotePaymentPlanAsync(string quoteId, string planId, [Body] object request);

        [Post("/quote/{id}/policy")]
        Task<ApiResponse<IdObject>> CreatePolicyAsync(string Id, [Body] PolicyRequestModel request);

        [Patch("/quote/policy/{policyId}")]
        Task<ApiResponse<IdObject>> PatchPolicyAsync(string policyId, [Body] PolicyRequestModel request);
    }
}
