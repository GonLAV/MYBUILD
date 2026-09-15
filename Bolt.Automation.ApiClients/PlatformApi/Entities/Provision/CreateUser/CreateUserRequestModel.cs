namespace Bolt.Automation.ApiClients.PlatformApi.Entities.Provision.CreateUser
{
    public class CreateUserRequestModel
    {
        public string? Id { get; set; }

        public string? ExternalId { get; set; }

        public string? UserName { get; set; }

        public string? Title { get; set; }

        public NameInfo? Name { get; set; }

        public List<EmailInfo>? Emails { get; set; }

        public string? UserType { get; set; }

        public List<Entitlement>? Entitlements { get; set; }
    }

}
