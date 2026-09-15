using Refit;

namespace Bolt.Automation.ApiClients.AdbxApi.Interfaces
{
    public partial interface IAdbxJourneyApi
    {
        /// <summary>
        /// Journey type names offered to the calling user, e.g. <c>["Internal","External",
        /// "BoltConsumer","Farmers"]</c>. ADBX scopes the set to the user's subtenant, so two
        /// users of the same tenant can legitimately get different lists.
        /// </summary>
        [Get("/journeys/available-journey-types")]
        Task<ApiResponse<IEnumerable<string>>> GetAvailableJourneyTypes();
    }
}
