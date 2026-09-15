namespace Bolt.Automation.ApiClients.PartnerPortalApi.Entities.Login
{
    public class LoginResponse
    {
        public int ExpirationTime { get; set; }
        public string access_token { get; set; }
        public string Error { get; set; }
    }
}
