using System.Text.Json;
using System.Text.Json.Serialization;
using Automation.Configuration.ExternalServices;
using Microsoft.Extensions.Options;
using Microsoft.Kiota.Abstractions.Authentication;

namespace Bolt.Automation.ExternalServices.Outlook
{
    public class TokenProvider : IAccessTokenProvider
    {
        private readonly OutlookClientOptions _options;

        public TokenProvider(IOptions<OutlookClientOptions> options)
        {
            _options = options.Value;
        }

        public AllowedHostsValidator AllowedHostsValidator { get; } = new AllowedHostsValidator();

        public async Task<string> GetAuthorizationTokenAsync(Uri uri, Dictionary<string, object>? additionalAuthenticationContext = null, CancellationToken cancellationToken = default)
        {
            var tenantId = _options.TenantId;
            var clientId = _options.ClientId;
            var clientSecret = _options.ClientSecret;

            var client = new HttpClient();
            var tokenEndpoint = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token";

            var content = new FormUrlEncodedContent([
                new KeyValuePair<string, string>("client_id", clientId),
            new KeyValuePair<string, string>("scope", "https://graph.microsoft.com/.default"),
            new KeyValuePair<string, string>("client_secret", clientSecret),
            new KeyValuePair<string, string>("grant_type", "client_credentials")
            ]);

            var response = await client.PostAsync(tokenEndpoint, content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var tokenResult = JsonSerializer.Deserialize<TokenResponse>(json);

            return tokenResult?.AccessToken ?? throw new Exception("Failed to retrieve access token.");
        }

        private class TokenResponse
        {
            [JsonPropertyName("access_token")]
            public string AccessToken { get; set; }
        }
    }
}
