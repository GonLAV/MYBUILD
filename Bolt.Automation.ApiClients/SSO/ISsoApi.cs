using Bolt.Automation.ApiClients.SSO.Entities;
using Refit;

namespace Bolt.Automation.ApiClients.SSO
{
    public interface ISsoApi
    {
        [Post("")]
        Task<ApiResponse<SsoHttpResponse>> GetSsoResponse();
    }
}
