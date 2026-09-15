using Bolt.Automation.ApiClients.GetQuoteApi.Models.Case;
using Refit;

namespace Bolt.Automation.ApiClients.GetQuoteApi.Interfaces
{
    public partial interface IGetQuoteCaseApi
    {
        [Get("/cases/{applicationId}")]
        Task<ApiResponse<List<CaseResponseModel>>> GetCaseAsync(string applicationId);
    }
}
