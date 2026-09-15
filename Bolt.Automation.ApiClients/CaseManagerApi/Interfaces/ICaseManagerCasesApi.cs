using Bolt.Automation.ApiClients.CaseManagerApi.Entities.Cases.CreateCase;
using Bolt.Automation.ApiClients.CaseManagerApi.Entities.Cases.CreateCaseMessages;
using Bolt.Automation.ApiClients.CaseManagerApi.Entities.Cases.UpdateCase;
using Bolt.Automation.ApiClients.CaseManagerApi.Entities.Common;
using Refit;

namespace Bolt.Automation.ApiClients.CaseManagerApi.Interfaces
{
    public partial interface ICaseManagerCasesApi
    {
        [Post("/{tenant}/cases")]
        Task<ApiResponse<CreateCaseResponse>> CreateCaseAsync([AliasAs("tenant")] string tenant, [Body] CreateCaseRequest request);

        [Put("/{tenant}/cases")]
        Task<ApiResponse<UpdateCaseResponse>> UpdateCaseAsync([AliasAs("tenant")] string tenant, [Body] UpdateCaseRequest request);

        [Get("/{tenant}/cases/{caseExternalId}/file/{fileId}")]
        Task<ApiResponse<FileModel>> GetCaseFileAsync([AliasAs("tenant")] string tenant, Guid fileId, string caseExternalId);

        [Post("/{tenant}/cases/{caseExternalId}/messages")]
        Task<ApiResponse<CreateCaseMessageResponse>> CreateCaseMessageAsync([AliasAs("tenant")] string tenant, string caseExternalId, [Body] CreateCaseMessageRequest request);

        [Get("/{tenant}/cases/{caseExternalId}/messages")]
        Task<ApiResponse<MessageListModel>> GetCaseMessagesAsync([AliasAs("tenant")] string tenant, string caseExternalId);
    }
}
