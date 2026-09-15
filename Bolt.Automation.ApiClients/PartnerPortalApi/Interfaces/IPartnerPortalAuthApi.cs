using Bolt.Automation.ApiClients.PartnerPortalApi.Entities.Login;
using Refit;

namespace Bolt.Automation.ApiClients.PartnerPortalApi.Interfaces
{
    public interface IPartnerPortalAuthApi
    {
        [Post("/login/token")]
        Task<ApiResponse<LoginResponse>> GetTokenAsync([Header("Tenant")] string tenant, [Header("Source")] string source, [Body] LoginRequest request);
    }
}
