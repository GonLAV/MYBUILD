namespace Bolt.Automation.ApiClients.PlatformApi.Entities.Provision.CreateUser
{
    public class GetUsersResponseModel
    {
        public int TotalResults { get; set; }

        public List<CreateUserRequestModel>? Resources { get; set; }
    }
}
