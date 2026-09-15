using Bolt.Automation.ApiClients.AdbxApi.Entities.Lead;
using Refit;

namespace Bolt.Automation.ApiClients.AdbxApi.Interfaces
{
    public partial interface IAdbxLeadApi
    {
        [Get("/leads/{leadId}/policies")]
        Task<ApiResponse<IEnumerable<LeadPolicyListItemModel>>> GetLeadPolicies(string leadId);

        [Get("/leads/{leadId}/quotes")]
        Task<ApiResponse<IEnumerable<LeadQuoteListItemModel>>> GetLeadQuotes(string leadId);

        [Get("/leads/{leadId}/communications")]
        Task<ApiResponse<GetCommunicationsResultModel>> GetLeadCommunications(string leadId);

        [Get("/leads/{leadId}/timeline")]
        Task<ApiResponse<GetTimelineResultModel>> GetLeadTimeline(string leadId);
    }
}
