using Newtonsoft.Json;

namespace Bolt.Automation.ApiClients.PlatformApi.Entities.Provision.UpdateUser
{
    public class UpdateUserRequest
    {
        [JsonProperty("agentID")]
        public Guid AgentID { get; set; }

        [JsonProperty("supervisorID")]
        public Guid SupervisorID { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("externalId")]
        public string ExternalId { get; set; }

        [JsonProperty("userName")]
        public string UserName { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("name")]
        public NameInfo Name { get; set; }

        [JsonProperty("emails")]
        public List<EmailInfo> Emails { get; set; }

        [JsonProperty("userType")]
        public string UserType { get; set; }

        [JsonProperty("entitlements")]
        public List<EntitlementInfo> Entitlements { get; set; }

        [JsonProperty("totalResults")]
        public int TotalResults { get; set; }

        [JsonProperty("Resources")]
        public List<object> Resources { get; set; }
    }

    public class EntitlementInfo
    {
        [JsonProperty("Value")]
        public string Value { get; set; }
    }

}
