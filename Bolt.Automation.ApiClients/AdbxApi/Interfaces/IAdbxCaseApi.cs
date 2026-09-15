using Bolt.Automation.ApiClients.AdbxApi.Entities;
using Bolt.Automation.ApiClients.AdbxApi.Entities.Case;
using Bolt.Automation.ApiClients.AdbxApi.Entities.Case.SaleCaseMessage;
using Refit;

namespace Bolt.Automation.ApiClients.AdbxApi.Interfaces
{
    public partial interface IAdbxCaseApi
    {
        [Get("/cases/{caseId}/notes")]
        Task<ApiResponse<GetCaseNotesResultModel>> GetCaseNotes(string caseId, [Query] PaginationQuery query);

        [Get("/cases/{caseId}")]
        Task<ApiResponse<CaseModel>> GetCase(string caseId);

        [Post("/cases/{caseId}/messages-to-uwr")]
        Task<ApiResponse<SendCaseMessageToUwrResultModel>> SendMessageToUwr(string caseId, [Body] SendCaseMessageToUwrModel request);

        [Get("/cases/queues")]
        Task<ApiResponse<UserCaseQueueModel>> GetCaseQueues();

        [Delete("/cases/views/{caseViewId}")]
        Task<ApiResponse<object>> DeleteCaseView(string caseViewId);

        [Post("/cases/views")]
        Task<ApiResponse<CreateCaseViewCommandResponse>> PostCaseView([Body] CreateCaseViewPayload request);

    }
}
