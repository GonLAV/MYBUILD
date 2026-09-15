namespace Bolt.Automation.ApiClients.PlatformApi.Entities.Provision
{
    public class User
    {
        public string Type { get; set; } = string.Empty;
        public string ExternalUserType { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string ChoiceBucket { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string? MiddleName { get; set; }
        public string LastName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public List<AffiliatedGroup> AffiliatedGroups { get; set; } = [];
    }
}