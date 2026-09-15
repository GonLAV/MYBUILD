using Bolt.Automation.ApiClients.AdbxApi.Entities.Subtenant;
using Refit;

namespace Bolt.Automation.ApiClients.AdbxApi.Interfaces
{
    public partial interface IAdbxSubtenantApi
    {
        [Put("/subtenants/{subtenantId}/mfa-settings/")]
        Task<ApiResponse<object>> UpdateSubtenantMfaSettings(string subtenantId, [Body] UpdateSubtenantMfaSettingsModel request);
    }
}
