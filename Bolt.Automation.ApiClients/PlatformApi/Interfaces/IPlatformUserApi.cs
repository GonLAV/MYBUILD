using Bolt.Automation.ApiClients.PlatformApi.Entities.Provision.BatchUserGroup;
using Bolt.Automation.ApiClients.PlatformApi.Entities.Provision.CreateUser;
using Bolt.Automation.ApiClients.PlatformApi.Entities.Provision.UpdateUser;
using Refit;

namespace Bolt.Automation.ApiClients.PlatformApi.Interfaces
{
    public partial interface IPlatformUserApi
    {
        [Post("/QuoteAPI/v1/Users")]
        Task<ApiResponse<CreateUserResponseModel>> CreateUser([Body] CreateUserRequestModel request);

        [Put("/QuoteAPI/v1/Users/{id}")]
        Task<ApiResponse<UpdateUserResponse>> UpdateUser(string id, [Body] CreateUserRequestModel request);

        [Delete("/QuoteAPI/v1/Users/{id}")]
        Task<ApiResponse<BatchUserGroupResponse>> DeleteUser(string id);

        [Get("/QuoteAPI/v1/Users")]
        Task<ApiResponse<GetUsersResponseModel>> GetUsers();
    }
}
