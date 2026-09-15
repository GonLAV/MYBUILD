
namespace Bolt.Automation.ApiClients.CaseManagerApi.Entities.Users
{
    public class CreateUpdateUserRequest
    {
        public string ExternalId { get; set; }
        public string Role { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }
}
