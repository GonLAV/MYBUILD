using System.Diagnostics;
using System.Text;
using Bolt.Automation.ApiClients.AdbxApi.Entities;
using Bolt.Automation.ApiClients.StsApi;
using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.Common.Models.Users;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Refit;

namespace Bolt.Automation.ApiClients.AdbxApi.Handlers
{
    public class AdbxApiHandler(
        IAutomationLogger logger,
        IScopeContext scopeContext)
        : DelegatingHandler
    {
        private readonly IAutomationLogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IScopeContext _scopeContext = scopeContext ?? throw new ArgumentNullException(nameof(scopeContext));

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var sw = Stopwatch.StartNew();
            var response = await base.SendAsync(request, cancellationToken);
            sw.Stop();
            await _logger.LogApiCallAsync(request, response, sw.ElapsedMilliseconds);
            return response;
        }

        public async Task<string> GetTokenFromStsAsync(UserTestData userData)
        {
            //var user = userData.Role.ToString();

            var url = _scopeContext.Data.UrlDataCollection.AdbxApi;

            var loginUrl = !string.IsNullOrEmpty(userData.LoginUrl) ? userData.LoginUrl : url.LoginUrl;
            if (string.IsNullOrEmpty(loginUrl))
                throw new InvalidOperationException("LoginUrl is not specified in user data or context.");

            var httpClient = new HttpClient
            {
                BaseAddress = new Uri(loginUrl)
            };

            var stsApi = RestService.For<IStsApi>(httpClient);
            var loginPage = await stsApi.GetLoginPage();
            var tokenRequestBody = GetTokenRequestBody(userData, loginPage);
            var response = await stsApi.GetToken(tokenRequestBody);

            var tokenUri = response.RequestMessage?.RequestUri
                ?? throw new InvalidOperationException($"Failed to get token from STS for user '{userData.Username}': no redirect URI returned.");

            if (tokenUri.AbsolutePath.Contains("ResetPassword", StringComparison.OrdinalIgnoreCase))
            {
                var resetMessage = GetQueryValue(tokenUri, "message");
                var reason = string.IsNullOrEmpty(resetMessage) ? "a password reset is required." : Uri.UnescapeDataString(resetMessage);
                throw new InvalidOperationException($"STS login failed for user '{userData.Username}': {reason}");
            }

            var token = GetQueryValue(tokenUri, "token")
                ?? throw new InvalidOperationException($"Failed to get token from STS for user '{userData.Username}': unexpected redirect URI '{tokenUri}'.");

            return token;
        }

        private static string? GetQueryValue(Uri uri, string key)
        {
            var query = uri.Query.TrimStart('?');
            foreach (var pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var separatorIndex = pair.IndexOf('=');
                var name = separatorIndex >= 0 ? pair[..separatorIndex] : pair;
                if (!string.Equals(name, key, StringComparison.OrdinalIgnoreCase))
                    continue;

                return separatorIndex >= 0 ? pair[(separatorIndex + 1)..] : string.Empty;
            }

            return null;
        }

        private static string GetTokenRequestBody(UserTestData userData, string loginPage)
        {
            string userName = userData.Username;
            string password = userData.Password;
            string groupName = userData.GroupExternalId ?? string.Empty;
            string subtenant = !string.IsNullOrEmpty(userData.Subtenant)
                ? userData.Subtenant
                : string.Empty;

            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(loginPage);
            string? requestVerificationToken = htmlDoc
                .DocumentNode
                .SelectSingleNode("//form[@id='LoginForm']/input[@name='__RequestVerificationToken']")
                ?.GetAttributeValue("value", "");

            return $"Subtenant={subtenant}&Email={userName.Replace("@", "%40")}&GroupExternalId={groupName}&Password={password}&__RequestVerificationToken={requestVerificationToken}";
        }
        
        public static async Task<string> GetCurrentSessionToken(HttpClient client, string tenant, string token)
        {
            var entranceData = new EntranceData
            {
                Tenant = tenant,
                Token = token
            };

            string entranceDataSerialized = JsonConvert.SerializeObject(entranceData);
            var fullTokenRequestBody = new StringContent(entranceDataSerialized, Encoding.UTF8, "application/json");
            var fullTokenUrl = string.Concat(client.BaseAddress, "current-session/enter");
            var fullTokenResp = await client.PostAsync(fullTokenUrl, fullTokenRequestBody);
            var fullTokenRespContent = await fullTokenResp.Content.ReadAsStringAsync();

            var fullTokenJson = JsonConvert.DeserializeObject<JObject>(fullTokenRespContent)
                ?? throw new InvalidOperationException("Failed to deserialize response content into JSON.");
            var authToken = fullTokenJson.SelectToken("authToken");

            return authToken == null
                ? throw new InvalidOperationException("authToken not found in the response JSON.")
                : authToken.ToString();
        }
    }
}
