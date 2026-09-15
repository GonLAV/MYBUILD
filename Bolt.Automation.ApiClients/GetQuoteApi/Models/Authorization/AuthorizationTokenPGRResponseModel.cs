using System.Text.Json.Serialization;

namespace Bolt.Automation.ApiClients.GetQuoteApi.Models.Authorization
{
    public class AuthorizationTokenPGRResponseModel
    {
        [JsonPropertyName("token")]
        public string? Token { get; set; }
        public bool IsSuccess { get; set; }
    }
}
