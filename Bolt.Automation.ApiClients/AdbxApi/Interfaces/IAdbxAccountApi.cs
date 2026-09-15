using Bolt.Automation.ApiClients.AdbxApi.Entities.Account;
using Refit;

namespace Bolt.Automation.ApiClients.AdbxApi.Interfaces
{
    public partial interface IAdbxAccountApi
    {
        [Get("/accounts/{accountId}")]
        Task<ApiResponse<GetAccountResponse>> GetAccount(string accountId);
    }
}
