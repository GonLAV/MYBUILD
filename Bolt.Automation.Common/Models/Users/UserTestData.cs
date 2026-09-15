using Bolt.Automation.Common.Enums;

namespace Bolt.Automation.Common.Models.Users
{
    public class UserTestData
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Username { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string UserExternalId { get; set; } = string.Empty;
        public string GroupExternalId { get; set; } = string.Empty;
        public string WorkSpaceGroupId { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public bool SendAgentIdentity { get; set; } = false;
        public string ApiKey { get; set; } = string.Empty;
        public string OAuthToken { get; set; } = string.Empty;
        public string AgentIdentity { get; set; } = string.Empty;
        public string ApiSource { get; set; } = string.Empty;
        public string Subtenant { get; set; } = string.Empty;
        public string SubtenantId { get; set; } = string.Empty;
        public string LoginUrl { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public List<string> Permissions { get; set; } = [];
        public Dictionary<string, object> Attributes { get; set; } = new();
        public SsoUserData? Sso { get; set; }

        /// <summary>
        /// Deep copy for hydration. The static <c>UserDataStore</c> instances are shared once per
        /// process; secret overlay must happen on a private clone so parallel tests never race on
        /// (or leak credentials into) the shared instance.
        /// </summary>
        public UserTestData Clone()
        {
            var clone = (UserTestData)MemberwiseClone();
            clone.Permissions = [.. Permissions];
            clone.Attributes = new Dictionary<string, object>(Attributes);
            clone.Sso = Sso is null ? null : new SsoUserData { Issuer = Sso.Issuer, Audience = Sso.Audience };
            return clone;
        }
    }
}
